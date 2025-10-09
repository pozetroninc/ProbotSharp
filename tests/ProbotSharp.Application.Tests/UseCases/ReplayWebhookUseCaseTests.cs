// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using NSubstitute;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Application.UseCases;
using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Tests.UseCases;

public sealed class ReplayWebhookUseCaseTests
{
    private readonly IWebhookProcessingPort _processingPort = Substitute.For<IWebhookProcessingPort>();
    private readonly IWebhookReplayQueuePort _queue = Substitute.For<IWebhookReplayQueuePort>();
    private readonly IWebhookStoragePort _storage = Substitute.For<IWebhookStoragePort>();
    private readonly ILoggingPort _logger = Substitute.For<ILoggingPort>();

    private ReplayWebhookUseCase CreateSut(int maxAttempts = 3)
        => new(_processingPort, _queue, _storage, _logger, maxAttempts);

    [Fact]
    public async Task ReplayAsync_WhenDeliveryAlreadyPersisted_ShouldReturnSuccessWithoutProcessing()
    {
        var command = CreateReplayCommand();
        _storage.GetAsync(command.Command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Result<WebhookDelivery?>.Success(WebhookDelivery.Create(
                command.Command.DeliveryId,
                command.Command.EventName,
                DateTimeOffset.UtcNow,
                command.Command.Payload,
                command.Command.InstallationId)));

        var result = await CreateSut().ReplayAsync(command);

        result.IsSuccess.Should().BeTrue();
        await _processingPort.DidNotReceive().ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_WhenProcessingSucceeds_ShouldReturnSuccess()
    {
        var command = CreateReplayCommand();
        _storage.GetAsync(command.Command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Result<WebhookDelivery?>.Success(null));
        _processingPort.ProcessAsync(command.Command, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await CreateSut().ReplayAsync(command);

        result.IsSuccess.Should().BeTrue();
        await _queue.DidNotReceive().EnqueueAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_WhenProcessingFails_ShouldEnqueueRetry()
    {
        var command = CreateReplayCommand();
        _storage.GetAsync(command.Command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Result<WebhookDelivery?>.Success(null));
        _processingPort.ProcessAsync(command.Command, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("processing_failed", "fail"));
        _queue.EnqueueAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await CreateSut().ReplayAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("replay_retry_scheduled");
        await _queue.Received(1).EnqueueAsync(Arg.Is<EnqueueReplayCommand>(c => c.Attempt == command.Attempt + 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_WhenProcessingFailsAndMaxAttemptsReached_ShouldReturnFailure()
    {
        var command = new EnqueueReplayCommand(CreateProcessCommand(), attempt: 2);
        _storage.GetAsync(command.Command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Result<WebhookDelivery?>.Success(null));
        _processingPort.ProcessAsync(command.Command, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("processing_failed", "fail"));

        var result = await CreateSut(maxAttempts: 3).ReplayAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("replay_max_attempts");
        await _queue.DidNotReceive().EnqueueAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_WhenRequeueFails_ShouldReturnFailure()
    {
        var command = CreateReplayCommand();
        _storage.GetAsync(command.Command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Result<WebhookDelivery?>.Success(null));
        _processingPort.ProcessAsync(command.Command, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("processing_failed", "fail"));
        _queue.EnqueueAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure("enqueue_failed", "boom"));

        var result = await CreateSut().ReplayAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("enqueue_failed");
    }

    [Fact]
    public async Task ReplayAsync_WhenCommandIsNull_ShouldThrowArgumentNullException()
    {
        var sut = CreateSut();

        var act = () => sut.ReplayAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReplayAsync_WhenStorageCheckFails_ShouldReturnFailure()
    {
        var command = CreateReplayCommand();
        _storage.GetAsync(command.Command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Result<WebhookDelivery?>.Failure("storage_error", "Storage unavailable"));

        var result = await CreateSut().ReplayAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("storage_error");
        await _processingPort.DidNotReceive().ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_WhenStorageCheckFailsWithoutError_ShouldReturnDefaultFailure()
    {
        var command = CreateReplayCommand();
        _storage.GetAsync(command.Command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(new Result<WebhookDelivery?>(false, null, null));

        var result = await CreateSut().ReplayAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("replay_storage_check_failed");
    }

    [Fact]
    public async Task ReplayAsync_WhenProcessingFailsWithoutError_ShouldUseDefaultError()
    {
        var command = CreateReplayCommand();
        _storage.GetAsync(command.Command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Result<WebhookDelivery?>.Success(null));
        _processingPort.ProcessAsync(command.Command, Arg.Any<CancellationToken>())
            .Returns(new Result(false, null));
        _queue.EnqueueAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await CreateSut().ReplayAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("replay_retry_scheduled");
    }

    [Fact]
    public async Task ReplayAsync_WhenRequeueFails_ShouldLogAndReturnFailure()
    {
        var command = CreateReplayCommand();
        _storage.GetAsync(command.Command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Result<WebhookDelivery?>.Success(null));
        _processingPort.ProcessAsync(command.Command, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("processing_failed", "fail"));
        _queue.EnqueueAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>())
            .Returns(new Result(false, null));

        var result = await CreateSut().ReplayAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("replay_enqueue_failed");
    }

    [Fact]
    public async Task ReplayAsync_WhenAttemptEqualsMaxMinus1_ShouldNotRequeue()
    {
        var command = new EnqueueReplayCommand(CreateProcessCommand(), attempt: 2);
        _storage.GetAsync(command.Command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Result<WebhookDelivery?>.Success(null));
        _processingPort.ProcessAsync(command.Command, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("processing_failed", "fail"));

        var result = await CreateSut(maxAttempts: 3).ReplayAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("replay_max_attempts");
        await _queue.DidNotReceive().EnqueueAsync(Arg.Any<EnqueueReplayCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_WhenMaxAttemptsReachedWithoutError_ShouldUseDefaultError()
    {
        var command = new EnqueueReplayCommand(CreateProcessCommand(), attempt: 2);
        _storage.GetAsync(command.Command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Result<WebhookDelivery?>.Success(null));
        _processingPort.ProcessAsync(command.Command, Arg.Any<CancellationToken>())
            .Returns(new Result(false, null));

        var result = await CreateSut(maxAttempts: 3).ReplayAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("replay_max_attempts");
        result.Error!.Value.Message.Should().Be("Maximum replay attempts reached");
    }

    [Fact]
    public async Task Constructor_WhenMaxAttemptsIsZero_ShouldUseDefaultMaxAttempts()
    {
        var processingPort = Substitute.For<IWebhookProcessingPort>();
        var queue = Substitute.For<IWebhookReplayQueuePort>();
        var storage = Substitute.For<IWebhookStoragePort>();
        var logger = Substitute.For<ILoggingPort>();

        var sut = new ReplayWebhookUseCase(processingPort, queue, storage, logger, maxAttempts: 0);

        var command = new EnqueueReplayCommand(CreateProcessCommand(), attempt: 4);
        storage.GetAsync(command.Command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Result<WebhookDelivery?>.Success(null));
        processingPort.ProcessAsync(command.Command, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("processing_failed", "fail"));

        var result = await sut.ReplayAsync(command);

        result.Error!.Value.Code.Should().Be("replay_max_attempts");
    }

    [Fact]
    public async Task Constructor_WhenMaxAttemptsIsNegative_ShouldUseDefaultMaxAttempts()
    {
        var processingPort = Substitute.For<IWebhookProcessingPort>();
        var queue = Substitute.For<IWebhookReplayQueuePort>();
        var storage = Substitute.For<IWebhookStoragePort>();
        var logger = Substitute.For<ILoggingPort>();

        var sut = new ReplayWebhookUseCase(processingPort, queue, storage, logger, maxAttempts: -1);

        var command = new EnqueueReplayCommand(CreateProcessCommand(), attempt: 4);
        storage.GetAsync(command.Command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Result<WebhookDelivery?>.Success(null));
        processingPort.ProcessAsync(command.Command, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("processing_failed", "fail"));

        var result = await sut.ReplayAsync(command);

        result.Error!.Value.Code.Should().Be("replay_max_attempts");
    }

    private static EnqueueReplayCommand CreateReplayCommand(int attempt = 0)
        => new(CreateProcessCommand(), attempt);

    private static ProcessWebhookCommand CreateProcessCommand()
        => new(
            DeliveryId.Create(Guid.NewGuid().ToString()),
            WebhookEventName.Create("push"),
            WebhookPayload.Create("{\"ok\":true}"),
            InstallationId.Create(1),
            WebhookSignature.Create("sha256=" + new string('a', 64)),
            "{\"ok\":true}");
}
