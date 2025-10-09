// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Port for persisting application configuration to environment.
/// Handles .env file updates and environment variable management.
/// </summary>
public interface IEnvironmentConfigurationPort
{
    /// <summary>
    /// Persists GitHub App credentials to environment configuration.
    /// Typically updates .env file with APP_ID, PRIVATE_KEY, WEBHOOK_SECRET, etc.
    /// </summary>
    /// <param name="appId">GitHub App ID</param>
    /// <param name="privateKey">Private key PEM</param>
    /// <param name="webhookSecret">Webhook secret</param>
    /// <param name="clientId">OAuth client ID</param>
    /// <param name="clientSecret">OAuth client secret</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or error result</returns>
    Task<Result> SaveAppCredentialsAsync(
        GitHubAppId appId,
        PrivateKeyPem privateKey,
        string webhookSecret,
        string? clientId = null,
        string? clientSecret = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists webhook proxy URL to environment configuration.
    /// </summary>
    /// <param name="webhookProxyUrl">Webhook proxy URL (e.g., Smee.io channel)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or error result</returns>
    Task<Result> SaveWebhookProxyUrlAsync(
        string webhookProxyUrl,
        CancellationToken cancellationToken = default);
}
