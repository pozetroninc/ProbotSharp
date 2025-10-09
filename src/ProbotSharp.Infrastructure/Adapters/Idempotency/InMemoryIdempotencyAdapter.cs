// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Collections.Concurrent;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Infrastructure.Adapters.Idempotency;

/// <summary>
/// In-memory implementation of idempotency storage using IMemoryCache.
/// Provides thread-safe idempotency key tracking with automatic expiration.
/// </summary>
/// <remarks>
/// WARNING: This adapter loses all idempotency state on application restart.
/// Only suitable for:
/// - Development and testing environments
/// - Single-instance deployments with no restart requirements
/// - Applications that can tolerate duplicate webhook processing after restart
///
/// For production use with multiple instances or restart resilience, use Redis or Database adapters.
/// </remarks>
public sealed partial class InMemoryIdempotencyAdapter : IIdempotencyPort, IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<InMemoryIdempotencyAdapter> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks;
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);
    private const string KeyPrefix = "idempotency:";

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryIdempotencyAdapter"/> class.
    /// </summary>
    /// <param name="memoryCache">The ASP.NET Core memory cache instance.</param>
    /// <param name="logger">The logger instance.</param>
    public InMemoryIdempotencyAdapter(
        IMemoryCache memoryCache,
        ILogger<InMemoryIdempotencyAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(memoryCache);
        ArgumentNullException.ThrowIfNull(logger);

        this._memoryCache = memoryCache;
        this._logger = logger;
        this._keyLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
    }

    /// <inheritdoc />
    public async Task<bool> TryAcquireAsync(
        IdempotencyKey key,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        var ttl = timeToLive ?? DefaultTtl;
        var cacheKey = GetCacheKey(key);

        // Get or create a lock for this specific key to ensure thread-safety
        var keyLock = this._keyLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

        await keyLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Check if the key already exists
            if (this._memoryCache.TryGetValue(cacheKey, out _))
            {
                LogMessages.LogIdempotencyKeyAlreadyExists(this._logger, key.Value);
                return false;
            }

            // Acquire the lock by setting the key
            var expiresAt = DateTimeOffset.UtcNow.Add(ttl);
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl,
            };

            // Register callback to clean up the lock reference when the cache entry is evicted
            options.RegisterPostEvictionCallback((evictedKey, value, reason, state) =>
            {
                if (evictedKey is string key)
                {
                    // Just remove from dictionary; disposal happens in Dispose() method
                    this._keyLocks.TryRemove(key, out _);
                }
            });

            this._memoryCache.Set(cacheKey, expiresAt, options);
            LogMessages.LogIdempotencyKeyAcquired(this._logger, key.Value, expiresAt);
            return true;
        }
        finally
        {
            keyLock.Release();
        }
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(
        IdempotencyKey key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        var cacheKey = GetCacheKey(key);
        var exists = this._memoryCache.TryGetValue(cacheKey, out _);
        return Task.FromResult(exists);
    }

    /// <inheritdoc />
    public async Task ReleaseAsync(
        IdempotencyKey key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        var cacheKey = GetCacheKey(key);

        // Use lock to ensure thread-safety during removal
        var keyLock = this._keyLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

        await keyLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            this._memoryCache.Remove(cacheKey);
            LogMessages.LogIdempotencyKeyReleased(this._logger, key.Value);
        }
        finally
        {
            keyLock.Release();

            // Remove the lock from dictionary; disposal happens in Dispose() method
            this._keyLocks.TryRemove(cacheKey, out _);
        }
    }

    /// <inheritdoc />
    public Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        // IMemoryCache automatically expires keys based on configured options, so no manual cleanup is needed
        LogMessages.LogIdempotencyCleanupSkipped(this._logger);
        return Task.FromResult(0);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var kvp in this._keyLocks)
        {
            kvp.Value.Dispose();
        }

        this._keyLocks.Clear();
    }

    private static string GetCacheKey(IdempotencyKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return $"{KeyPrefix}{key.Value}";
    }
}
