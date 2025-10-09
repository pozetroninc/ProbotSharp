// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Collections.Generic;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.UseCases;

/// <summary>
/// Handles replaying webhook deliveries that previously failed or need reprocessing.
/// Validates idempotency, retries transient failures, and delegates to the primary webhook processing use case.
/// </summary>
public sealed class ReplayWebhookUseCase : IReplayWebhookPort
{
    private const int DefaultMaxAttempts = 5;

    private readonly IWebhookProcessingPort _processingPort;
    private readonly IWebhookReplayQueuePort _queue;
    private readonly IWebhookStoragePort _storage;
    private readonly ILoggingPort _logger;
    private readonly int _maxAttempts;

    /// <summary>
    /// Creates a new instance of <see cref="ReplayWebhookUseCase"/>.
    /// </summary>
    public ReplayWebhookUseCase(
        IWebhookProcessingPort processingPort,
        IWebhookReplayQueuePort queue,
        IWebhookStoragePort storage,
        ILoggingPort logger,
        int maxAttempts = DefaultMaxAttempts)
    {
        ArgumentNullException.ThrowIfNull(processingPort);
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(logger);

        _processingPort = processingPort;
        _queue = queue;
        _storage = storage;
        _logger = logger;
        _maxAttempts = maxAttempts > 0 ? maxAttempts : DefaultMaxAttempts;
    }

    /// <inheritdoc />
    public async Task<Result> ReplayAsync(EnqueueReplayCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var deliveryId = command.Command.DeliveryId;
        var attempt = command.Attempt;

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["delivery_id"] = deliveryId.Value,
            ["attempt"] = attempt + 1,
            ["max_attempts"] = _maxAttempts
        });

        _logger.LogInformation(
            "Processing replay for delivery {DeliveryId} (attempt {Attempt}/{MaxAttempts})",
            deliveryId.Value,
            attempt + 1,
            _maxAttempts);

        // Step 1: Ensure we do not duplicate work if the delivery already exists.
        var existingResult = await _storage.GetAsync(deliveryId, cancellationToken)
            .ConfigureAwait(false);
        if (!existingResult.IsSuccess)
        {
            _logger.LogWarning(
                "Failed to check existing delivery {DeliveryId}: {Error}",
                deliveryId.Value,
                existingResult.Error?.ToString() ?? "unknown error");

            return existingResult.Error is null
                ? Result.Failure("replay_storage_check_failed", "Unable to verify delivery before replay")
                : Result.Failure(existingResult.Error.Value);
        }

        if (existingResult.Value is not null)
        {
            _logger.LogInformation("Delivery {DeliveryId} already stored; skipping", deliveryId.Value);
            return Result.Success();
        }

        // Step 2: Delegate to the primary processing use case.
        var processResult = await _processingPort.ProcessAsync(command.Command, cancellationToken)
            .ConfigureAwait(false);
        if (processResult.IsSuccess)
        {
            _logger.LogInformation("Replay succeeded for delivery {DeliveryId}", deliveryId.Value);
            return Result.Success();
        }

        // Step 3: Manage retry scheduling.
        if (attempt + 1 >= _maxAttempts)
        {
            _logger.LogError(
                null,
                "Replay failed for delivery {DeliveryId}; max attempts reached ({MaxAttempts}). Error: {Error}",
                deliveryId.Value,
                _maxAttempts,
                processResult.Error?.ToString() ?? "unknown error");

            return processResult.Error is null
                ? Result.Failure("replay_max_attempts", "Maximum replay attempts reached")
                : Result.Failure("replay_max_attempts", processResult.Error.Value.Message, processResult.Error.Value.Details);
        }

        var requeueResult = await _queue.EnqueueAsync(command.NextAttempt(), cancellationToken)
            .ConfigureAwait(false);
        if (!requeueResult.IsSuccess)
        {
            _logger.LogError(
                null,
                "Failed to re-enqueue delivery {DeliveryId}: {Error}",
                deliveryId.Value,
                requeueResult.Error?.ToString() ?? "unknown error");

            return requeueResult.Error is null
                ? Result.Failure("replay_enqueue_failed", "Unable to enqueue replay command for retry")
                : Result.Failure(requeueResult.Error.Value);
        }

        _logger.LogWarning(
            "Replay for delivery {DeliveryId} scheduled for retry (attempt {NextAttempt}/{MaxAttempts})",
            deliveryId.Value,
            attempt + 2,
            _maxAttempts);

        return Result.Failure(
            "replay_retry_scheduled",
            $"Replay scheduled to retry (attempt {attempt + 2} of {_maxAttempts}).",
            processResult.Error?.ToString());
    }
}
