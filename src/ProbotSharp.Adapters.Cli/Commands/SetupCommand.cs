// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Diagnostics;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Domain.ValueObjects;

using Spectre.Console;
using Spectre.Console.Cli;

namespace ProbotSharp.Adapters.Cli.Commands;

/// <summary>
/// Interactive setup wizard for creating a new GitHub App.
/// Guides users through the GitHub App creation process.
/// </summary>
public sealed class SetupCommand : AsyncCommand<SetupCommand.Settings>
{
    private readonly ISetupWizardPort _setupWizardPort;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetupCommand"/> class.
    /// </summary>
    /// <param name="setupWizardPort">The setup wizard port for GitHub App creation.</param>
    public SetupCommand(ISetupWizardPort setupWizardPort)
    {
        this._setupWizardPort = setupWizardPort;
    }

    // CA1849: AnsiConsole.Ask/Confirm are intentionally synchronous - CLI requires blocking user input
#pragma warning disable CA1849
    /// <summary>
    /// Executes the interactive setup wizard to create a GitHub App.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings including port and prompt options.</param>
    /// <returns>Exit code: 0 for success, 1 for failure.</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            // Display welcome banner
            var panel = new Panel(
                new Markup(
                    "[bold blue]ProbotSharp Setup Wizard[/]\n\n" +
                    "This wizard will help you create a new GitHub App.\n" +
                    "You'll need:\n" +
                    "  • A GitHub account\n" +
                    "  • Repository admin access (for testing)\n" +
                    "  • A publicly accessible webhook URL (or use a proxy like Smee.io)"))
            {
                Border = BoxBorder.Rounded,
                Padding = new Padding(2, 1),
            };
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();

            // Collect setup information
            var appName = AnsiConsole.Ask<string>("[cyan]GitHub App name:[/]");
            var webhookUrl = AnsiConsole.Ask<string>("[cyan]Webhook URL (or leave empty to use Smee.io proxy):[/]");
            var description = AnsiConsole.Ask("[cyan]App description:[/]", string.Empty);

