// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Adapters.Workers;

/// <summary>
/// Configuration options for the webhook replay worker.
/// </summary>
public sealed class WebhookReplayWorkerOptions
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts before moving to dead-letter queue.
    /// Default is 3 retries.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay between retry attempts in milliseconds.
    /// Actual delay uses exponential backoff: baseDelay * (2 ^ attempt).
    /// Default is 1000ms (1 second).
    /// </summary>
    public int RetryBaseDelayMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the polling interval for checking the replay queue in milliseconds.
    /// Default is 1000ms (1 second).
    /// </summary>
    public int PollIntervalMs { get; set; } = 1000;
}
