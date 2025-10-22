// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Text.Json;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Application.UseCases;

/// <summary>
/// Use case for orchestrating the GitHub App setup wizard flow.
/// Handles manifest creation, OAuth app registration, credential management, and webhook channel setup.
/// Implements hexagonal architecture by coordinating multiple outbound ports for setup operations.
/// </summary>
public sealed class RunSetupWizardUseCase : ISetupWizardPort
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly IManifestPersistencePort _manifestPersistence;
    private readonly IGitHubAppManifestPort _appManifest;
    private readonly IEnvironmentConfigurationPort _envConfig;
    private readonly IWebhookChannelPort _webhookChannel;
    private readonly ILoggingPort _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunSetupWizardUseCase"/> class.
    /// </summary>
    /// <param name="manifestPersistence">The manifest persistence port for storing GitHub App manifests.</param>
    /// <param name="appManifest">The GitHub App manifest port for OAuth app registration.</param>
    /// <param name="envConfig">The environment configuration port for credential management.</param>
    /// <param name="webhookChannel">The webhook channel port for creating webhook proxy channels.</param>
    /// <param name="logger">The logging port for recording setup operations.</param>
    public RunSetupWizardUseCase(
        IManifestPersistencePort manifestPersistence,
        IGitHubAppManifestPort appManifest,
        IEnvironmentConfigurationPort envConfig,
        IWebhookChannelPort webhookChannel,
        ILoggingPort logger)
    {
        this._manifestPersistence = manifestPersistence;
        this._appManifest = appManifest;
        this._envConfig = envConfig;
        this._webhookChannel = webhookChannel;
        this._logger = logger;
    }

    /// <summary>
    /// Creates a GitHub App manifest JSON for the setup wizard.
    /// </summary>
    /// <param name="command">The command containing app name, description, and URLs.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the manifest JSON string.</returns>
    public async Task<Result<string>> CreateManifestAsync(
        CreateManifestCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.AppName))
        {
            return Result<string>.Failure(
                "app_name_required",
                "Application name is required to create a manifest");
        }

        if (string.IsNullOrWhiteSpace(command.BaseUrl))
        {
            return Result<string>.Failure(
                "base_url_required",
                "Base URL is required to create a manifest");
        }

        // Step 1: Build the manifest JSON
        var cleanedBaseUri = new Uri(command.BaseUrl, UriKind.Absolute);
        var homepageUri = string.IsNullOrWhiteSpace(command.Homepage)
            ? cleanedBaseUri
            : new Uri(command.Homepage, UriKind.Absolute);
        var webhookUri = string.IsNullOrWhiteSpace(command.WebhookProxyUrl)
            ? new Uri(cleanedBaseUri, "/")
            : new Uri(command.WebhookProxyUrl, UriKind.Absolute);
        var redirectUri = new Uri(cleanedBaseUri, "/probot/setup");

        var manifest = new
        {
            name = command.AppName,
            description = command.Description ?? $"{command.AppName} GitHub App",
            url = homepageUri.ToString(),
            hook_attributes = new
            {
                url = webhookUri.ToString()
            },
            redirect_url = redirectUri.ToString(),
            @public = command.IsPublic,
            version = "v1"
        };

        var manifestJson = JsonSerializer.Serialize(manifest, s_jsonOptions);

        // Step 2: Save the manifest
        var saveResult = await this._manifestPersistence.SaveAsync(manifestJson, cancellationToken).ConfigureAwait(false);
        if (!saveResult.IsSuccess)
        {
            return saveResult.Error is null
                ? Result<string>.Failure(
                    "manifest_save_failed",
                    "Failed to save manifest")
                : Result<string>.Failure(saveResult.Error.Value);
        }

        this._logger.LogInformation("Manifest created successfully for app: {AppName}", command.AppName);

        return Result<string>.Success(manifestJson);
    }

    /// <summary>
    /// Gets or creates a GitHub App manifest and returns the GitHub creation URL.
    /// </summary>
    /// <param name="command">The command containing app configuration for manifest creation.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the manifest and GitHub App creation URL.</returns>
    public async Task<Result<GetManifestResponse>> GetManifestAsync(
        GetManifestCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.BaseUrl))
        {
            return Result<GetManifestResponse>.Failure(
                "base_url_required",
                "Base URL is required to get manifest");
        }

        // Step 1: Try to get existing manifest
        var manifestResult = await this._manifestPersistence.GetAsync(cancellationToken).ConfigureAwait(false);

        string manifestJson;
        if (manifestResult.IsSuccess && !string.IsNullOrWhiteSpace(manifestResult.Value))
        {
            manifestJson = manifestResult.Value;
        }
        else
        {
            // Step 2: Create a new manifest if none exists
            var createCommand = new CreateManifestCommand(
                command.AppName ?? "Probot App",
                command.Description ?? "A Probot app",
                command.BaseUrl,
                command.WebhookProxyUrl,
                command.HomepageUrl,
                command.IsPublic);

            var createResult = await this.CreateManifestAsync(createCommand, cancellationToken).ConfigureAwait(false);
            if (!createResult.IsSuccess)
            {
                return createResult.Error is null
                    ? Result<GetManifestResponse>.Failure(
                        "manifest_creation_failed",
                        "Failed to create manifest")
                    : Result<GetManifestResponse>.Failure(createResult.Error.Value);
            }

            manifestJson = createResult.Value!;
        }

        // Step 3: Determine the GitHub App creation URL
        var githubHost = Environment.GetEnvironmentVariable("GHE_HOST") ?? "github.com";
        var githubProtocol = Environment.GetEnvironmentVariable("GHE_PROTOCOL") ?? "https";
        var githubOrg = Environment.GetEnvironmentVariable("GH_ORG");

        var githubBaseUri = new UriBuilder
        {
            Scheme = githubProtocol,
            Host = githubHost,
            Path = string.IsNullOrEmpty(githubOrg)
                ? "settings/apps/new"
                : $"organizations/{githubOrg}/settings/apps/new"
        }.Uri;

        var response = new GetManifestResponse(manifestJson, githubBaseUri);
        return Result<GetManifestResponse>.Success(response);
    }

    /// <summary>
    /// Completes the GitHub App setup by exchanging OAuth code for credentials.
    /// </summary>
    /// <param name="command">The command containing the OAuth code from GitHub callback.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the GitHub App HTML URL.</returns>
    public async Task<Result<string>> CompleteSetupAsync(
        CompleteSetupCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Code))
        {
            return Result<string>.Failure(
                "code_required",
                "OAuth code is required to complete setup");
        }

        this._logger.LogInformation("Completing setup with OAuth code");

        // Step 1: Exchange OAuth code for app credentials
        var credentialsResult = await this._appManifest.CreateAppFromCodeAsync(
            command.Code,
            command.BaseUrl,
            cancellationToken).ConfigureAwait(false);

        if (!credentialsResult.IsSuccess)
        {
            return credentialsResult.Error is null
                ? Result<string>.Failure(
                    "credentials_exchange_failed",
                    "Failed to exchange OAuth code for credentials")
                : Result<string>.Failure(credentialsResult.Error.Value);
        }

        var credentials = credentialsResult.Value;
        if (credentials is null)
        {
            return Result<string>.Failure(
                "credentials_null",
                "Credentials were null after successful exchange");
        }

        // Step 2: Save credentials to environment configuration
        var saveResult = await this._envConfig.SaveAppCredentialsAsync(
            credentials.AppId,
            credentials.PrivateKey,
            credentials.WebhookSecret,
            credentials.ClientId,
            credentials.ClientSecret,
            cancellationToken).ConfigureAwait(false);

        if (!saveResult.IsSuccess)
        {
            return saveResult.Error is null
                ? Result<string>.Failure(
                    "credentials_save_failed",
                    "Failed to save credentials to environment")
                : Result<string>.Failure(saveResult.Error.Value);
        }

        this._logger.LogInformation(
            "Setup completed successfully. App ID: {AppId}",
            credentials.AppId.Value);

        return Result<string>.Success(credentials.HtmlUrl.ToString());
    }

    /// <summary>
    /// Imports manually provided GitHub App credentials into the environment configuration.
    /// </summary>
    /// <param name="command">The command containing app ID, private key, and webhook secret.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    public async Task<Result> ImportAppCredentialsAsync(
        ImportAppCredentialsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        this._logger.LogInformation("Importing app credentials for App ID: {AppId}", command.AppId.Value);

        // Save credentials to environment configuration
        var saveResult = await this._envConfig.SaveAppCredentialsAsync(
            command.AppId,
            command.PrivateKey,
            command.WebhookSecret,
            clientId: null,
            clientSecret: null,
            cancellationToken).ConfigureAwait(false);

        if (!saveResult.IsSuccess)
        {
            return saveResult.Error is null
                ? Result.Failure(
                    "credentials_import_failed",
                    "Failed to import credentials")
                : Result.Failure(saveResult.Error.Value);
        }

        this._logger.LogInformation("Credentials imported successfully");

        return Result.Success();
    }

    /// <summary>
    /// Creates a webhook proxy channel (e.g., Smee.io) for local development.
    /// </summary>
    /// <param name="command">The command to create a webhook channel.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the webhook proxy URL and channel information.</returns>
    public async Task<Result<CreateWebhookChannelResponse>> CreateWebhookChannelAsync(
        CreateWebhookChannelCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        this._logger.LogInformation("Creating webhook proxy channel");

        // Step 1: Create webhook channel (e.g., Smee.io)
        var channelResult = await this._webhookChannel.CreateChannelAsync(cancellationToken).ConfigureAwait(false);
        if (!channelResult.IsSuccess)
        {
            return channelResult.Error is null
                ? Result<CreateWebhookChannelResponse>.Failure(
                    "webhook_channel_creation_failed",
                    "Failed to create webhook proxy channel")
                : Result<CreateWebhookChannelResponse>.Failure(channelResult.Error.Value);
        }

        var channel = channelResult.Value;
        if (channel is null)
        {
            return Result<CreateWebhookChannelResponse>.Failure(
                "webhook_channel_null",
                "Webhook channel was null after successful creation");
        }

        // Step 2: Save webhook proxy URL to environment
        var saveResult = await this._envConfig.SaveWebhookProxyUrlAsync(
            channel.WebhookProxyUrl,
            cancellationToken).ConfigureAwait(false);

        if (!saveResult.IsSuccess)
        {
            this._logger.LogWarning(
                "Failed to save webhook proxy URL to environment: {Error}",
                saveResult.Error?.Message);
            // Don't fail the operation, just log the warning
        }

        this._logger.LogInformation(
            "Webhook proxy channel created: {ProxyUrl}",
            channel.WebhookProxyUrl);

        return Result<CreateWebhookChannelResponse>.Success(channel);
    }
}
