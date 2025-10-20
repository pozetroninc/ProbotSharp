// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Port for GitHub OAuth operations.
/// </summary>
public interface IGitHubOAuthPort
{
    /// <summary>
    /// Creates an installation access token for the specified installation.
    /// </summary>
    /// <param name="installationId">The installation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the access token or an error.</returns>
    Task<Result<InstallationAccessToken>> CreateInstallationTokenAsync(InstallationId installationId, CancellationToken cancellationToken = default);
}
