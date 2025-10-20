// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Ports.Outbound;

#pragma warning disable CA1848 // Performance: LoggerMessage delegates - not performance-critical for this codebase

namespace ProbotSharp.Adapters.Workers;

/// <summary>
/// Background worker that processes failed webhook deliveries from the replay queue.
/// Implements exponential backoff retry logic and moves failed deliveries to dead-letter queue after max retries.
/// </summary>
public sealed class WebhookReplayWorker : BackgroundService
{
    private readonly ILogger<WebhookReplayWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WebhookReplayWorkerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookReplayWorker"/> class.
    /// </summary>
    /// <param name="logger">The logger for recording worker operations.</param>
    /// <param name="scopeFactory">The service scope factory for creating scoped services per webhook replay.</param>
    /// <param name="options">The configuration options for the webhook replay worker.</param>
    public WebhookReplayWorker(
        ILogger<WebhookReplayWorker> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<WebhookReplayWorkerOptions> options)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);

        this._logger = logger;
        this._scopeFactory = scopeFactory;
        this._options = options.Value;
    }

    /// <summary>
    /// Executes the webhook replay worker background service.
    /// Continuously polls the replay queue and processes failed webhooks with retry logic.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token to stop the worker.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromMilliseconds(this._options.PollIntervalMs);
        this._logger.LogInformation(
            "Webhook replay worker started - Poll interval: {PollIntervalMs}ms, Max retries: {MaxRetries}, Base delay: {BaseDelayMs}ms",
            this._options.PollIntervalMs,
            this._options.MaxRetryAttempts,
            this._options.RetryBaseDelayMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = this._scopeFactory.CreateScope();
            var queue = scope.ServiceProvider.GetRequiredService<IWebhookReplayQueuePort>();

            var dequeueResult = await queue.DequeueAsync(stoppingToken).ConfigureAwait(false);
            if (dequeueResult.IsSuccess && dequeueResult.Value is { } command)
            {
                await this.ProcessCommandAsync(scope.ServiceProvider, command, stoppingToken).ConfigureAwait(false);
            }
            else if (!dequeueResult.IsSuccess && dequeueResult.Error is { } error)
            {
                this._logger.LogError("Failed to dequeue replay command: {Error}", error.ToString());
            }

            await Task.Delay(pollInterval, stoppingToken).ConfigureAwait(false);
        }

        this._logger.LogInformation("Webhook replay worker stopping");
    }

    private async Task ProcessCommandAsync(IServiceProvider serviceProvider, EnqueueReplayCommand command, CancellationToken cancellationToken)
    {
        var deliveryId = command.Command.DeliveryId.Value;

        // Resolve scoped services
        var replayPort = serviceProvider.GetRequiredService<IReplayWebhookPort>();
        var deadLetterQueue = serviceProvider.GetRequiredService<IDeadLetterQueuePort>();
        var queue = serviceProvider.GetRequiredService<IWebhookReplayQueuePort>();
        var metrics = serviceProvider.GetRequiredService<IMetricsPort>();

        try
        {
            // Check if max retries exceeded
            if (command.Attempt >= this._options.MaxRetryAttempts)
            {
                this._logger.LogWarning(
                    "Webhook delivery {DeliveryId} exceeded max retry attempts ({MaxAttempts}), moving to dead-letter queue",
                    deliveryId,
                    this._options.MaxRetryAttempts);

                var dlqResult = await deadLetterQueue.MoveToDeadLetterAsync(
                    command,
                    $"Exceeded maximum retry attempts ({this._options.MaxRetryAttempts})",
                    cancellationToken).ConfigureAwait(false);

                if (dlqResult.IsSuccess)
                {
                    metrics.IncrementCounter("webhook_replay_dlq_moved", 1,
                        new KeyValuePair<string, object?>("delivery_id", deliveryId),
                        new KeyValuePair<string, object?>("attempts", command.Attempt));
                }

                return;
            }

            // Apply exponential backoff if this is a retry
            if (command.Attempt > 0)
            {
                var backoffDelay = this.CalculateBackoffDelay(command.Attempt);
                this._logger.LogDebug(
                    "Applying backoff delay of {BackoffMs}ms for delivery {DeliveryId} (attempt {Attempt})",
                    backoffDelay.TotalMilliseconds,
                    deliveryId,
                    command.Attempt);

                await Task.Delay(backoffDelay, cancellationToken).ConfigureAwait(false);
            }

            // Attempt replay
            this._logger.LogInformation(
                "Processing webhook replay for delivery {DeliveryId} (attempt {Attempt}/{MaxAttempts})",
                deliveryId,
                command.Attempt + 1,
                this._options.MaxRetryAttempts);

            var result = await replayPort.ReplayAsync(command, cancellationToken).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                metrics.IncrementCounter("webhook_replay_success", 1,
                    new KeyValuePair<string, object?>("delivery_id", deliveryId),
                    new KeyValuePair<string, object?>("attempts", command.Attempt + 1));

                this._logger.LogInformation(
                    "Webhook replay succeeded for delivery {DeliveryId} after {Attempts} attempt(s)",
                    deliveryId,
                    command.Attempt + 1);
            }
            else
            {
                // Replay failed, re-enqueue with incremented attempt count
                var nextCommand = command.NextAttempt();
                var enqueueResult = await queue.EnqueueAsync(nextCommand, cancellationToken).ConfigureAwait(false);

                if (enqueueResult.IsSuccess)
                {
                    metrics.IncrementCounter("webhook_replay_retry", 1,
                        new KeyValuePair<string, object?>("delivery_id", deliveryId),
                        new KeyValuePair<string, object?>("attempts", nextCommand.Attempt));

                    this._logger.LogWarning(
                        "Webhook replay failed for delivery {DeliveryId} (attempt {Attempt}): {Error}. Re-enqueued for retry.",
                        deliveryId,
                        command.Attempt + 1,
                        result.Error?.ToString() ?? "Unknown error");
                }
                else
                {
                    this._logger.LogError(
                        "Failed to re-enqueue webhook replay for delivery {DeliveryId}: {Error}",
                        deliveryId,
                        enqueueResult.Error?.ToString() ?? "Unknown error");
                }
            }
        }
        catch (Exception ex)
        {
            // CA1031: Catching general exception is intentional here for worker resilience
            this._logger.LogError(ex, "Exception processing replay command for delivery {DeliveryId}", deliveryId);

            // Re-enqueue on exception unless max retries exceeded
            if (command.Attempt < this._options.MaxRetryAttempts)
            {
                var nextCommand = command.NextAttempt();
                var enqueueResult = await queue.EnqueueAsync(nextCommand, cancellationToken).ConfigureAwait(false);

                if (enqueueResult.IsSuccess)
                {
                    metrics.IncrementCounter("webhook_replay_error_retry", 1,
                        new KeyValuePair<string, object?>("delivery_id", deliveryId),
                        new KeyValuePair<string, object?>("attempts", nextCommand.Attempt));
                }
            }
        }
    }

    private TimeSpan CalculateBackoffDelay(int attempt)
    {
        // Exponential backoff: baseDelay * (2 ^ attempt)
        // Capped at 60 seconds to avoid excessive delays
        var delayMs = this._options.RetryBaseDelayMs * Math.Pow(2, attempt - 1);
        var cappedDelayMs = Math.Min(delayMs, 60000);
        return TimeSpan.FromMilliseconds(cappedDelayMs);
    }
}

#pragma warning restore CA1848
