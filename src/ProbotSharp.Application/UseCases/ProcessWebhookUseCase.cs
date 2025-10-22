// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Diagnostics;

using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Application.Services;
using ProbotSharp.Application.WorkflowStates;
using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.Services;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.UseCases;

/// <summary>
/// Use case for processing GitHub webhook deliveries.
/// Validates signatures, checks for duplicates, persists deliveries, and routes events to handlers.
/// Implements distributed tracing and metrics collection for observability.
/// </summary>
public sealed class ProcessWebhookUseCase : IWebhookProcessingPort
{
    private readonly IWebhookStoragePort _storage;
    private readonly IIdempotencyPort _idempotency;
    private readonly IClockPort _clock;
    private readonly IUnitOfWorkPort _unitOfWork;
    private readonly IAppConfigurationPort _appConfig;
    private readonly WebhookSignatureValidator _signatureValidator;
    private readonly ITracingPort _tracing;
    private readonly IMetricsPort _metrics;
    private readonly IProbotSharpContextFactory _contextFactory;
    private readonly EventRouter _eventRouter;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessWebhookUseCase"/> class.
    /// </summary>
    /// <param name="storage">The webhook storage port for persisting deliveries.</param>
    /// <param name="idempotency">The idempotency port for distributed locking.</param>
    /// <param name="clock">The clock port for timestamp generation.</param>
    /// <param name="unitOfWork">The unit of work port for transaction management.</param>
    /// <param name="appConfig">The application configuration port for retrieving webhook secret.</param>
    /// <param name="signatureValidator">The webhook signature validator.</param>
    /// <param name="tracing">The tracing port for distributed tracing.</param>
    /// <param name="metrics">The metrics port for observability.</param>
    /// <param name="contextFactory">The context factory for creating ProbotSharp contexts.</param>
    /// <param name="eventRouter">The event router for routing webhooks to handlers.</param>
    /// <param name="serviceProvider">The service provider for dependency resolution.</param>
    public ProcessWebhookUseCase(
        IWebhookStoragePort storage,
        IIdempotencyPort idempotency,
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
        this._storage = storage;
        this._idempotency = idempotency;
        this._clock = clock;
        this._unitOfWork = unitOfWork;
        this._appConfig = appConfig;
        this._signatureValidator = signatureValidator;
        this._tracing = tracing;
        this._metrics = metrics;
        this._contextFactory = contextFactory;
        this._eventRouter = eventRouter;
        this._serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Processes a webhook delivery using a functional pipeline.
    /// Pipeline: UntrustedWebhook → ValidatedWebhook → VerifiedUniqueWebhook → PersistedWebhook → RouteToHandlers.
    /// </summary>
    /// <param name="command">The command containing webhook delivery information.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A result indicating success or failure of webhook processing.</returns>
    public async Task<Result> ProcessAsync(ProcessWebhookCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Start distributed tracing span for webhook processing
        using var activity = this._tracing.StartActivity(
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
            this._metrics.IncrementCounter(
                "webhook.received",
                1,
                [
                    new("event", command.EventName.Value),
                ]);

            using var _ = this._metrics.MeasureDuration(
                "webhook.processing.duration",
                [
                    new("event", command.EventName.Value),
                ]);

            var result = await this._unitOfWork.ExecuteAsync(async ct =>
            {
                // Railway-oriented pipeline: Compose all steps with automatic short-circuiting
                var pipelineResult = await this.ValidateSignatureAsync(new UntrustedWebhook(command), ct)
                    .TapSuccessAsync(_ =>
                    {
                        this._tracing.AddEvent("webhook.signature_validated");
                        return Task.CompletedTask;
                    })
                    .TapFailureAsync(error =>
                    {
                        if (error.Code == "webhook_signature_invalid")
                        {
                            this._metrics.IncrementCounter(
                                "webhook.signature_invalid",
                                1,
                                [new("event", command.EventName.Value)]);
                        }
                        return Task.CompletedTask;
                    })
                    .BindAsync(validated => this.CheckForDuplicateAsync(validated, ct))
                    .TapFailureAsync(error =>
                    {
                        if (error.Code == "webhook_duplicate_delivery")
                        {
                            this._tracing.AddEvent("webhook.duplicate_delivery");
                            this._metrics.IncrementCounter(
                                "webhook.duplicate",
                                1,
                                [new("event", command.EventName.Value)]);
                        }
                        return Task.CompletedTask;
                    })
                    .BindAsync(unique => this.PersistDeliveryAsync(unique, ct))
                    .TapSuccessAsync(_ =>
                    {
                        this._tracing.AddEvent("webhook.delivery_persisted");
                        this._metrics.IncrementCounter(
                            "webhook.processed",
                            1,
                            [new("event", command.EventName.Value)]);
                        return Task.CompletedTask;
                    })
                    .BindAsync(persisted => this.RouteToHandlersAsync(persisted, ct))
                    .TapSuccessAsync(_ =>
                    {
                        this._tracing.AddEvent("webhook.handlers_completed");
                        return Task.CompletedTask;
                    })
                    .ConfigureAwait(false);

                // Handle the special case: duplicate delivery is a success scenario
                if (!pipelineResult.IsSuccess &&
                    pipelineResult.Error?.Code == "webhook_duplicate_delivery")
                {
                    // Duplicate delivery detected - return success (idempotent operation)
                    return Result.Success();
                }

                // Convert Result<PersistedWebhook> to Result
                return pipelineResult.IsSuccess
                    ? Result.Success()
                    : Result.Failure(
                        pipelineResult.Error ?? new Error(
                            "webhook_processing_failed",
                            "Webhook processing failed"));
            }, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                this._tracing.AddTags([new("error", true), new("error.type", result.Error?.Code)]);
                this._metrics.IncrementCounter(
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
            this._tracing.RecordException(ex);
            this._metrics.IncrementCounter(
                "webhook.processing_exception",
                1,
                [
                    new("event", command.EventName.Value),
                    new("exception_type", ex.GetType().Name),
                ]);
            throw;
        }
    }

    /// <summary>
    /// Step 1: Validate webhook signature.
    /// UntrustedWebhook → ValidatedWebhook.
    /// </summary>
    /// <param name="untrusted">The untrusted webhook.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>ValidatedWebhook if signature is valid, otherwise failure.</returns>
    private async Task<Result<ValidatedWebhook>> ValidateSignatureAsync(
        UntrustedWebhook untrusted,
        CancellationToken cancellationToken)
    {
        var command = untrusted.Command;

        // Get the webhook secret from configuration
        var secretResult = await this._appConfig.GetWebhookSecretAsync(cancellationToken).ConfigureAwait(false);
        if (!secretResult.IsSuccess)
        {
            return Result<ValidatedWebhook>.Failure(
                secretResult.Error ?? new Error(
                    "webhook_secret_unavailable",
                    "Unable to retrieve webhook secret for signature validation"));
        }

        var secret = secretResult.Value;
        if (string.IsNullOrWhiteSpace(secret))
        {
            return Result<ValidatedWebhook>.Failure(
                new Error("webhook_secret_empty", "Webhook secret is not configured"));
        }

        // Validate the signature
        var isSignatureValid = this._signatureValidator.IsSignatureValid(
            command.RawPayload,
            secret,
            command.Signature.Value);

        if (!isSignatureValid)
        {
            return Result<ValidatedWebhook>.Failure(
                new Error("webhook_signature_invalid", "Webhook signature validation failed"));
        }

        return Result<ValidatedWebhook>.Success(new ValidatedWebhook(untrusted));
    }

    /// <summary>
    /// Step 2: Check for duplicate delivery (idempotency).
    /// ValidatedWebhook → VerifiedUniqueWebhook.
    /// </summary>
    /// <param name="validated">The validated webhook.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>VerifiedUniqueWebhook if not duplicate, otherwise failure with duplicate code.</returns>
    private async Task<Result<VerifiedUniqueWebhook>> CheckForDuplicateAsync(
        ValidatedWebhook validated,
        CancellationToken cancellationToken)
    {
        var command = validated.Command;

        // Check if delivery already exists in storage
        var existingResult = await this._storage.GetAsync(command.DeliveryId, cancellationToken).ConfigureAwait(false);
        if (!existingResult.IsSuccess)
        {
            return Result<VerifiedUniqueWebhook>.Failure(
                existingResult.Error ?? new Error(
                    "storage_read_failed",
                    "Unable to check for existing webhook delivery"));
        }

        if (existingResult.Value is not null)
        {
            // Special case: duplicate is a valid success scenario, but needs different handling
            // We'll use a custom error code that the orchestrator recognizes
            return Result<VerifiedUniqueWebhook>.Failure(
                new Error(
                    "webhook_duplicate_delivery",
                    $"Webhook delivery {command.DeliveryId.Value} has already been processed"));
        }

        return Result<VerifiedUniqueWebhook>.Success(new VerifiedUniqueWebhook(validated));
    }

    /// <summary>
    /// Step 3: Persist webhook delivery.
    /// VerifiedUniqueWebhook → PersistedWebhook.
    /// </summary>
    /// <param name="unique">The verified unique webhook.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PersistedWebhook if persistence succeeds, otherwise failure.</returns>
    private async Task<Result<PersistedWebhook>> PersistDeliveryAsync(
        VerifiedUniqueWebhook unique,
        CancellationToken cancellationToken)
    {
        var command = unique.Command;

        WebhookDelivery delivery;
        try
        {
            delivery = WebhookDelivery.Create(
                command.DeliveryId,
                command.EventName,
                this._clock.UtcNow,
                command.Payload,
                command.InstallationId);
        }
        catch (ArgumentException ex)
        {
            return Result<PersistedWebhook>.Failure(
                new Error(
                    "webhook_delivery_creation_failed",
                    $"Failed to create webhook delivery entity: {ex.Message}"));
        }

        var saveResult = await this._storage.SaveAsync(delivery, cancellationToken).ConfigureAwait(false);
        if (!saveResult.IsSuccess)
        {
            return Result<PersistedWebhook>.Failure(
                saveResult.Error ?? new Error(
                    "storage_write_failed",
                    "Unable to save webhook delivery"));
        }

        // Mark delivery as processed in idempotency system for distributed duplicate prevention
        var idempotencyKey = IdempotencyKey.FromDeliveryId(command.DeliveryId);
        var acquired = await this._idempotency.TryAcquireAsync(
            idempotencyKey,
            timeToLive: TimeSpan.FromHours(24),
            cancellationToken).ConfigureAwait(false);

        // Note: We don't fail if idempotency key acquisition fails since the delivery
        // has already been persisted. The storage check will prevent true duplicates.
        // This is a defense-in-depth mechanism for distributed scenarios.

        return Result<PersistedWebhook>.Success(new PersistedWebhook(unique, delivery));
    }

    /// <summary>
    /// Step 4: Route to event handlers.
    /// PersistedWebhook → PersistedWebhook (terminal step).
    /// </summary>
    /// <param name="persisted">The persisted webhook.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Same PersistedWebhook after routing completes.</returns>
    private async Task<Result<PersistedWebhook>> RouteToHandlersAsync(
        PersistedWebhook persisted,
        CancellationToken cancellationToken)
    {
        var delivery = persisted.Delivery;

        // Add tracing event for routing lifecycle
        this._tracing.AddEvent("webhook.route_to_handlers.start");

        try
        {
            var context = await this._contextFactory.CreateAsync(delivery, cancellationToken).ConfigureAwait(false);
            await this._eventRouter.RouteAsync(context, this._serviceProvider, cancellationToken).ConfigureAwait(false);

            this._tracing.AddEvent("webhook.route_to_handlers.completed");
        }
        catch (Exception ex)
        {
            // Note: Handler failures should not cause webhook processing to fail
            // The webhook has been successfully persisted at this point
            this._tracing.AddEvent("webhook.route_to_handlers.error");
            this._metrics.IncrementCounter(
                "webhook.routing_error",
                1,
                [
                    new("event", delivery.EventName.Value),
                    new("exception_type", ex.GetType().Name),
                ]);
        }

        return Result<PersistedWebhook>.Success(persisted);
    }
}
