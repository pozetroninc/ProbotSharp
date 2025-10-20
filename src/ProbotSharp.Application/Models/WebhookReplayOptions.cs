// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Configuration options for webhook replay queue processing.
/// </summary>
public sealed class WebhookReplayOptions
{
    /// <summary>
    /// Maximum number of retry attempts before moving to dead-letter queue.
    /// Default: 5.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 5;

    /// <summary>
    /// Initial backoff delay in seconds for the first retry.
    /// Default: 2 seconds.
    /// </summary>
    public int InitialBackoffSeconds { get; set; } = 2;

    /// <summary>
    /// Maximum backoff delay in seconds.
    /// Default: 300 seconds (5 minutes).
    /// </summary>
    public int MaxBackoffSeconds { get; set; } = 300;

    /// <summary>
    /// Backoff multiplier for exponential backoff.
    /// Default: 2.0 (doubles each retry).
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Maximum jitter percentage (0-1) to add randomness to backoff delays.
    /// Default: 0.1 (10% jitter).
    /// </summary>
    public double JitterFactor { get; set; } = 0.1;

    /// <summary>
    /// Poll interval in seconds for checking the queue.
    /// Default: 1 second.
    /// </summary>
    public int PollIntervalSeconds { get; set; } = 1;

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any option is invalid.</exception>
    public void Validate()
    {
        if (this.MaxRetryAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(this.MaxRetryAttempts), "Must be at least 1");
        }

        if (this.InitialBackoffSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(this.InitialBackoffSeconds), "Cannot be negative");
        }

        if (this.MaxBackoffSeconds < this.InitialBackoffSeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(this.MaxBackoffSeconds), "Cannot be less than InitialBackoffSeconds");
        }

        if (this.BackoffMultiplier <= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(this.BackoffMultiplier), "Must be greater than 1.0");
        }

        if (this.JitterFactor is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(this.JitterFactor), "Must be between 0 and 1");
        }

        if (this.PollIntervalSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(this.PollIntervalSeconds), "Cannot be negative");
        }
    }
}
