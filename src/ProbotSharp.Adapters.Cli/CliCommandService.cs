// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Reflection;

using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

#pragma warning disable CA1848 // Performance: LoggerMessage delegates - not performance-critical for this codebase

namespace ProbotSharp.Adapters.Cli;

/// <summary>
/// Minimal CLI command adapter bridging console commands to application use cases.
/// </summary>
public sealed class CliCommandService : ICliCommandPort
{
    private readonly IAppLifecyclePort _lifecyclePort;
    private readonly IWebhookProcessingPort _webhookProcessingPort;
    private readonly ILogger<CliCommandService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CliCommandService"/> class.
    /// </summary>
    /// <param name="lifecyclePort">The application lifecycle port for server operations.</param>
    /// <param name="webhookProcessingPort">The webhook processing port for handling webhook events.</param>
    /// <param name="logger">The logger for recording CLI operations.</param>
    public CliCommandService(
        IAppLifecyclePort lifecyclePort,
        IWebhookProcessingPort webhookProcessingPort,
        ILogger<CliCommandService> logger)
    {
        this._lifecyclePort = lifecyclePort;
        this._webhookProcessingPort = webhookProcessingPort;
        this._logger = logger;
    }

    /// <summary>
    /// Executes the run command to start the ProbotSharp development server.
    /// </summary>
    /// <param name="command">The run command with server configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result indicating success or failure with error details.</returns>
    public async Task<Result> ExecuteRunAsync(RunCliCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.AppPaths is null || command.AppPaths.Length == 0)
        {
            return Result.Failure("cli_run_missing_app", "Provide at least one app path when running the server.");
        }

        var startCommand = new StartServerCommand(
            command.Host,
            command.Port,
            command.WebhookPath ?? "/webhooks",
            command.WebhookProxy,
            command.AppId,
            command.PrivateKey,
            command.Secret,
            command.BaseUrl,
            command.AppPaths);

        this._logger.LogInformation(
            "Starting server on {Host}:{Port} for {AppCount} app(s)",
            command.Host,
            command.Port,
            command.AppPaths.Length);

        var lifecycleResult = await this._lifecyclePort.StartServerAsync(startCommand, cancellationToken).ConfigureAwait(false);
        if (!lifecycleResult.IsSuccess && lifecycleResult.Error is not null)
        {
            this._logger.LogError(
                "Failed to start server: {Error}",
                lifecycleResult.Error.Value.ToString());
        }

        if (lifecycleResult.IsSuccess && lifecycleResult.Value is { } info)
        {
            this._logger.LogInformation(
                "Server ready at http://{Host}:{Port}{Path}",
                info.Host,
                info.Port,
                info.WebhookPath);
            return Result.Success();
        }

