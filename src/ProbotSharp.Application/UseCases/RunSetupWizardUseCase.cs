// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Text.Json;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.UseCases;

/// <summary>
/// Use case for orchestrating the GitHub App setup wizard flow.
/// Handles manifest creation, OAuth app registration, credential management, and webhook channel setup.
/// Implements hexagonal architecture by coordinating multiple outbound ports for setup operations.
/// </summary>
public sealed class RunSetupWizardUseCase : ISetupWizardPort
{
    private readonly IManifestPersistencePort _manifestPersistence;
    private readonly IGitHubAppManifestPort _appManifest;
    private readonly IEnvironmentConfigurationPort _envConfig;
    private readonly IWebhookChannelPort _webhookChannel;
    private readonly ILoggingPort _logger;

    public RunSetupWizardUseCase(
        IManifestPersistencePort manifestPersistence,
        IGitHubAppManifestPort appManifest,
        IEnvironmentConfigurationPort envConfig,
        IWebhookChannelPort webhookChannel,
        ILoggingPort logger)
    {
        _manifestPersistence = manifestPersistence;
        _appManifest = appManifest;
        _envConfig = envConfig;
        _webhookChannel = webhookChannel;
        _logger = logger;
    }

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

        var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Step 2: Save the manifest
        var saveResult = await _manifestPersistence.SaveAsync(manifestJson, cancellationToken);
        if (!saveResult.IsSuccess)
        {
            return saveResult.Error is null
                ? Result<string>.Failure(
                    "manifest_save_failed",
                    "Failed to save manifest")
                : Result<string>.Failure(saveResult.Error.Value);
        }

        _logger.LogInformation("Manifest created successfully for app: {AppName}", command.AppName);

        return Result<string>.Success(manifestJson);
    }

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
        var manifestResult = await _manifestPersistence.GetAsync(cancellationToken);

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

            var createResult = await CreateManifestAsync(createCommand, cancellationToken);
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

        _logger.LogInformation("Completing setup with OAuth code");

        // Step 1: Exchange OAuth code for app credentials
        var credentialsResult = await _appManifest.CreateAppFromCodeAsync(
            command.Code,
            command.BaseUrl,
            cancellationToken);

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
        var saveResult = await _envConfig.SaveAppCredentialsAsync(
            credentials.AppId,
            credentials.PrivateKey,
            credentials.WebhookSecret,
            credentials.ClientId,
            credentials.ClientSecret,
            cancellationToken);

        if (!saveResult.IsSuccess)
        {
            return saveResult.Error is null
                ? Result<string>.Failure(
                    "credentials_save_failed",
                    "Failed to save credentials to environment")
                : Result<string>.Failure(saveResult.Error.Value);
        }

        _logger.LogInformation(
            "Setup completed successfully. App ID: {AppId}",
            credentials.AppId.Value);

        return Result<string>.Success(credentials.HtmlUrl.ToString());
    }

    public async Task<Result> ImportAppCredentialsAsync(
        ImportAppCredentialsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        _logger.LogInformation("Importing app credentials for App ID: {AppId}", command.AppId.Value);

        // Save credentials to environment configuration
        var saveResult = await _envConfig.SaveAppCredentialsAsync(
            command.AppId,
            command.PrivateKey,
            command.WebhookSecret,
            clientId: null,
            clientSecret: null,
            cancellationToken);

        if (!saveResult.IsSuccess)
        {
            return saveResult.Error is null
                ? Result.Failure(
                    "credentials_import_failed",
                    "Failed to import credentials")
                : Result.Failure(saveResult.Error.Value);
        }

        _logger.LogInformation("Credentials imported successfully");

        return Result.Success();
    }

    public async Task<Result<CreateWebhookChannelResponse>> CreateWebhookChannelAsync(
        CreateWebhookChannelCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        _logger.LogInformation("Creating webhook proxy channel");

        // Step 1: Create webhook channel (e.g., Smee.io)
        var channelResult = await _webhookChannel.CreateChannelAsync(cancellationToken);
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
        var saveResult = await _envConfig.SaveWebhookProxyUrlAsync(
            channel.WebhookProxyUrl,
            cancellationToken);

        if (!saveResult.IsSuccess)
        {
            _logger.LogWarning(
                "Failed to save webhook proxy URL to environment: {Error}",
                saveResult.Error?.Message);
            // Don't fail the operation, just log the warning
        }

        _logger.LogInformation(
            "Webhook proxy channel created: {ProxyUrl}",
            channel.WebhookProxyUrl);

        return Result<CreateWebhookChannelResponse>.Success(channel);
    }
}
