// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Diagnostics;

using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.Services;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.UseCases;

public sealed class ProcessWebhookUseCase : IWebhookProcessingPort
{
    private readonly IWebhookStoragePort _storage;
    private readonly IClockPort _clock;
    private readonly IUnitOfWorkPort _unitOfWork;
    private readonly IAppConfigurationPort _appConfig;
    private readonly WebhookSignatureValidator _signatureValidator;
    private readonly ITracingPort _tracing;
    private readonly IMetricsPort _metrics;
    private readonly IProbotSharpContextFactory _contextFactory;
    private readonly EventRouter _eventRouter;
    private readonly IServiceProvider _serviceProvider;

    public ProcessWebhookUseCase(
        IWebhookStoragePort storage,
        IClockPort clock,
        IUnitOfWorkPort unitOfWork,
        IAppConfigurationPort appConfig,
        WebhookSignatureValidator signatureValidator,
        ITracingPort tracing,
        IMetricsPort metrics,
        IProbotSharpContextFactory contextFactory,
        EventRouter eventRouter,
        IServiceProvider serviceProvider)
    {
        _storage = storage;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _appConfig = appConfig;
        _signatureValidator = signatureValidator;
        _tracing = tracing;
        _metrics = metrics;
        _contextFactory = contextFactory;
        _eventRouter = eventRouter;
        _serviceProvider = serviceProvider;
    }

    public async Task<Result> ProcessAsync(ProcessWebhookCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Start distributed tracing span for webhook processing
        using var activity = _tracing.StartActivity(
            "webhook.process",
            ActivityKind.Server,
            tags:
            [
                new("webhook.event", command.EventName.Value),
                new("webhook.delivery_id", command.DeliveryId.Value),
                new("webhook.installation_id", command.InstallationId?.Value),
            ]);

        try
        {
            // Record metrics for webhook received
            _metrics.IncrementCounter(
                "webhook.received",
                1,
                [
                    new("event", command.EventName.Value),
                ]);

            using var _ = _metrics.MeasureDuration(
                "webhook.processing.duration",
                [
                    new("event", command.EventName.Value),
                ]);

            var result = await _unitOfWork.ExecuteAsync(async ct =>
            {
                // Step 1: Validate webhook signature (security first)
                _tracing.AddEvent("webhook.validate_signature");
                var secretResult = await _appConfig.GetWebhookSecretAsync(ct).ConfigureAwait(false);
                if (!secretResult.IsSuccess)
                {
                    return secretResult.Error is null
                        ? Result.Failure("webhook_secret_unavailable", "Unable to retrieve webhook secret for signature validation")
                        : Result.Failure(secretResult.Error.Value);
                }

                var secret = secretResult.Value;
                if (string.IsNullOrWhiteSpace(secret))
                {
                    return Result.Failure("webhook_secret_empty", "Webhook secret is not configured");
                }

                var isSignatureValid = _signatureValidator.IsSignatureValid(
                    command.RawPayload,
                    secret,
                    command.Signature.Value);

                if (!isSignatureValid)
                {
                    _tracing.AddEvent("webhook.signature_invalid");
                    _metrics.IncrementCounter(
                        "webhook.signature_invalid",
                        1,
                        [
                            new("event", command.EventName.Value),
                        ]);
                    return Result.Failure("webhook_signature_invalid", "Webhook signature validation failed");
                }

                // Step 2: Check for duplicate delivery (idempotency)
                _tracing.AddEvent("webhook.check_duplicate");
                var existingResult = await _storage.GetAsync(command.DeliveryId, ct).ConfigureAwait(false);
                if (!existingResult.IsSuccess)
                {
                    return existingResult.Error is null
                        ? Result.Failure("storage_read_failed", "Unable to check for existing webhook delivery")
                        : Result.Failure(existingResult.Error.Value);
                }

                if (existingResult.Value is not null)
                {
                    // Duplicate delivery detected - return success (idempotent operation)
                    _tracing.AddEvent("webhook.duplicate_delivery");
                    _metrics.IncrementCounter(
                        "webhook.duplicate",
                        1,
                        [
                            new("event", command.EventName.Value),
                        ]);
                    return Result.Success();
                }

                // Step 3: Process and save webhook delivery
                _tracing.AddEvent("webhook.save_delivery");
                var delivery = WebhookDelivery.Create(
                    command.DeliveryId,
                    command.EventName,
                    _clock.UtcNow,
                    command.Payload,
                    command.InstallationId);

                var saveResult = await _storage.SaveAsync(delivery, ct).ConfigureAwait(false);
                if (!saveResult.IsSuccess)
                {
                    return saveResult.Error is null
                        ? Result.Failure("storage_write_failed", "Unable to save webhook delivery")
                        : Result.Failure(saveResult.Error.Value);
                }

                _metrics.IncrementCounter(
                    "webhook.processed",
                    1,
                    [
                        new("event", command.EventName.Value),
                    ]);

                // Step 4: Route event to registered handlers
                _tracing.AddEvent("webhook.route_to_handlers");
                try
                {
                    var context = await _contextFactory.CreateAsync(delivery, ct).ConfigureAwait(false);
                    await _eventRouter.RouteAsync(context, _serviceProvider, ct).ConfigureAwait(false);
                    _tracing.AddEvent("webhook.handlers_completed");
                }
                catch (Exception routingEx)
                {
                    // Log routing errors but don't fail the webhook processing
                    // The webhook has been successfully persisted at this point
                    _tracing.AddEvent("webhook.routing_error");
                    _metrics.IncrementCounter(
                        "webhook.routing_error",
                        1,
                        [
                            new("event", command.EventName.Value),
                            new("exception_type", routingEx.GetType().Name),
                        ]);

                    // Note: We still return success because the webhook was persisted successfully
                    // Handler failures should not cause webhook processing to fail
                }

                return Result.Success();
            }, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                _tracing.AddTags([new("error", true), new("error.type", result.Error?.Code)]);
                _metrics.IncrementCounter(
                    "webhook.processing_failed",
                    1,
                    [
                        new("event", command.EventName.Value),
                        new("error_code", result.Error?.Code),
                    ]);
            }

            return result;
        }
        catch (Exception ex)
        {
            _tracing.RecordException(ex);
            _metrics.IncrementCounter(
                "webhook.processing_exception",
                1,
                [
                    new("event", command.EventName.Value),
                    new("exception_type", ex.GetType().Name),
                ]);
            throw;
        }
    }
}

