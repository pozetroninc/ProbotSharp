// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace ProbotSharp.Infrastructure.Adapters.Idempotency;

/// <summary>
/// Structured logging messages for InMemoryIdempotencyAdapter using source generation.
/// </summary>
public sealed partial class InMemoryIdempotencyAdapter
{
    internal static partial class LogMessages
    {
        [LoggerMessage(
            Level = LogLevel.Debug,
            Message = "Memory idempotency key acquired: {IdempotencyKey}, expires at {ExpiresAt}")]
        internal static partial void LogIdempotencyKeyAcquired(
            ILogger logger,
            string idempotencyKey,
            DateTimeOffset expiresAt);

        [LoggerMessage(
            Level = LogLevel.Debug,
            Message = "Memory idempotency key already exists: {IdempotencyKey}")]
        internal static partial void LogIdempotencyKeyAlreadyExists(
            ILogger logger,
            string idempotencyKey);

        [LoggerMessage(
            Level = LogLevel.Debug,
            Message = "Memory idempotency key released: {IdempotencyKey}")]
        internal static partial void LogIdempotencyKeyReleased(
            ILogger logger,
            string idempotencyKey);

        [LoggerMessage(
            Level = LogLevel.Debug,
            Message = "Memory idempotency cleanup skipped (automatic expiration via IMemoryCache)")]
        internal static partial void LogIdempotencyCleanupSkipped(ILogger logger);
    }
}