            // Permissions selection
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Select repository permissions:[/]");
            var permissions = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Choose [green]permissions[/]:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more permissions)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
                    .AddChoices(new[]
                    {
                        "Contents (Read & Write)",
                        "Issues (Read & Write)",
                        "Pull Requests (Read & Write)",
                        "Metadata (Read only)",
                        "Checks (Read & Write)",
                        "Statuses (Read & Write)",
                        "Deployments (Read & Write)",
                    }));

            // Events selection
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Select webhook events:[/]");
            var events = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Choose [green]events[/] to subscribe to:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more events)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
                    .AddChoices(new[]
                    {
                        "Push",
                        "Pull request",
                        "Issues",
                        "Issue comment",
                        "Check run",
                        "Check suite",
                        "Status",
                        "Deployment",
                        "Release",
                        "Pull request review",
                    }));

            // Port configuration
            var port = settings.Port;
            if (!settings.SkipPrompt)
            {
                port = AnsiConsole.Ask("[cyan]Local server port:[/]", 3000);
            }

            // Determine base URL for local server
            var baseUrl = $"http://localhost:{port}";

            // Create webhook channel if no webhook URL provided
            string? webhookProxyUrl = null;
            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                AnsiConsole.MarkupLine("[yellow]No webhook URL provided. Creating Smee.io proxy channel...[/]");
                var channelResult = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Creating webhook proxy channel...", async ctx =>
                    {
                        var channelCommand = new CreateWebhookChannelCommand();
                        return await this._setupWizardPort.CreateWebhookChannelAsync(channelCommand).ConfigureAwait(false);
                    }).ConfigureAwait(false);

                if (channelResult.IsSuccess && channelResult.Value is { } channel)
                {
                    webhookProxyUrl = channel.WebhookProxyUrl;
                    AnsiConsole.MarkupLine($"[green]Webhook proxy created:[/] {webhookProxyUrl}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Warning:[/] Could not create webhook proxy. You'll need to configure webhooks manually.");
                }
            }
            else
            {
                webhookProxyUrl = webhookUrl;
            }

            // Confirmation
            AnsiConsole.WriteLine();
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[bold]Setting[/]")
                .AddColumn("[bold]Value[/]")
                .AddRow("App Name", appName)
                .AddRow("Webhook URL", webhookProxyUrl ?? "[dim](none)[/]")
                .AddRow("Description", string.IsNullOrWhiteSpace(description) ? "[dim](none)[/]" : description)
                .AddRow("Permissions", string.Join(", ", permissions))
                .AddRow("Events", string.Join(", ", events))
                .AddRow("Port", port.ToString())
                .AddRow("Base URL", baseUrl);

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            var confirm = AnsiConsole.Confirm("[yellow]Create GitHub App with these settings?[/]", true);
            if (!confirm)
            {
                AnsiConsole.MarkupLine("[yellow]Setup cancelled.[/]");
                return 0;
            }

            // Create manifest via setup wizard port
            AnsiConsole.WriteLine();
            var manifestResponse = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Generating GitHub App manifest...", async ctx =>
                {
                    var manifestCommand = new GetManifestCommand(
                        BaseUrl: baseUrl,
                        AppName: appName,
                        Description: description,
                        HomepageUrl: null,
                        WebhookProxyUrl: webhookProxyUrl,
                        IsPublic: false);

                    return await this._setupWizardPort.GetManifestAsync(manifestCommand).ConfigureAwait(false);
                }).ConfigureAwait(false);

            if (!manifestResponse.IsSuccess || manifestResponse.Value is null)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Failed to create manifest: {manifestResponse.Error?.Message ?? "Unknown error"}");
                return 1;
            }

            var manifest = manifestResponse.Value;
            AnsiConsole.MarkupLine("[green]Manifest created successfully![/]");
            AnsiConsole.WriteLine();

            // Display next steps with GitHub URL
            var instructionsPanel = new Panel(
                new Markup(
                    "[bold yellow]Next Steps:[/]\n\n" +
                    "1. Opening your browser to create the GitHub App...\n" +
                    $"2. URL: [cyan]{manifest.CreateAppUrl}[/]\n" +
                    "3. Review the permissions and click 'Create GitHub App'\n" +
                    "4. After creation, you'll be redirected back to complete setup\n\n" +
                    "[dim]If the browser doesn't open automatically, copy the URL above.[/]"))
            {
                Border = BoxBorder.Rounded,
                Padding = new Padding(2, 1),
            };
            AnsiConsole.Write(instructionsPanel);
            AnsiConsole.WriteLine();

            // Try to open browser
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = manifest.CreateAppUrl.ToString(),
                    UseShellExecute = true,
                };
                Process.Start(psi);
                AnsiConsole.MarkupLine("[green]Browser opened successfully.[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Could not open browser automatically:[/] {ex.Message}");
                AnsiConsole.MarkupLine("[yellow]Please open the URL manually.[/]");
            }

            // Offer options for completing setup
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Setup Options:[/]");
            var setupChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("How would you like to [green]complete setup[/]?")
                    .AddChoices(new[]
                    {
                        "Manual credential entry (recommended for CLI)",
                        "Wait for OAuth callback (requires running server)",
                        "Skip for now (complete later via web)",
                    }));

            if (setupChoice.StartsWith("Manual"))
            {
                // Manual credential import flow
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]After creating the GitHub App in your browser:[/]");
                AnsiConsole.MarkupLine("1. Note the [cyan]App ID[/] from the app settings page");
                AnsiConsole.MarkupLine("2. Generate and download a [cyan]private key[/]");
                AnsiConsole.MarkupLine("3. Copy the [cyan]webhook secret[/] if you set one");
                AnsiConsole.WriteLine();

                var readyToContinue = AnsiConsole.Confirm("[yellow]Have you created the app and have the credentials ready?[/]", false);
                if (!readyToContinue)
                {
                    AnsiConsole.MarkupLine("[yellow]Setup paused. Run 'probot-sharp setup' again when ready.[/]");
                    return 0;
                }

                // Collect credentials
                var appId = AnsiConsole.Ask<long>("[cyan]GitHub App ID:[/]");
                var privateKeyPath = AnsiConsole.Ask<string>("[cyan]Path to private key file:[/]");
                var webhookSecret = AnsiConsole.Ask("[cyan]Webhook secret (leave empty if none):[/]", string.Empty);

                // Validate private key file exists
                if (!File.Exists(privateKeyPath))
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] Private key file not found: {privateKeyPath}");
                    return 1;
                }

                // Read private key
                string privateKey;
                try
                {
                    privateKey = await File.ReadAllTextAsync(privateKeyPath).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error reading private key:[/] {ex.Message}");
                    return 1;
                }

                // Import credentials via setup wizard port
                var importResult = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Importing credentials...", async ctx =>
                    {
                        var importCommand = new ImportAppCredentialsCommand(
                            AppId: GitHubAppId.Create(appId),
                            PrivateKey: PrivateKeyPem.Create(privateKey),
                            WebhookSecret: string.IsNullOrWhiteSpace(webhookSecret) ? null : webhookSecret);

                        return await this._setupWizardPort.ImportAppCredentialsAsync(importCommand).ConfigureAwait(false);
                    }).ConfigureAwait(false);

                if (!importResult.IsSuccess)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] Failed to import credentials: {importResult.Error?.Message ?? "Unknown error"}");
                    return 1;
                }

                AnsiConsole.MarkupLine("[green]✓ Credentials imported successfully![/]");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold green]Setup complete![/]");
                AnsiConsole.MarkupLine($"You can now run your app with: [cyan]probot-sharp run ./your-app[/]");

                return 0;
            }
            else if (setupChoice.StartsWith("Wait"))
            {
                // OAuth callback flow (requires running server)
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]OAuth callback flow requires a running server.[/]");
                AnsiConsole.MarkupLine("This flow is better suited for the web-based setup.");
                AnsiConsole.MarkupLine($"Run: [cyan]probot-sharp run --port {port}[/] and navigate to the setup page.");
                return 0;
            }
            else
            {
                // Skip - user will complete later
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]Setup paused.[/]");
                AnsiConsole.MarkupLine("You can complete setup later by:");
                AnsiConsole.MarkupLine("  • Running [cyan]probot-sharp setup[/] again");
                AnsiConsole.MarkupLine("  • Or running the server and visiting the setup page");
                return 0;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Fatal error:[/] {ex.Message}");
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
#pragma warning restore CA1849

    /// <summary>
    /// Settings for the setup command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the server port for the setup wizard.
        /// </summary>
        [Description("Server port for setup wizard")]
        [CommandOption("-p|--port")]
        [DefaultValue(3000)]
        public int Port { get; set; } = 3000;

        /// <summary>
        /// Gets or sets a value indicating whether to skip interactive prompts and use defaults.
        /// </summary>
        [Description("Skip interactive prompts and use defaults")]
        [CommandOption("--skip-prompt")]
        [DefaultValue(false)]
        public bool SkipPrompt { get; set; }

        /// <summary>
        /// Validates the command settings.
        /// </summary>
        /// <returns>Validation result indicating success or error.</returns>
        public override ValidationResult Validate()
        {
            if (this.Port < 1 || this.Port > 65535)
            {
                return ValidationResult.Error("Port must be between 1 and 65535");
            }

            return ValidationResult.Success();
        }
    }
}
