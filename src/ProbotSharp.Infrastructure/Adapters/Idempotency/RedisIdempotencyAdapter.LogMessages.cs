// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace ProbotSharp.Infrastructure.Adapters.Idempotency;

/// <summary>
/// Logging extensions for <see cref="RedisIdempotencyAdapter"/>.
/// </summary>
public sealed partial class RedisIdempotencyAdapter
{
    /// <summary>
    /// Log message definitions for Redis idempotency operations.
    /// </summary>
    private static partial class LogMessages
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Debug,
            Message = "Idempotency key already exists in Redis: {IdempotencyKey}")]
        public static partial void LogIdempotencyKeyAlreadyExists(
            ILogger logger,
            string idempotencyKey);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Debug,
            Message = "Idempotency key acquired in Redis: {IdempotencyKey}, expires at {ExpiresAt}")]
        public static partial void LogIdempotencyKeyAcquired(
            ILogger logger,
            string idempotencyKey,
            DateTimeOffset expiresAt);

        [LoggerMessage(
            EventId = 3,
            Level = LogLevel.Debug,
            Message = "Idempotency key released from Redis: {IdempotencyKey}")]
        public static partial void LogIdempotencyKeyReleased(
            ILogger logger,
            string idempotencyKey);

        [LoggerMessage(
            EventId = 4,
            Level = LogLevel.Warning,
            Message = "Redis operation '{Operation}' failed for idempotency key {IdempotencyKey}")]
        public static partial void LogRedisOperationFailed(
            ILogger logger,
            string operation,
            string idempotencyKey,
            Exception exception);

        [LoggerMessage(
            EventId = 5,
            Level = LogLevel.Debug,
            Message = "Idempotency cleanup skipped - Redis handles TTL automatically")]
        public static partial void LogIdempotencyCleanupSkipped(ILogger logger);
    }
}
