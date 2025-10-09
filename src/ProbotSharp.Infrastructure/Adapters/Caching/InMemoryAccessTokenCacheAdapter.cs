// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Globalization;

using Microsoft.Extensions.Caching.Memory;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Infrastructure.Adapters.Caching;

/// <summary>
/// In-memory cache implementation for GitHub installation access tokens.
/// </summary>
public sealed class InMemoryAccessTokenCacheAdapter : IAccessTokenCachePort
{
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryAccessTokenCacheAdapter"/> class.
    /// </summary>
    /// <param name="memoryCache">The ASP.NET Core memory cache instance.</param>
    public InMemoryAccessTokenCacheAdapter(IMemoryCache memoryCache)
    {
        ArgumentNullException.ThrowIfNull(memoryCache);
        this._memoryCache = memoryCache;
    }

    /// <inheritdoc />
    public Task<InstallationAccessToken?> GetAsync(InstallationId installationId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(installationId);

        if (this._memoryCache.TryGetValue(CreateKey(installationId), out InstallationAccessToken? token))
        {
            return Task.FromResult<InstallationAccessToken?>(token);
        }

        return Task.FromResult<InstallationAccessToken?>(null);
    }

    /// <inheritdoc />
    public Task SetAsync(InstallationId installationId, InstallationAccessToken token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(installationId);
        ArgumentNullException.ThrowIfNull(token);

        var absoluteExpiration = token.ExpiresAt.Subtract(TimeSpan.FromSeconds(30));
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = absoluteExpiration > DateTimeOffset.UtcNow
                ? absoluteExpiration
                : token.ExpiresAt,
        };

        this._memoryCache.Set(CreateKey(installationId), token, cacheEntryOptions);
        return Task.CompletedTask;
    }

    private static string CreateKey(InstallationId installationId)
    {
        ArgumentNullException.ThrowIfNull(installationId);
        return string.Create(CultureInfo.InvariantCulture, $"installation-token:{installationId.Value}");
    }
}

