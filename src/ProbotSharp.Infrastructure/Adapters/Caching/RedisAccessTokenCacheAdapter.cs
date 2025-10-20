// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Globalization;
using System.Text.Json;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.ValueObjects;

using StackExchange.Redis;

namespace ProbotSharp.Infrastructure.Adapters.Caching;

/// <summary>
/// Redis-based cache implementation for GitHub installation access tokens.
/// </summary>
public sealed class RedisAccessTokenCacheAdapter : IAccessTokenCachePort
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisAccessTokenCacheAdapter"/> class.
    /// </summary>
    /// <param name="redis">The Redis connection multiplexer instance.</param>
    public RedisAccessTokenCacheAdapter(IConnectionMultiplexer redis)
    {
        ArgumentNullException.ThrowIfNull(redis);
        this._redis = redis;
        this._database = redis.GetDatabase();
    }

    /// <inheritdoc />
    public async Task<InstallationAccessToken?> GetAsync(InstallationId installationId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(installationId);

        var key = CreateKey(installationId);
        var value = await this._database.StringGetAsync(key).ConfigureAwait(false);

        if (value.IsNullOrEmpty)
        {
            return null;
        }

        try
        {
            var tokenData = JsonSerializer.Deserialize<TokenCacheData>(value!);
            if (tokenData == null)
            {
                return null;
            }

            return InstallationAccessToken.Create(tokenData.Token, tokenData.ExpiresAt);
        }
        catch (JsonException)
        {
            // Invalid JSON, return null
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync(InstallationId installationId, InstallationAccessToken token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(installationId);
        ArgumentNullException.ThrowIfNull(token);

        var key = CreateKey(installationId);
        var tokenData = new TokenCacheData
        {
            Token = token.Value,
            ExpiresAt = token.ExpiresAt,
        };

        var value = JsonSerializer.Serialize(tokenData);

        // Calculate expiration with 30 second buffer
        var absoluteExpiration = token.ExpiresAt.Subtract(TimeSpan.FromSeconds(30));
        var ttl = absoluteExpiration > DateTimeOffset.UtcNow
            ? absoluteExpiration - DateTimeOffset.UtcNow
            : token.ExpiresAt - DateTimeOffset.UtcNow;

        await this._database.StringSetAsync(key, value, ttl).ConfigureAwait(false);
    }

    private static string CreateKey(InstallationId installationId)
    {
        ArgumentNullException.ThrowIfNull(installationId);
        return string.Create(CultureInfo.InvariantCulture, $"installation-token:{installationId.Value}");
    }

    private sealed class TokenCacheData
    {
        public string Token { get; set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; set; }
    }
}
