// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.ValueObjects;

using StackExchange.Redis;

namespace ProbotSharp.Infrastructure.Adapters.Idempotency;

/// <summary>
/// Redis-backed implementation of idempotency storage.
/// Provides distributed idempotency key tracking with automatic expiration using Redis TTL.
/// </summary>
public sealed partial class RedisIdempotencyAdapter : IIdempotencyPort
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisIdempotencyAdapter> _logger;
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);
    private const string KeyPrefix = "idempotency:";

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisIdempotencyAdapter"/> class.
    /// </summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="logger">The logger instance.</param>
    public RedisIdempotencyAdapter(
        IConnectionMultiplexer redis,
        ILogger<RedisIdempotencyAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(logger);

        this._redis = redis;
        this._logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> TryAcquireAsync(
        IdempotencyKey key,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        var ttl = timeToLive ?? DefaultTtl;
        var redisKey = GetRedisKey(key);

        try
        {
            var db = this._redis.GetDatabase();

            // Use SET NX (set if not exists) with expiration
            var acquired = await db.StringSetAsync(
                    redisKey,
                    DateTimeOffset.UtcNow.ToString("O"),
                    ttl,
                    When.NotExists)
                .ConfigureAwait(false);

            if (acquired)
            {
                LogMessages.LogIdempotencyKeyAcquired(this._logger, key.Value, DateTimeOffset.UtcNow.Add(ttl));
            }
            else
            {
                LogMessages.LogIdempotencyKeyAlreadyExists(this._logger, key.Value);
            }

            return acquired;
        }
        catch (RedisException ex)
        {
            LogMessages.LogRedisOperationFailed(this._logger, "TryAcquire", key.Value, ex);
            return false; // Fail open to avoid blocking webhooks
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        IdempotencyKey key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var db = this._redis.GetDatabase();
            var redisKey = GetRedisKey(key);
            var exists = await db.KeyExistsAsync(redisKey).ConfigureAwait(false);
            return exists;
        }
        catch (RedisException ex)
        {
            LogMessages.LogRedisOperationFailed(this._logger, "Exists", key.Value, ex);
            return false; // Fail open
        }
    }

    /// <inheritdoc />
    public async Task ReleaseAsync(
        IdempotencyKey key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var db = this._redis.GetDatabase();
            var redisKey = GetRedisKey(key);
            var deleted = await db.KeyDeleteAsync(redisKey).ConfigureAwait(false);

            if (deleted)
            {
                LogMessages.LogIdempotencyKeyReleased(this._logger, key.Value);
            }
        }
        catch (RedisException ex)
        {
            LogMessages.LogRedisOperationFailed(this._logger, "Release", key.Value, ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        // Redis automatically expires keys based on TTL, so no manual cleanup is needed
        LogMessages.LogIdempotencyCleanupSkipped(this._logger);
        return await Task.FromResult(0).ConfigureAwait(false);
    }

    private static string GetRedisKey(IdempotencyKey key) => $"{KeyPrefix}{key.Value}";
}
