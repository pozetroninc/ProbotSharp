// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Application.UseCases;

/// <summary>
/// Use case for managing application lifecycle operations.
/// Handles server startup, shutdown, app loading, and lifecycle state management.
/// Maintains internal state for the running server and loaded GitHub App.
/// Implements hexagonal architecture by orchestrating lifecycle operations.
/// </summary>
public sealed class RunAppLifecycleUseCase : IAppLifecyclePort
{
    private readonly ILoggingPort _logger;
    private GitHubApp? _app;
    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunAppLifecycleUseCase"/> class.
    /// </summary>
    /// <param name="logger">The logging port for structured logging.</param>
    public RunAppLifecycleUseCase(ILoggingPort logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Starts the application server with the specified configuration.
    /// </summary>
    /// <param name="command">The command containing server start configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A result containing server information if startup is successful.</returns>
    public async Task<Result<ServerInfo>> StartServerAsync(
        StartServerCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (this._isRunning)
        {
            return Result<ServerInfo>.Failure(
                "server_already_running",
                "Server is already running");
        }

        // Step 1: Validate configuration for non-setup mode
        if (command.AppId is not null && command.PrivateKey is not null)
        {
            if (string.IsNullOrWhiteSpace(command.WebhookSecret))
            {
                return Result<ServerInfo>.Failure(
                    "webhook_secret_required",
                    "Webhook secret is required when app ID and private key are provided");
            }

            // Create and validate GitHub App entity
            try
            {
                this._app = GitHubApp.Create(
                    command.AppId,
                    "Probot App",
                    command.PrivateKey,
                    command.WebhookSecret);

                this._logger.LogInformation(
                    $"GitHub App initialized with ID: {command.AppId.Value}");
            }
            catch (Exception ex)
            {
                return Result<ServerInfo>.Failure(
                    "app_initialization_failed",
                    "Failed to initialize GitHub App",
                    ex.Message);
            }
        }
        else
        {
            // Setup mode - app will be configured later
            this._logger.LogInformation(
                "Starting in setup mode - APP_ID or PRIVATE_KEY not provided");
        }

        // Step 2: Mark server as running
        this._isRunning = true;

        // Step 3: Log server start
        this._logger.LogInformation(
            $"Server starting on {command.Host}:{command.Port} at webhook path: {command.WebhookPath}");

        var serverInfo = new ServerInfo(
            command.Host,
            command.Port,
            command.WebhookPath,
            IsRunning: true,
            command.WebhookProxy);

        return Result<ServerInfo>.Success(serverInfo);
    }

    /// <summary>
    /// Stops the application server gracefully or immediately.
    /// </summary>
    /// <param name="command">The command containing server stop configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A result indicating whether the server stopped successfully.</returns>
    public async Task<Result> StopServerAsync(
        StopServerCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!this._isRunning)
        {
            return Result.Failure(
                "server_not_running",
                "Server is not currently running");
        }

        // Log graceful shutdown if requested
        if (command.Graceful)
        {
            this._logger.LogInformation(
                "Initiating graceful shutdown...");
        }
        else
        {
            this._logger.LogInformation(
                "Initiating immediate shutdown...");
        }

        // Mark server as stopped
        this._isRunning = false;
        this._app = null;

        this._logger.LogInformation(
            "Server stopped successfully");

        return Result.Success();
    }

    /// <summary>
    /// Loads a GitHub App from the specified application path.
    /// </summary>
    /// <param name="command">The command containing app loading configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A result indicating whether the app was loaded successfully.</returns>
    public Task<Result> LoadAppAsync(
        LoadAppCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.AppPath))
        {
            return Task.FromResult(Result.Failure(
                "app_path_required",
                "Application path is required to load an app"));
        }

        // For now, this is a placeholder that validates the command
        // In a full implementation, this would:
        // 1. Resolve the app module from the path
        // 2. Load and validate the app function
        // 3. Register webhook handlers from the app
        // 4. Store the loaded app in state

        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Gets information about the currently loaded GitHub App.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A result containing the app information.</returns>
    public Task<Result<AppInfo>> GetAppInfoAsync(CancellationToken cancellationToken = default)
    {
        var appInfo = new AppInfo(
            this._app?.Id,
            this._app?.Name,
            LoadedAppPaths: Array.Empty<string>(), // Placeholder for loaded app paths
            IsSetupMode: this._app is null);

        return Task.FromResult(Result<AppInfo>.Success(appInfo));
    }
}
