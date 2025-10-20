// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.ComponentModel;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;

using Spectre.Console;
using Spectre.Console.Cli;

namespace ProbotSharp.Adapters.Cli.Commands;

/// <summary>
/// CLI command to display help information.
/// </summary>
public sealed class HelpCommand : AsyncCommand<HelpCommand.Settings>
{
    private readonly ICliCommandPort _cliPort;

    /// <summary>
    /// Initializes a new instance of the <see cref="HelpCommand"/> class.
    /// </summary>
    /// <param name="cliPort">The CLI command port for retrieving help information.</param>
    public HelpCommand(ICliCommandPort cliPort)
    {
        this._cliPort = cliPort;
    }

    /// <summary>
    /// Executes the help command to display help information.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings including optional command name.</param>
    /// <returns>Exit code: 0 for success, 1 for failure.</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            var query = new GetHelpQuery(settings.CommandName);
            var result = await this._cliPort.GetHelpAsync(query, CancellationToken.None).ConfigureAwait(false);

            if (result.IsSuccess && !string.IsNullOrEmpty(result.Value))
            {
                AnsiConsole.WriteLine(result.Value);
                return 0;
            }

            if (result.Error is { } error)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {error.Message}");
                return 1;
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Fatal error:[/] {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Settings for the help command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the command name to get help for.
        /// </summary>
        [Description("Command name to get help for")]
        [CommandArgument(0, "[command]")]
        public string? CommandName { get; set; }
    }
}
