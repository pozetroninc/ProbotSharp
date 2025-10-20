// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Port for caching installation access tokens.
/// </summary>
public interface IAccessTokenCachePort
{
    /// <summary>
    /// Gets a cached installation access token.
    /// </summary>
    /// <param name="installationId">The installation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached token if found; otherwise, null.</returns>
    Task<InstallationAccessToken?> GetAsync(InstallationId installationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Caches an installation access token.
    /// </summary>
    /// <param name="installationId">The installation ID.</param>
    /// <param name="token">The access token to cache.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetAsync(InstallationId installationId, InstallationAccessToken token, CancellationToken cancellationToken = default);
}
