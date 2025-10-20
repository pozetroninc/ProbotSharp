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
/// CLI command to start the Probot development server.
/// </summary>
public sealed class RunCommand : AsyncCommand<RunCommand.Settings>
{
    private readonly ICliCommandPort _cliPort;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunCommand"/> class.
    /// </summary>
    /// <param name="cliPort">The CLI command port.</param>
    public RunCommand(ICliCommandPort cliPort)
    {
        this._cliPort = cliPort;
    }

    /// <summary>
    /// Executes the run command asynchronously.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <returns>Exit code (0 for success, 1 for failure).</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            var rule = new Rule("[blue]ProbotSharp - Starting Development Server[/]")
            {
                Justification = Justify.Left,
            };
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();

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

            // Create command
            var command = new RunCliCommand(
                AppPaths: settings.AppPaths.ToArray(),
                Port: settings.Port,
                Host: settings.Host,
                WebhookPath: settings.WebhookPath,
                WebhookProxy: settings.WebhookProxy,
                AppId: appId,
                PrivateKey: privateKey,
                Secret: settings.Secret,
                BaseUrl: settings.BaseUrl,
                RedisUrl: settings.RedisUrl,
                LogLevel: settings.LogLevel,
                LogFormat: settings.LogFormat,
                LogLevelInString: settings.LogLevelInString,
                LogMessageKey: settings.LogMessageKey,
                SentryDsn: settings.SentryDsn);

            // Execute via port
            var result = await this._cliPort.ExecuteRunAsync(command, CancellationToken.None).ConfigureAwait(false);

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

            // Server is running - this blocks until shutdown
            AnsiConsole.MarkupLine("[green]Server started successfully.[/]");
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
    /// Settings for the run command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the path(s) to app module(s).
        /// </summary>
        [Description("Path(s) to app module(s)")]
        [CommandArgument(0, "[appPaths]")]
        public string[] AppPaths { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the server host.
        /// </summary>
        [Description("Server host")]
        [CommandOption("--host")]
        [DefaultValue("localhost")]
        public string Host { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the server port.
        /// </summary>
        [Description("Server port")]
        [CommandOption("-p|--port")]
        [DefaultValue(3000)]
        public int Port { get; set; } = 3000;

        /// <summary>
        /// Gets or sets the webhook endpoint path.
        /// </summary>
        [Description("Webhook endpoint path")]
        [CommandOption("--webhook-path")]
        public string? WebhookPath { get; set; }

        /// <summary>
        /// Gets or sets the webhook proxy URL (e.g., Smee.io).
        /// </summary>
        [Description("Webhook proxy URL (e.g., Smee.io)")]
        [CommandOption("--webhook-proxy")]
        public string? WebhookProxy { get; set; }

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
        /// Gets or sets the webhook secret.
        /// </summary>
        [Description("Webhook secret")]
        [CommandOption("--secret")]
        public string? Secret { get; set; }

        /// <summary>
        /// Gets or sets the GitHub API base URL (for GitHub Enterprise).
        /// </summary>
        [Description("GitHub API base URL (for GitHub Enterprise)")]
        [CommandOption("--base-url")]
#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility
        public string? BaseUrl { get; set; }

        /// <summary>
        /// Gets or sets the Redis connection URL.
        /// </summary>
        [Description("Redis connection URL")]
        [CommandOption("--redis-url")]
        public string? RedisUrl { get; set; }
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
            if (this.AppPaths.Length == 0)
            {
                return ValidationResult.Error("At least one app path must be specified");
            }

            if (this.Port < 1 || this.Port > 65535)
            {
                return ValidationResult.Error("Port must be between 1 and 65535");
            }

            return ValidationResult.Success();
        }
    }
}
