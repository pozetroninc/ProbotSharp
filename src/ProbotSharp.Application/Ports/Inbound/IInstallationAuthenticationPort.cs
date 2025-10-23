// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Application.Ports.Inbound;

/// <summary>
/// Port for installation authentication operations.
/// </summary>
public interface IInstallationAuthenticationPort
{
    /// <summary>
    /// Authenticates an installation and returns an access token.
    /// </summary>
    /// <param name="command">The authentication command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the installation access token or an error.</returns>
    Task<Result<InstallationAccessToken>> AuthenticateAsync(
        AuthenticateInstallationCommand command,
        CancellationToken cancellationToken = default);
}
