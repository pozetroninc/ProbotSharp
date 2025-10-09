// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace ProbotSharp.Infrastructure.Adapters.Idempotency;

/// <summary>
/// Logging extensions for <see cref="DbIdempotencyAdapter"/>.
/// </summary>
public sealed partial class DbIdempotencyAdapter
{
    /// <summary>
    /// Log message definitions for idempotency operations.
    /// </summary>
    private static partial class LogMessages
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Debug,
            Message = "Idempotency key already exists: {IdempotencyKey}")]
        public static partial void LogIdempotencyKeyAlreadyExists(
            ILogger logger,
            string idempotencyKey);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Debug,
            Message = "Idempotency key acquired: {IdempotencyKey}, expires at {ExpiresAt}")]
        public static partial void LogIdempotencyKeyAcquired(
            ILogger logger,
            string idempotencyKey,
            DateTimeOffset expiresAt);

        [LoggerMessage(
            EventId = 3,
            Level = LogLevel.Debug,
            Message = "Idempotency key race condition detected: {IdempotencyKey}")]
        public static partial void LogIdempotencyKeyRaceCondition(
            ILogger logger,
            string idempotencyKey,
            Exception exception);

        [LoggerMessage(
            EventId = 4,
            Level = LogLevel.Debug,
            Message = "Idempotency key released: {IdempotencyKey}")]
        public static partial void LogIdempotencyKeyReleased(
            ILogger logger,
            string idempotencyKey);

        [LoggerMessage(
            EventId = 5,
            Level = LogLevel.Information,
            Message = "Idempotency cleanup completed: {RemovedCount} expired records removed")]
        public static partial void LogIdempotencyCleanupCompleted(
            ILogger logger,
            int removedCount);
    }
}
