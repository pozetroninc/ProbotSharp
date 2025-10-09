// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Outbound port for idempotency storage operations.
/// Enables prevention of duplicate webhook processing by tracking processed delivery IDs.
/// </summary>
public interface IIdempotencyPort
{
    /// <summary>
    /// Attempts to acquire an idempotency lock for the given key.
    /// Returns true if the lock was acquired (first time processing this key).
    /// Returns false if the key has already been processed.
    /// </summary>
    /// <param name="key">The idempotency key.</param>
    /// <param name="timeToLive">Optional TTL for the idempotency record. Defaults to 24 hours.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the lock was acquired; false if the key was already processed.</returns>
    Task<bool> TryAcquireAsync(
        IdempotencyKey key,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an idempotency key has already been processed.
    /// </summary>
    /// <param name="key">The idempotency key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the key has been processed; false otherwise.</returns>
    Task<bool> ExistsAsync(
        IdempotencyKey key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases an idempotency lock. This is primarily used for testing or manual cleanup.
    /// </summary>
    /// <param name="key">The idempotency key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReleaseAsync(
        IdempotencyKey key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes expired idempotency records.
    /// This operation is typically run periodically by a background service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of expired records removed.</returns>
    Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default);
}
