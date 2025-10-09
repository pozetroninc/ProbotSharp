// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.ComponentModel;

using Spectre.Console;
using Spectre.Console.Cli;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Adapters.Cli.Commands;

/// <summary>
/// CLI command to start the Probot development server.
/// </summary>
public sealed class RunCommand : AsyncCommand<RunCommand.Settings>
{
    private readonly ICliCommandPort _cliPort;

    public RunCommand(ICliCommandPort cliPort)
    {
        _cliPort = cliPort;
    }

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

                var pem = await File.ReadAllTextAsync(settings.PrivateKeyFile);
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
            var result = await _cliPort.ExecuteRunAsync(command, CancellationToken.None);

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

    public sealed class Settings : CommandSettings
    {
        [Description("Path(s) to app module(s)")]
        [CommandArgument(0, "[appPaths]")]
        public string[] AppPaths { get; set; } = Array.Empty<string>();

        [Description("Server host")]
        [CommandOption("--host")]
        [DefaultValue("localhost")]
        public string Host { get; set; } = "localhost";

        [Description("Server port")]
        [CommandOption("-p|--port")]
        [DefaultValue(3000)]
        public int Port { get; set; } = 3000;

        [Description("Webhook endpoint path")]
        [CommandOption("--webhook-path")]
        public string? WebhookPath { get; set; }

        [Description("Webhook proxy URL (e.g., Smee.io)")]
        [CommandOption("--webhook-proxy")]
        public string? WebhookProxy { get; set; }

        [Description("GitHub App ID")]
        [CommandOption("--app-id")]
        public long? AppId { get; set; }

        [Description("Path to private key PEM file")]
        [CommandOption("--private-key")]
        public string? PrivateKeyFile { get; set; }

        [Description("Webhook secret")]
        [CommandOption("--secret")]
        public string? Secret { get; set; }

        [Description("GitHub API base URL (for GitHub Enterprise)")]
        [CommandOption("--base-url")]
        public string? BaseUrl { get; set; }

        [Description("Redis connection URL")]
        [CommandOption("--redis-url")]
        public string? RedisUrl { get; set; }

        [Description("Log level (trace, debug, info, warn, error, fatal)")]
        [CommandOption("--log-level")]
        [DefaultValue("info")]
        public string? LogLevel { get; set; } = "info";

        [Description("Log format (json or pretty)")]
        [CommandOption("--log-format")]
        [DefaultValue("pretty")]
        public string? LogFormat { get; set; } = "pretty";

        [Description("Include log level in string format")]
        [CommandOption("--log-level-in-string")]
        [DefaultValue(false)]
        public bool LogLevelInString { get; set; }

        [Description("Custom log message key")]
        [CommandOption("--log-message-key")]
        public string? LogMessageKey { get; set; }

        [Description("Sentry DSN for error tracking")]
        [CommandOption("--sentry-dsn")]
        public string? SentryDsn { get; set; }

        public override ValidationResult Validate()
        {
            if (AppPaths.Length == 0)
            {
                return ValidationResult.Error("At least one app path must be specified");
            }

            if (Port < 1 || Port > 65535)
            {
                return ValidationResult.Error("Port must be between 1 and 65535");
            }

            return ValidationResult.Success();
        }
    }
}
