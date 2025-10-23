// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Linq;

using NSubstitute;

using ProbotSharp.Application.Abstractions;
using ProbotSharp.Domain.Contracts;
using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Application.Services;
using ProbotSharp.Application.UseCases;
using ProbotSharp.Application.WorkflowStates;
using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.Services;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Application.Tests.UseCases;

public class ProcessWebhookUseCaseTests
{
    private readonly IWebhookStoragePort _storage = Substitute.For<IWebhookStoragePort>();
    private readonly IIdempotencyPort _idempotency = Substitute.For<IIdempotencyPort>();
    private readonly IClockPort _clock = Substitute.For<IClockPort>();
    private readonly IUnitOfWorkPort _unitOfWork = Substitute.For<IUnitOfWorkPort>();
    private readonly IAppConfigurationPort _appConfig = Substitute.For<IAppConfigurationPort>();
    private readonly WebhookSignatureValidator _signatureValidator = new();
    private readonly ITracingPort _tracing = Substitute.For<ITracingPort>();
    private readonly IMetricsPort _metrics = Substitute.For<IMetricsPort>();
    private readonly IProbotSharpContextFactory _contextFactory = Substitute.For<IProbotSharpContextFactory>();
    private readonly EventRouter _eventRouter = new(Substitute.For<ILogger<EventRouter>>());
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    public ProcessWebhookUseCaseTests()
    {
        _unitOfWork.ExecuteAsync(Arg.Any<Func<CancellationToken, Task<Result>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var callback = callInfo.Arg<Func<CancellationToken, Task<Result>>>();
                return callback(default);
            });
    }

    private ProcessWebhookUseCase CreateSut()
        => new(_storage, _idempotency, _clock, _unitOfWork, _appConfig, _signatureValidator, _tracing, _metrics, _contextFactory, _eventRouter, _serviceProvider);

    [Fact]
    public async Task ProcessAsync_WhenDeliveryExists_ShouldReturnSuccessWithoutSaving()
    {
        var command = CreateCommand();
        var deliveryResult = WebhookDelivery.Create(
            command.DeliveryId,
            command.EventName,
            DateTimeOffset.UtcNow,
            command.Payload,
            command.InstallationId);
        var existingDelivery = deliveryResult.Value!;

        _appConfig.GetWebhookSecretAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("test-secret")));
        _storage.GetAsync(command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<WebhookDelivery?>.Success(existingDelivery)));

        var result = await CreateSut().ProcessAsync(command);

        result.IsSuccess.Should().BeTrue();
        await _storage.DidNotReceive().SaveAsync(Arg.Any<WebhookDelivery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenGetReturnsFailure_ShouldPropagateFailure()
    {
        var command = CreateCommand();
        var failure = Result<WebhookDelivery?>.Failure("storage", "failure");

        _appConfig.GetWebhookSecretAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("test-secret")));
        _storage.GetAsync(command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(failure));

        var result = await CreateSut().ProcessAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        await _storage.DidNotReceive().SaveAsync(Arg.Any<WebhookDelivery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenGetReturnsFailureWithoutError_ShouldReturnDefaultStorageFailure()
    {
        var command = CreateCommand();
        var failure = new Result<WebhookDelivery?>(false, null, null);

        _appConfig.GetWebhookSecretAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("test-secret")));
        _storage.GetAsync(command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(failure));

        var result = await CreateSut().ProcessAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("storage_read_failed");
        await _storage.DidNotReceive().SaveAsync(Arg.Any<WebhookDelivery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenSaveFails_ShouldReturnFailure()
    {
        var command = CreateCommand();
        _appConfig.GetWebhookSecretAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("test-secret")));
        _storage.GetAsync(command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<WebhookDelivery?>.Success(null)));
        _storage.SaveAsync(Arg.Any<WebhookDelivery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure("save_failed", "Unable to save"));
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var result = await CreateSut().ProcessAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessAsync_WhenSignatureIsInvalid_ShouldReturnFailure()
    {
        var command = CreateCommand(signatureValid: false);
        _appConfig.GetWebhookSecretAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("test-secret")));

        var result = await CreateSut().ProcessAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("webhook_signature_invalid");
        await _storage.DidNotReceive().GetAsync(Arg.Any<DeliveryId>(), Arg.Any<CancellationToken>());
        await _storage.DidNotReceive().SaveAsync(Arg.Any<WebhookDelivery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenSecretIsUnavailable_ShouldReturnFailure()
    {
        var command = CreateCommand();
        _appConfig.GetWebhookSecretAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Failure("config_error", "Cannot retrieve secret")));

        var result = await CreateSut().ProcessAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("config_error");
        await _storage.DidNotReceive().GetAsync(Arg.Any<DeliveryId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenSecretIsEmpty_ShouldReturnFailure()
    {
        var command = CreateCommand();
        _appConfig.GetWebhookSecretAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("")));

        var result = await CreateSut().ProcessAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("webhook_secret_empty");
        await _storage.DidNotReceive().GetAsync(Arg.Any<DeliveryId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenValidSignatureAndNewDelivery_ShouldSaveSuccessfully()
    {
        var command = CreateCommand();
        _appConfig.GetWebhookSecretAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("test-secret")));
        _storage.GetAsync(command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<WebhookDelivery?>.Success(null)));
        _storage.SaveAsync(Arg.Any<WebhookDelivery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _idempotency.TryAcquireAsync(Arg.Any<IdempotencyKey>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var result = await CreateSut().ProcessAsync(command);

        result.IsSuccess.Should().BeTrue();
        await _storage.Received(1).SaveAsync(Arg.Is<WebhookDelivery>(d =>
            d.Id == command.DeliveryId &&
            d.EventName == command.EventName),
            Arg.Any<CancellationToken>());
    }

    private static ProcessWebhookCommand CreateCommand(bool signatureValid = true, string secret = "test-secret")
    {
        const string payloadJson = "{\"ok\":true}";
        var signature = signatureValid
            ? ComputeSignature(payloadJson, secret)
            : "sha256=" + new string('a', 64);

        return new ProcessWebhookCommand(
            DeliveryId.Create(Guid.NewGuid().ToString()),
            WebhookEventName.Create("push"),
            WebhookPayload.Create(payloadJson),
            InstallationId.Create(1),
            WebhookSignature.Create(signature),
            payloadJson);
    }

    [Fact]
    public async Task ProcessAsync_WhenCommandIsNull_ShouldThrowArgumentNullException()
    {
        var sut = CreateSut();

        var act = () => sut.ProcessAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ProcessAsync_WhenUnhandledExceptionOccurs_ShouldPropagateException()
    {
        var command = CreateCommand();
        _appConfig.GetWebhookSecretAsync(Arg.Any<CancellationToken>())
            .Returns<Task<Result<string>>>(_ => throw new InvalidOperationException("Unexpected error"));

        var act = () => CreateSut().ProcessAsync(command);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ProcessAsync_WhenStorageThrowsException_ShouldPropagateException()
    {
        var command = CreateCommand();
        _appConfig.GetWebhookSecretAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("test-secret")));
        _storage.GetAsync(command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns<Task<Result<WebhookDelivery?>>>(_ => throw new InvalidOperationException("Storage error"));

        var act = () => CreateSut().ProcessAsync(command);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ProcessAsync_WhenUnitOfWorkExecutes_ShouldCallExpectedPorts()
    {
        var command = CreateCommand();
        _appConfig.GetWebhookSecretAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("test-secret")));
        _storage.GetAsync(command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<WebhookDelivery?>.Success(null)));
        _storage.SaveAsync(Arg.Any<WebhookDelivery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _idempotency.TryAcquireAsync(Arg.Any<IdempotencyKey>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        await CreateSut().ProcessAsync(command);

        await _appConfig.Received(1).GetWebhookSecretAsync(Arg.Any<CancellationToken>());
        await _storage.Received(1).GetAsync(command.DeliveryId, Arg.Any<CancellationToken>());
        await _storage.Received(1).SaveAsync(Arg.Any<WebhookDelivery>(), Arg.Any<CancellationToken>());
    }


    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Fact]
    public async Task ValidateSignatureAsync_WithValidSignature_ShouldReturnValidatedWebhook()
    {
        // Arrange
        var command = CreateCommand();
        var untrusted = new UntrustedWebhook(command);

        _appConfig.GetWebhookSecretAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("test-secret")));

        var useCase = CreateSut();

        // Use reflection to access private method
        var method = typeof(ProcessWebhookUseCase).GetMethod(
            "ValidateSignatureAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var resultTask = method!.Invoke(useCase, new object[] { untrusted, CancellationToken.None });
        var result = await (Task<Result<ValidatedWebhook>>)resultTask!;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Command.Should().Be(command);
    }

    [Fact]
    public async Task ValidateSignatureAsync_WithInvalidSignature_ShouldReturnFailure()
    {
        // Arrange
        var command = CreateCommand(signatureValid: false);
        var untrusted = new UntrustedWebhook(command);

        _appConfig.GetWebhookSecretAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("test-secret")));

        var useCase = CreateSut();

        // Use reflection to access private method
        var method = typeof(ProcessWebhookUseCase).GetMethod(
            "ValidateSignatureAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var resultTask = method!.Invoke(useCase, new object[] { untrusted, CancellationToken.None });
        var result = await (Task<Result<ValidatedWebhook>>)resultTask!;

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("webhook_signature_invalid");
        result.Error!.Value.Message.Should().Contain("signature validation failed");
    }

    [Fact]
    public async Task CheckForDuplicateAsync_WhenNotDuplicate_ShouldReturnVerifiedUniqueWebhook()
    {
        // Arrange
        var command = CreateCommand();
        var untrusted = new UntrustedWebhook(command);
        var validated = new ValidatedWebhook(untrusted);

        _storage.GetAsync(command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<WebhookDelivery?>.Success(null)));

        var useCase = CreateSut();

        // Use reflection to access private method
        var method = typeof(ProcessWebhookUseCase).GetMethod(
            "CheckForDuplicateAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var resultTask = method!.Invoke(useCase, new object[] { validated, CancellationToken.None });
        var result = await (Task<Result<VerifiedUniqueWebhook>>)resultTask!;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Command.Should().Be(command);
    }

    [Fact]
    public async Task CheckForDuplicateAsync_WhenDuplicate_ShouldReturnFailureWithDuplicateCode()
    {
        // Arrange
        var command = CreateCommand();
        var untrusted = new UntrustedWebhook(command);
        var validated = new ValidatedWebhook(untrusted);

        // Create existing delivery to simulate duplicate
        var existingDeliveryResult = WebhookDelivery.Create(
            command.DeliveryId,
            command.EventName,
            DateTimeOffset.UtcNow,
            command.Payload,
            command.InstallationId);
        var existingDelivery = existingDeliveryResult.Value!;

        _storage.GetAsync(command.DeliveryId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<WebhookDelivery?>.Success(existingDelivery)));

        var useCase = CreateSut();

        // Use reflection to access private method
        var method = typeof(ProcessWebhookUseCase).GetMethod(
            "CheckForDuplicateAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var resultTask = method!.Invoke(useCase, new object[] { validated, CancellationToken.None });
        var result = await (Task<Result<VerifiedUniqueWebhook>>)resultTask!;

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("webhook_duplicate_delivery");
        result.Error!.Value.Message.Should().Contain(command.DeliveryId.Value);
    }

    [Fact]
    public async Task PersistDeliveryAsync_WithValidDelivery_ShouldReturnPersistedWebhook()
    {
        // Arrange
        var command = CreateCommand();
        var untrusted = new UntrustedWebhook(command);
        var validated = new ValidatedWebhook(untrusted);
        var unique = new VerifiedUniqueWebhook(validated);

        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        _storage.SaveAsync(Arg.Any<WebhookDelivery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _idempotency.TryAcquireAsync(Arg.Any<IdempotencyKey>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var useCase = CreateSut();

        // Use reflection to access private method
        var method = typeof(ProcessWebhookUseCase).GetMethod(
            "PersistDeliveryAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var resultTask = method!.Invoke(useCase, new object[] { unique, CancellationToken.None });
        var result = await (Task<Result<PersistedWebhook>>)resultTask!;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Command.Should().Be(command);
        result.Value!.Delivery.Should().NotBeNull();
        result.Value!.Delivery.Id.Should().Be(command.DeliveryId);

        await _storage.Received(1).SaveAsync(
            Arg.Any<WebhookDelivery>(),
            Arg.Any<CancellationToken>());

        // Verify that idempotency key was set after successful save
        await _idempotency.Received(1).TryAcquireAsync(
            Arg.Is<IdempotencyKey>(k => k.Value == IdempotencyKey.FromDeliveryId(command.DeliveryId).Value),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PersistDeliveryAsync_WhenDeliveryCreationFails_ShouldReturnFailure()
    {
        // Arrange - Create command with valid signature but will fail WebhookDelivery.Create
        var command = new ProcessWebhookCommand(
            DeliveryId: DeliveryId.Create("test-123"),
            EventName: WebhookEventName.Create("issues"),
            Payload: WebhookPayload.Create("{}"),
            InstallationId: InstallationId.Create(123),
            Signature: WebhookSignature.Create("sha256=" + new string('a', 64)),
            RawPayload: "{}");

        var untrusted = new UntrustedWebhook(command);
        var validated = new ValidatedWebhook(untrusted);
        var unique = new VerifiedUniqueWebhook(validated);

        // Set clock to return default DateTime which will fail WebhookDelivery.Create validation
        _clock.UtcNow.Returns(default(DateTimeOffset));

        var useCase = CreateSut();

        // Use reflection to access private method
        var method = typeof(ProcessWebhookUseCase).GetMethod(
            "PersistDeliveryAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var resultTask = method!.Invoke(useCase, new object[] { unique, CancellationToken.None });
        var result = await (Task<Result<PersistedWebhook>>)resultTask!;

        // Assert - Should return failure Result, not throw
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("webhook_delivery_creation_failed");
        result.Error!.Value.Message.Should().Contain("DeliveredAt");

        await _storage.DidNotReceive().SaveAsync(
            Arg.Any<WebhookDelivery>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RouteToHandlersAsync_ShouldCreateContextAndRoute()
    {
        // Arrange
        var command = CreateCommand();
        var delivery = CreateWebhookDelivery(command);
        var untrusted = new UntrustedWebhook(command);
        var validated = new ValidatedWebhook(untrusted);
        var unique = new VerifiedUniqueWebhook(validated);
        var persisted = new PersistedWebhook(unique, delivery);

        // Create a real context using mocks for dependencies
        var mockLogger = Substitute.For<ILogger>();
        var mockGitHubClient = Substitute.For<Octokit.IGitHubClient>();
        var mockGraphQLClient = Substitute.For<ProbotSharp.Domain.Contracts.IGitHubGraphQlClient>();
        var context = new ProbotSharp.Domain.Context.ProbotSharpContext(
            delivery.Id.Value,
            delivery.EventName.Value,
            null,
            JObject.Parse(delivery.Payload.RawBody),
            mockLogger,
            mockGitHubClient,
            mockGraphQLClient,
            null,
            null,
            false);

        _contextFactory.CreateAsync(Arg.Any<WebhookDelivery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(context));

        var useCase = CreateSut();

        // Use reflection to access private method
        var method = typeof(ProcessWebhookUseCase).GetMethod(
            "RouteToHandlersAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var resultTask = method!.Invoke(useCase, new object[] { persisted, CancellationToken.None });
        var result = await (Task<Result<PersistedWebhook>>)resultTask!;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(persisted);

        await _contextFactory.Received(1).CreateAsync(
            Arg.Is<WebhookDelivery>(d => d.Id == command.DeliveryId),
            Arg.Any<CancellationToken>());

        _tracing.Received(1).AddEvent("webhook.route_to_handlers.start");
        _tracing.Received(1).AddEvent("webhook.route_to_handlers.completed");
    }

    [Fact]
    public async Task RouteToHandlersAsync_WhenRoutingThrowsException_ShouldStillReturnSuccessAndRecordMetrics()
    {
        // Arrange
        var command = CreateCommand();
        var delivery = CreateWebhookDelivery(command);
        var untrusted = new UntrustedWebhook(command);
        var validated = new ValidatedWebhook(untrusted);
        var unique = new VerifiedUniqueWebhook(validated);
        var persisted = new PersistedWebhook(unique, delivery);

        // Make context factory throw an exception to simulate routing error
        var expectedException = new InvalidOperationException("Handler failed");
        _contextFactory.CreateAsync(Arg.Any<WebhookDelivery>(), Arg.Any<CancellationToken>())
            .Returns<Task<ProbotSharp.Domain.Context.ProbotSharpContext>>(_ => throw expectedException);

        var useCase = CreateSut();

        // Use reflection to access private method
        var method = typeof(ProcessWebhookUseCase).GetMethod(
            "RouteToHandlersAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var resultTask = method!.Invoke(useCase, new object[] { persisted, CancellationToken.None });
        var result = await (Task<Result<PersistedWebhook>>)resultTask!;

        // Assert - should still return success because webhook was persisted
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(persisted);

        await _contextFactory.Received(1).CreateAsync(
            Arg.Is<WebhookDelivery>(d => d.Id == command.DeliveryId),
            Arg.Any<CancellationToken>());

        _tracing.Received(1).AddEvent("webhook.route_to_handlers.start");
        _tracing.Received(1).AddEvent("webhook.route_to_handlers.error");
        _metrics.Received(1).IncrementCounter(
            "webhook.routing_error",
            1,
            Arg.Is<KeyValuePair<string, object?>[]>(tags =>
                tags.Any(t => t.Key == "event" && t.Value!.ToString() == command.EventName.Value) &&
                tags.Any(t => t.Key == "exception_type" && t.Value!.ToString() == "InvalidOperationException")));
    }

    private WebhookDelivery CreateWebhookDelivery(ProcessWebhookCommand command)
    {
        var result = WebhookDelivery.Create(
            command.DeliveryId,
            command.EventName,
            DateTimeOffset.UtcNow,
            command.Payload,
            command.InstallationId);
        return result.Value!;
    }
}
