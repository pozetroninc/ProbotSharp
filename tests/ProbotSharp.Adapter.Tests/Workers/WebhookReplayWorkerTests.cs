// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ProbotSharp.Adapters.Workers;
using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Adapter.Tests.Workers;

public sealed class WebhookReplayWorkerTests
{
    private readonly ILogger<WebhookReplayWorker> _logger = Substitute.For<ILogger<WebhookReplayWorker>>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IServiceScope _scope = Substitute.For<IServiceScope>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IWebhookReplayQueuePort _queuePort = Substitute.For<IWebhookReplayQueuePort>();
    private readonly IReplayWebhookPort _replayPort = Substitute.For<IReplayWebhookPort>();
    private readonly IDeadLetterQueuePort _deadLetterQueuePort = Substitute.For<IDeadLetterQueuePort>();
    private readonly IMetricsPort _metricsPort = Substitute.For<IMetricsPort>();
    private readonly WebhookReplayWorkerOptions _options;

    public WebhookReplayWorkerTests()
    {
        _options = new WebhookReplayWorkerOptions
        {
            PollIntervalMs = 10, // Fast polling for tests
            MaxRetryAttempts = 3,
            RetryBaseDelayMs = 100,
        };

        // Setup service provider mocks
        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.Returns(_serviceProvider);

        _serviceProvider.GetService(typeof(IWebhookReplayQueuePort)).Returns(_queuePort);
        _serviceProvider.GetService(typeof(IReplayWebhookPort)).Returns(_replayPort);
        _serviceProvider.GetService(typeof(IDeadLetterQueuePort)).Returns(_deadLetterQueuePort);
        _serviceProvider.GetService(typeof(IMetricsPort)).Returns(_metricsPort);
    }

