// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.Entities;
using ProbotSharp.Shared.Abstractions;

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

    public RunAppLifecycleUseCase(ILoggingPort logger)
    {
        _logger = logger;
    }

    public async Task<Result<ServerInfo>> StartServerAsync(
        StartServerCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_isRunning)
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
                _app = GitHubApp.Create(
                    command.AppId,
                    "Probot App",
                    command.PrivateKey,
                    command.WebhookSecret);

                _logger.LogInformation(
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
            _logger.LogInformation(
                "Starting in setup mode - APP_ID or PRIVATE_KEY not provided");
        }

        // Step 2: Mark server as running
        _isRunning = true;

        // Step 3: Log server start
        _logger.LogInformation(
            $"Server starting on {command.Host}:{command.Port} at webhook path: {command.WebhookPath}");

        var serverInfo = new ServerInfo(
            command.Host,
            command.Port,
            command.WebhookPath,
            IsRunning: true,
            command.WebhookProxy);

        return Result<ServerInfo>.Success(serverInfo);
    }

    public async Task<Result> StopServerAsync(
        StopServerCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_isRunning)
        {
            return Result.Failure(
                "server_not_running",
                "Server is not currently running");
        }

        // Log graceful shutdown if requested
        if (command.Graceful)
        {
            _logger.LogInformation(
                "Initiating graceful shutdown...");
        }
        else
        {
            _logger.LogInformation(
                "Initiating immediate shutdown...");
        }

        // Mark server as stopped
        _isRunning = false;
        _app = null;

        _logger.LogInformation(
            "Server stopped successfully");

        return Result.Success();
    }

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

    public Task<Result<AppInfo>> GetAppInfoAsync(CancellationToken cancellationToken = default)
    {
        var appInfo = new AppInfo(
            _app?.Id,
            _app?.Name,
            LoadedAppPaths: Array.Empty<string>(), // Placeholder for loaded app paths
            IsSetupMode: _app is null);

        return Task.FromResult(Result<AppInfo>.Success(appInfo));
    }
}
