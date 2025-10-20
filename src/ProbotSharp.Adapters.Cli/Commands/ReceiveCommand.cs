// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.ComponentModel;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Domain.ValueObjects;

using Spectre.Console;
using Spectre.Console.Cli;

namespace ProbotSharp.Adapters.Cli.Commands;

/// <summary>
/// CLI command to receive a single webhook event from a file or stdin.
/// Used for local testing and debugging of webhook handlers.
/// </summary>
public sealed class ReceiveCommand : AsyncCommand<ReceiveCommand.Settings>
{
    private readonly ICliCommandPort _cliPort;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiveCommand"/> class.
    /// </summary>
    /// <param name="cliPort">The CLI command port.</param>
    public ReceiveCommand(ICliCommandPort cliPort)
    {
        this._cliPort = cliPort;
    }

    /// <summary>
    /// Executes the receive command asynchronously.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <returns>Exit code (0 for success, 1 for failure).</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            var rule = new Rule("[blue]ProbotSharp - Receive Webhook Event[/]")
            {
                Justification = Justify.Left,
            };
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();

            // Determine payload path
            string payloadPath;
            if (!string.IsNullOrWhiteSpace(settings.PayloadFile))
            {
                payloadPath = settings.PayloadFile;
                if (!File.Exists(payloadPath))
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] Payload file not found: {payloadPath}");
                    return 1;
                }
            }
            else
            {
                // Read from stdin
                AnsiConsole.MarkupLine("[yellow]Reading payload from stdin...[/]");
                AnsiConsole.MarkupLine("[dim](Type or paste JSON payload, then press Ctrl+D or Ctrl+Z)[/]");
                AnsiConsole.WriteLine();

                var stdinContent = await Console.In.ReadToEndAsync().ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(stdinContent))
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] No payload provided via stdin");
                    return 1;
                }

                // Write to temp file
                payloadPath = Path.GetTempFileName();
                await File.WriteAllTextAsync(payloadPath, stdinContent).ConfigureAwait(false);
                AnsiConsole.MarkupLine($"[dim]Payload written to temporary file: {payloadPath}[/]");
            }

            // Parse private key if file provided
            PrivateKeyPem? privateKey = null;
            if (!string.IsNullOrWhiteSpace(settings.PrivateKeyFile))
            {
                if (!File.Exists(settings.PrivateKeyFile))
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] Private key file not found: {settings.PrivateKeyFile}");
                    return 1;
                }

                var pem = await File.ReadAllTextAsync(settings.PrivateKeyFile).ConfigureAwait(false);
                privateKey = PrivateKeyPem.Create(pem);
            }

            // Parse app ID if provided
            GitHubAppId? appId = null;
            if (settings.AppId.HasValue)
            {
                appId = GitHubAppId.Create(settings.AppId.Value);
            }

            // Create event name
            var eventName = WebhookEventName.Create(settings.EventName);

            // Create command
            var command = new ReceiveEventCommand(
                EventName: eventName,
                PayloadPath: payloadPath,
                AppPath: settings.AppPath,
                Token: settings.Token,
                AppId: appId,
                PrivateKey: privateKey,
                BaseUrl: settings.BaseUrl,
                LogLevel: settings.LogLevel,
                LogFormat: settings.LogFormat,
                LogLevelInString: settings.LogLevelInString,
                LogMessageKey: settings.LogMessageKey,
                SentryDsn: settings.SentryDsn);

            AnsiConsole.MarkupLine($"[cyan]Event:[/] {settings.EventName}");
            AnsiConsole.MarkupLine($"[cyan]App:[/] {settings.AppPath}");
            AnsiConsole.WriteLine();

            // Execute via port
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start("Processing webhook event...", ctx =>
                {
                    // This is synchronous in the Status context
                });

            var result = await this._cliPort.ExecuteReceiveAsync(command, CancellationToken.None).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                if (result.Error is { } error)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] {error.Message}");
                    if (!string.IsNullOrWhiteSpace(error.Details))
                    {
                        AnsiConsole.MarkupLine($"[dim]{error.Details}[/]");
                    }
                }

                return 1;
            }

            AnsiConsole.MarkupLine("[green]Webhook event processed successfully.[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Fatal error:[/] {ex.Message}");
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    /// <summary>
    /// Settings for the receive command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the webhook event name (e.g., 'issues.opened', 'pull_request').
        /// </summary>
        [Description("Webhook event name (e.g., 'issues.opened', 'pull_request')")]
        [CommandOption("-e|--event")]
        public string EventName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the path to app module.
        /// </summary>
        [Description("Path to app module")]
        [CommandArgument(0, "<appPath>")]
        public string AppPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the path to JSON payload file (if not provided, reads from stdin).
        /// </summary>
        [Description("Path to JSON payload file (if not provided, reads from stdin)")]
        [CommandOption("-f|--file")]
        public string? PayloadFile { get; set; }

        /// <summary>
        /// Gets or sets the GitHub personal access token (for testing).
        /// </summary>
        [Description("GitHub personal access token (for testing)")]
        [CommandOption("-t|--token")]
        public string? Token { get; set; }

        /// <summary>
        /// Gets or sets the GitHub App ID.
        /// </summary>
        [Description("GitHub App ID")]
        [CommandOption("--app-id")]
        public long? AppId { get; set; }

        /// <summary>
        /// Gets or sets the path to private key PEM file.
        /// </summary>
        [Description("Path to private key PEM file")]
        [CommandOption("--private-key")]
        public string? PrivateKeyFile { get; set; }

        /// <summary>
        /// Gets or sets the GitHub API base URL (for GitHub Enterprise).
        /// </summary>
        [Description("GitHub API base URL (for GitHub Enterprise)")]
        [CommandOption("--base-url")]
#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility
        public string? BaseUrl { get; set; }
#pragma warning restore CA1056

        /// <summary>
        /// Gets or sets the log level (trace, debug, info, warn, error, fatal).
        /// </summary>
        [Description("Log level (trace, debug, info, warn, error, fatal)")]
        [CommandOption("--log-level")]
        [DefaultValue("info")]
        public string? LogLevel { get; set; } = "info";

        /// <summary>
        /// Gets or sets the log format (json or pretty).
        /// </summary>
        [Description("Log format (json or pretty)")]
        [CommandOption("--log-format")]
        [DefaultValue("pretty")]
        public string? LogFormat { get; set; } = "pretty";

        /// <summary>
        /// Gets or sets a value indicating whether to include log level in string format.
        /// </summary>
        [Description("Include log level in string format")]
        [CommandOption("--log-level-in-string")]
        [DefaultValue(false)]
        public bool LogLevelInString { get; set; }

        /// <summary>
        /// Gets or sets the custom log message key.
        /// </summary>
        [Description("Custom log message key")]
        [CommandOption("--log-message-key")]
        public string? LogMessageKey { get; set; }

        /// <summary>
        /// Gets or sets the Sentry DSN for error tracking.
        /// </summary>
        [Description("Sentry DSN for error tracking")]
        [CommandOption("--sentry-dsn")]
        public string? SentryDsn { get; set; }

        /// <summary>
        /// Validates the command settings.
        /// </summary>
        /// <returns>Validation result.</returns>
        public override ValidationResult Validate()
        {
            if (string.IsNullOrWhiteSpace(this.AppPath))
            {
                return ValidationResult.Error("App path is required");
            }

            if (string.IsNullOrWhiteSpace(this.EventName))
            {
                return ValidationResult.Error("Event name is required (use -e or --event)");
            }

            return ValidationResult.Success();
        }
    }
}