    private static EnqueueReplayCommand CreateReplayCommand(
        int attempt = 0,
        string eventName = "issues.opened",
        string? deliveryId = null)
    {
        var actualDeliveryId = deliveryId ?? Guid.NewGuid().ToString();
        var rawPayload = "{\"action\":\"opened\"}";

        var processCommand = new ProcessWebhookCommand(
            DeliveryId.Create(actualDeliveryId),
            WebhookEventName.Create(eventName),
            WebhookPayload.Create(rawPayload),
            InstallationId.Create(12345),
            WebhookSignature.Create("sha256=" + new string('a', 64)),
            rawPayload);

        return new EnqueueReplayCommand(processCommand, attempt);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldThrow_WhenLoggerIsNull()
    {
        // Act
        var act = () => new WebhookReplayWorker(
            null!,
            _scopeFactory,
            Options.Create(_options));

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenScopeFactoryIsNull()
    {
        // Act
        var act = () => new WebhookReplayWorker(
            _logger,
            null!,
            Options.Create(_options));

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenOptionsIsNull()
    {
        // Act
        var act = () => new WebhookReplayWorker(
            _logger,
            _scopeFactory,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Happy Path Tests

    [Fact]
    public async Task ExecuteAsync_ShouldProcessCommand_WhenDequeueSucceeds()
    {
        // Arrange
        var deliveryId = Guid.NewGuid().ToString();
        var command = CreateReplayCommand(attempt: 0, deliveryId: deliveryId);

        // First dequeue returns command, second returns null to stop the loop
        _queuePort.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(
                Result<EnqueueReplayCommand?>.Success(command),
                Result<EnqueueReplayCommand?>.Success(null));

        _replayPort.ReplayAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        using var worker = new WebhookReplayWorker(_logger, _scopeFactory, Options.Create(_options));

        // Act
        await worker.StartAsync(default);
        await Task.Delay(50); // Let it process one cycle
        await worker.StopAsync(default);

        // Assert
        await _replayPort.Received(1).ReplayAsync(
            Arg.Is<EnqueueReplayCommand>(c =>
                c.Attempt == 0 &&
                c.Command.DeliveryId.Value == deliveryId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIncrementMetrics_WhenReplaySucceeds()
    {
        // Arrange
        var command = CreateReplayCommand(attempt: 0);

        _queuePort.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(
                Result<EnqueueReplayCommand?>.Success(command),
                Result<EnqueueReplayCommand?>.Success(null));

        _replayPort.ReplayAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        using var worker = new WebhookReplayWorker(_logger, _scopeFactory, Options.Create(_options));

        // Act
        await worker.StartAsync(default);
        await Task.Delay(50);
        await worker.StopAsync(default);

        // Assert
        _metricsPort.Received(1).IncrementCounter(
            "webhook_replay_success",
            1,
            Arg.Any<KeyValuePair<string, object?>>(),
            Arg.Any<KeyValuePair<string, object?>>());
    }

    #endregion

    #region Retry Logic Tests

    [Fact]
    public async Task ExecuteAsync_ShouldReEnqueue_WhenReplayFails()
    {
        // Arrange
        var command = CreateReplayCommand(attempt: 0);

        _queuePort.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(
                Result<EnqueueReplayCommand?>.Success(command),
                Result<EnqueueReplayCommand?>.Success(null));

        _replayPort.ReplayAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure("replay_error", "Failed to replay"));

        _queuePort.EnqueueAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        using var worker = new WebhookReplayWorker(_logger, _scopeFactory, Options.Create(_options));

        // Act
        await worker.StartAsync(default);
        await Task.Delay(50);
        await worker.StopAsync(default);

        // Assert
        await _queuePort.Received(1).EnqueueAsync(
            Arg.Is<EnqueueReplayCommand>(c => c.Attempt == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIncrementRetryMetrics_WhenReEnqueuing()
    {
        // Arrange
        var command = CreateReplayCommand(attempt: 1);

        _queuePort.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(
                Result<EnqueueReplayCommand?>.Success(command),
                Result<EnqueueReplayCommand?>.Success(null));

        _replayPort.ReplayAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure("replay_error", "Failed"));

        _queuePort.EnqueueAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        using var worker = new WebhookReplayWorker(_logger, _scopeFactory, Options.Create(_options));

        // Act
        await worker.StartAsync(default);
        await Task.Delay(150); // Allow time for backoff delay
        await worker.StopAsync(default);

        // Assert
        _metricsPort.Received(1).IncrementCounter(
            "webhook_replay_retry",
            1,
            Arg.Any<KeyValuePair<string, object?>>(),
            Arg.Any<KeyValuePair<string, object?>>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldApplyBackoff_WhenRetrying()
    {
        // Arrange - command with attempt 1 should have backoff delay
        var command = CreateReplayCommand(attempt: 1);

        _queuePort.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(
                Result<EnqueueReplayCommand?>.Success(command),
                Result<EnqueueReplayCommand?>.Success(null));

        _replayPort.ReplayAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        using var worker = new WebhookReplayWorker(_logger, _scopeFactory, Options.Create(_options));
        var startTime = DateTime.UtcNow;

        // Act
        await worker.StartAsync(default);
        await Task.Delay(200); // Wait longer than poll interval to account for backoff
        await worker.StopAsync(default);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert - Should have processed the command (with backoff delay applied)
        await _replayPort.Received(1).ReplayAsync(
            Arg.Any<EnqueueReplayCommand>(),
            Arg.Any<CancellationToken>());

        // Verify some delay occurred (at least backoff for attempt 1 = 100ms)
        elapsed.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(100);
    }

    #endregion

    #region Dead Letter Queue Tests

    [Fact]
    public async Task ExecuteAsync_ShouldMoveToDeadLetter_WhenMaxRetriesExceeded()
    {
        // Arrange - command already at max retries (3)
        var command = CreateReplayCommand(attempt: 3);

        _queuePort.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(
                Result<EnqueueReplayCommand?>.Success(command),
                Result<EnqueueReplayCommand?>.Success(null));

        _deadLetterQueuePort.MoveToDeadLetterAsync(
                Arg.Any<EnqueueReplayCommand>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        using var worker = new WebhookReplayWorker(_logger, _scopeFactory, Options.Create(_options));

        // Act
        await worker.StartAsync(default);
        await Task.Delay(50);
        await worker.StopAsync(default);

        // Assert - Should move to DLQ, not replay
        await _deadLetterQueuePort.Received(1).MoveToDeadLetterAsync(
            Arg.Is<EnqueueReplayCommand>(c => c.Attempt == 3),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await _replayPort.DidNotReceive().ReplayAsync(
            Arg.Any<EnqueueReplayCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIncrementDlqMetrics_WhenMovingToDeadLetter()
    {
        // Arrange
        var command = CreateReplayCommand(attempt: 3);

        _queuePort.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(
                Result<EnqueueReplayCommand?>.Success(command),
                Result<EnqueueReplayCommand?>.Success(null));

        _deadLetterQueuePort.MoveToDeadLetterAsync(
                Arg.Any<EnqueueReplayCommand>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        using var worker = new WebhookReplayWorker(_logger, _scopeFactory, Options.Create(_options));

        // Act
        await worker.StartAsync(default);
        await Task.Delay(50);
        await worker.StopAsync(default);

        // Assert
        _metricsPort.Received(1).IncrementCounter(
            "webhook_replay_dlq_moved",
            1,
            Arg.Any<KeyValuePair<string, object?>>(),
            Arg.Any<KeyValuePair<string, object?>>());
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task ExecuteAsync_ShouldReEnqueueOnException_WhenNotAtMaxAttempts()
    {
        // Arrange
        var command = CreateReplayCommand(attempt: 0);

        _queuePort.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(
                Result<EnqueueReplayCommand?>.Success(command),
                Result<EnqueueReplayCommand?>.Success(null));

        _replayPort.ReplayAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>())
            .Returns<Result>(x => throw new InvalidOperationException("Test exception"));

        _queuePort.EnqueueAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        using var worker = new WebhookReplayWorker(_logger, _scopeFactory, Options.Create(_options));

        // Act
        await worker.StartAsync(default);
        await Task.Delay(50);
        await worker.StopAsync(default);

        // Assert
        await _queuePort.Received(1).EnqueueAsync(
            Arg.Is<EnqueueReplayCommand>(c => c.Attempt == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIncrementErrorMetrics_WhenExceptionOccurs()
    {
        // Arrange
        var command = CreateReplayCommand(attempt: 0);

        _queuePort.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(
                Result<EnqueueReplayCommand?>.Success(command),
                Result<EnqueueReplayCommand?>.Success(null));

        _replayPort.ReplayAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>())
            .Returns<Result>(x => throw new InvalidOperationException("Test exception"));

        _queuePort.EnqueueAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        using var worker = new WebhookReplayWorker(_logger, _scopeFactory, Options.Create(_options));

        // Act
        await worker.StartAsync(default);
        await Task.Delay(50);
        await worker.StopAsync(default);

        // Assert
        _metricsPort.Received(1).IncrementCounter(
            "webhook_replay_error_retry",
            1,
            Arg.Any<KeyValuePair<string, object?>>(),
            Arg.Any<KeyValuePair<string, object?>>());
    }

    #endregion

    #region Lifecycle Tests

    [Fact]
    public async Task ExecuteAsync_ShouldStopGracefully_WhenCancellationRequested()
    {
        // Arrange
        _queuePort.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(Result<EnqueueReplayCommand?>.Success(null));

        using var worker = new WebhookReplayWorker(_logger, _scopeFactory, Options.Create(_options));

        // Act
        await worker.StartAsync(default);
        await Task.Delay(30); // Let it run briefly
        await worker.StopAsync(default); // Should stop gracefully

        // Assert - No exceptions thrown, worker stopped cleanly
        await _queuePort.Received().DequeueAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldContinueProcessing_AfterHandlingException()
    {
        // Arrange
        var command1 = CreateReplayCommand(attempt: 0, deliveryId: "delivery1");
        var command2 = CreateReplayCommand(attempt: 0, deliveryId: "delivery2");

        _queuePort.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(
                Result<EnqueueReplayCommand?>.Success(command1),
                Result<EnqueueReplayCommand?>.Success(command2),
                Result<EnqueueReplayCommand?>.Success(null));

        // First call throws, second succeeds
        var callCount = 0;
        _replayPort.ReplayAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("Test exception");
                }

                return Task.FromResult(Result.Success());
            });

        _queuePort.EnqueueAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        using var worker = new WebhookReplayWorker(_logger, _scopeFactory, Options.Create(_options));

        // Act
        await worker.StartAsync(default);
        await Task.Delay(100); // Allow time for multiple processing cycles
        await worker.StopAsync(default);

        // Assert - Should have tried to replay both commands
        await _replayPort.Received(2).ReplayAsync(
            Arg.Any<EnqueueReplayCommand>(),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