        return lifecycleResult.Error is null
            ? Result.Failure("server_start_failed", "Unable to start server")
            : Result.Failure(lifecycleResult.Error.Value);

    }

    /// <summary>
    /// Executes the receive command to simulate processing a webhook event from a file.
    /// </summary>
    /// <param name="command">The receive command with event name and payload path.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result indicating success or failure with error details.</returns>
    public async Task<Result> ExecuteReceiveAsync(ReceiveEventCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.PayloadPath))
        {
            return Result.Failure("cli_receive_missing_payload", "Payload path is required.");
        }

        if (!File.Exists(command.PayloadPath))
        {
            return Result.Failure("cli_receive_payload_not_found", $"Payload file not found: {command.PayloadPath}");
        }

        if (string.IsNullOrWhiteSpace(command.AppPath))
        {
            return Result.Failure("cli_receive_missing_app", "App path is required.");
        }

        try
        {
            // Read and parse the webhook payload
            var payloadJson = await File.ReadAllTextAsync(command.PayloadPath, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                return Result.Failure("cli_receive_empty_payload", "Payload file is empty.");
            }

            this._logger.LogInformation(
                "Processing webhook event {EventName} from {PayloadPath} for app {AppPath}",
                command.EventName.Value,
                command.PayloadPath,
                command.AppPath);

            // Parse payload into domain value object
            var payload = WebhookPayload.Create(payloadJson);

            // Extract installation ID from payload if present
            InstallationId? installationId = null;
            if (payload.RootElement.TryGetProperty("installation", out var installation) &&
                installation.TryGetProperty("id", out var installationIdElement) &&
                installationIdElement.TryGetInt64(out var installationIdValue))
            {
                installationId = InstallationId.Create(installationIdValue);
            }

            // Generate a delivery ID for this simulated webhook
            var deliveryId = DeliveryId.Create(Guid.NewGuid().ToString());

            // Create a dummy signature for CLI testing (signature validation is bypassed in local mode)
            var signature = WebhookSignature.Create("sha256=" + new string('0', 64));

            // Create the webhook processing command
            var processCommand = new ProcessWebhookCommand(
                deliveryId,
                command.EventName,
                payload,
                installationId,
                signature,
                payloadJson);

            // Delegate to the webhook processing use case
            var result = await this._webhookProcessingPort.ProcessAsync(processCommand, cancellationToken).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                this._logger.LogInformation(
                    "Successfully processed webhook event {EventName} with delivery ID {DeliveryId}",
                    command.EventName.Value,
                    deliveryId.Value);
            }
            else
            {
                this._logger.LogError(
                    "Failed to process webhook event {EventName}: {Error}",
                    command.EventName.Value,
                    result.Error?.Message ?? "Unknown error");
            }

            return result;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to receive webhook event");
            return Result.Failure(
                "cli_receive_failed",
                $"Failed to process webhook event: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the ProbotSharp version information.
    /// </summary>
    /// <param name="query">The version query.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the version string.</returns>
    public Task<Result<string>> GetVersionAsync(GetVersionQuery query, CancellationToken cancellationToken = default)
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0";
        return Task.FromResult(Result<string>.Success(version));
    }

    /// <summary>
    /// Gets help information for a specific command or general help.
    /// </summary>
    /// <param name="query">The help query with optional command name.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the help text.</returns>
    public Task<Result<string>> GetHelpAsync(GetHelpQuery query, CancellationToken cancellationToken = default)
    {
        var help = query.CommandName switch
        {
            "run" => GetRunHelp(),
            "receive" => GetReceiveHelp(),
            "setup" => GetSetupHelp(),
            "version" => "Display version information\n\nUsage:\n  probot-sharp version",
            "help" => "Display help information\n\nUsage:\n  probot-sharp help [command]",
            null => GetGeneralHelp(),
            _ => $"Unknown command: {query.CommandName}\n\n{GetGeneralHelp()}",
        };

        return Task.FromResult(Result<string>.Success(help));
    }

    private static string GetGeneralHelp() =>
        "ProbotSharp - A .NET GitHub App framework\n\n" +
        "Usage:\n" +
        "  probot-sharp <command> [options]\n\n" +
        "Commands:\n" +
        "  run       Start the development server\n" +
        "  receive   Simulate receiving a webhook event\n" +
        "  setup     Interactive setup wizard for creating a GitHub App\n" +
        "  version   Display version information\n" +
        "  help      Display help information\n\n" +
        "Run 'probot-sharp help <command>' for more information on a command.";

    private static string GetRunHelp() =>
        "Start the ProbotSharp development server\n\n" +
        "Usage:\n" +
        "  probot-sharp run <app-path> [additional-app-paths] [options]\n\n" +
        "Arguments:\n" +
        "  <app-path>  Path(s) to your app module(s)\n\n" +
        "Options:\n" +
        "  --host <host>           Server host (default: localhost)\n" +
        "  -p, --port <port>       Server port (default: 3000)\n" +
        "  --webhook-path <path>   Webhook endpoint path\n" +
        "  --webhook-proxy <url>   Webhook proxy URL (e.g., Smee.io)\n" +
        "  --app-id <id>           GitHub App ID\n" +
        "  --private-key <file>    Path to private key PEM file\n" +
        "  --secret <secret>       Webhook secret\n" +
        "  --base-url <url>        GitHub API base URL (for GHE)\n" +
        "  --redis-url <url>       Redis connection URL\n" +
        "  --log-level <level>     Log level (default: info)\n" +
        "  --log-format <format>   Log format: json or pretty (default: pretty)\n" +
        "  --sentry-dsn <dsn>      Sentry DSN for error tracking";

    private static string GetReceiveHelp() =>
        "Simulate receiving a webhook event for local testing\n\n" +
        "Usage:\n" +
        "  probot-sharp receive <app-path> [options]\n\n" +
        "Arguments:\n" +
        "  <app-path>  Path to your app module\n\n" +
        "Options:\n" +
        "  -e, --event <name>      Webhook event name (e.g., 'issues.opened')\n" +
        "  -f, --file <path>       Path to JSON payload file (or reads from stdin)\n" +
        "  -t, --token <token>     GitHub personal access token\n" +
        "  --app-id <id>           GitHub App ID\n" +
        "  --private-key <file>    Path to private key PEM file\n" +
        "  --base-url <url>        GitHub API base URL (for GHE)\n" +
        "  --log-level <level>     Log level (default: info)\n" +
        "  --log-format <format>   Log format: json or pretty (default: pretty)";

    private static string GetSetupHelp() =>
        "Interactive setup wizard for creating a GitHub App\n\n" +
        "Usage:\n" +
        "  probot-sharp setup [options]\n\n" +
        "Options:\n" +
        "  -p, --port <port>    Local server port for setup (default: 3000)\n" +
        "  --skip-prompt        Skip interactive prompts and use defaults";
}

#pragma warning restore CA1848
