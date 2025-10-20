// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Application.Services;
using ProbotSharp.Application.UseCases;
using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.Services;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Tests.UseCases;

public class ProcessWebhookUseCaseTests
{
    private readonly IWebhookStoragePort _storage = Substitute.For<IWebhookStoragePort>();
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
        => new(_storage, _clock, _unitOfWork, _appConfig, _signatureValidator, _tracing, _metrics, _contextFactory, _eventRouter, _serviceProvider);

    [Fact]
    public async Task ProcessAsync_WhenDeliveryExists_ShouldReturnSuccessWithoutSaving()
    {
        var command = CreateCommand();
        var existingDelivery = WebhookDelivery.Create(
            command.DeliveryId,
            command.EventName,
            DateTimeOffset.UtcNow,
            command.Payload,
            command.InstallationId);

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
}
