// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Port for GitHub App manifest operations.
/// Handles app creation via OAuth manifest flow and credential exchange.
/// </summary>
public interface IGitHubAppManifestPort
{
    /// <summary>
    /// Exchanges an OAuth code for GitHub App credentials.
    /// Called after user completes app registration on GitHub.
    /// </summary>
    /// <param name="code">OAuth code from GitHub callback.</param>
    /// <param name="baseUrl">Optional GitHub Enterprise base URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>App credentials if successful, error otherwise.</returns>
#pragma warning disable CA1054 // URI parameters should be strings for JSON serialization compatibility
    Task<Result<CreateAppFromCodeResponse>> CreateAppFromCodeAsync(
        string code,
        string? baseUrl,
        CancellationToken cancellationToken = default);
#pragma warning restore CA1054
}
