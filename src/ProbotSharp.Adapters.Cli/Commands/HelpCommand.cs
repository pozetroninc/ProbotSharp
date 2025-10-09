// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.ComponentModel;

using Spectre.Console;
using Spectre.Console.Cli;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;

namespace ProbotSharp.Adapters.Cli.Commands;

/// <summary>
/// CLI command to display help information.
/// </summary>
public sealed class HelpCommand : AsyncCommand<HelpCommand.Settings>
{
    private readonly ICliCommandPort _cliPort;

    public HelpCommand(ICliCommandPort cliPort)
    {
        _cliPort = cliPort;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            var query = new GetHelpQuery(settings.CommandName);
            var result = await _cliPort.GetHelpAsync(query, CancellationToken.None);

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

    public sealed class Settings : CommandSettings
    {
        [Description("Command name to get help for")]
        [CommandArgument(0, "[command]")]
        public string? CommandName { get; set; }
    }
}
