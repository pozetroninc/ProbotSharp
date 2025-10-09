// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace ProbotSharp.Infrastructure.Adapters.GitHub;

/// <summary>
/// Structured log messages for GitHub REST HTTP adapter operations.
/// </summary>
internal static partial class LogMessages
{
    /// <summary>
    /// Logs when a GitHub REST API call fails.
    /// </summary>
    [LoggerMessage(EventId = 2300, Level = LogLevel.Error, Message = "GitHub REST call failed.")]
    public static partial void GitHubRestCallFailed(ILogger logger, Exception exception);

    /// <summary>
    /// Logs when a retry is attempted after a transient failure.
    /// </summary>
    [LoggerMessage(EventId = 2301, Level = LogLevel.Warning, Message = "GitHub REST call failed with transient error. Retrying... (Attempt {RetryAttempt} of {MaxRetries}, Delay: {RetryDelay}ms)")]
    public static partial void GitHubRestRetrying(ILogger logger, int retryAttempt, int maxRetries, double retryDelay);

    /// <summary>
    /// Logs when the circuit breaker opens due to repeated failures.
    /// </summary>
    [LoggerMessage(EventId = 2302, Level = LogLevel.Error, Message = "GitHub REST circuit breaker opened after {FailureCount} consecutive failures. Blocking requests for {BreakDuration}ms.")]
    public static partial void GitHubRestCircuitBreakerOpened(ILogger logger, int failureCount, double breakDuration);

    /// <summary>
    /// Logs when the circuit breaker resets after successful call.
    /// </summary>
    [LoggerMessage(EventId = 2303, Level = LogLevel.Information, Message = "GitHub REST circuit breaker reset after successful call.")]
    public static partial void GitHubRestCircuitBreakerReset(ILogger logger);

    /// <summary>
    /// Logs when a request times out.
    /// </summary>
    [LoggerMessage(EventId = 2304, Level = LogLevel.Error, Message = "GitHub REST call timed out after {Timeout}ms.")]
    public static partial void GitHubRestTimeout(ILogger logger, double timeout, Exception exception);

    /// <summary>
    /// Logs when a call is rejected due to open circuit breaker.
    /// </summary>
    [LoggerMessage(EventId = 2305, Level = LogLevel.Warning, Message = "GitHub REST call rejected: circuit breaker is open.")]
    public static partial void GitHubRestCircuitBreakerRejected(ILogger logger);
}
