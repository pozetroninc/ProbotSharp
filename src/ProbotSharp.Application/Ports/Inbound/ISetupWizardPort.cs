// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Inbound;

/// <summary>
/// Inbound port for GitHub App setup wizard operations.
/// Handles manifest creation, app registration via OAuth, credential import, and webhook channel creation.
/// </summary>
public interface ISetupWizardPort
{
    /// <summary>
    /// Creates a GitHub App manifest for app registration.
    /// Returns the manifest JSON and the GitHub URL to create the app.
    /// </summary>
    /// <param name="command">Manifest creation command with app metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Manifest JSON if successful, error otherwise.</returns>
    Task<Result<string>> CreateManifestAsync(
        CreateManifestCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a pre-configured GitHub App manifest based on app settings.
    /// Used to render the setup page with manifest details.
    /// </summary>
    /// <param name="command">Get manifest command with base URL and optional metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Manifest response with JSON and creation URL if successful, error otherwise.</returns>
    Task<Result<GetManifestResponse>> GetManifestAsync(
        GetManifestCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes the setup wizard by exchanging an OAuth code for app credentials.
    /// Persists credentials to .env file and returns the app HTML URL.
    /// </summary>
    /// <param name="command">Command containing OAuth code from GitHub callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>GitHub App HTML URL if successful, error otherwise.</returns>
    Task<Result<string>> CompleteSetupAsync(
        CompleteSetupCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports existing GitHub App credentials for manual configuration.
    /// Persists credentials to .env file, bypassing the OAuth flow.
    /// </summary>
    /// <param name="command">Command containing app ID, private key, and webhook secret.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or error result.</returns>
    Task<Result> ImportAppCredentialsAsync(
        ImportAppCredentialsCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a webhook proxy channel (e.g., Smee.io) for local development.
    /// Enables webhook delivery to localhost during development.
    /// </summary>
    /// <param name="command">Command specifying proxy configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Webhook channel response with proxy URL if successful, error otherwise.</returns>
    Task<Result<CreateWebhookChannelResponse>> CreateWebhookChannelAsync(
        CreateWebhookChannelCommand command,
        CancellationToken cancellationToken = default);
}
