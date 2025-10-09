// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Spectre.Console;
using Spectre.Console.Cli;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;

namespace ProbotSharp.Adapters.Cli.Commands;

/// <summary>
/// CLI command to display version information.
/// </summary>
public sealed class VersionCommand : AsyncCommand
{
    private readonly ICliCommandPort _cliPort;

    public VersionCommand(ICliCommandPort cliPort)
    {
        _cliPort = cliPort;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        try
        {
            var result = await _cliPort.GetVersionAsync(new GetVersionQuery(), CancellationToken.None);

            if (result.IsSuccess && !string.IsNullOrEmpty(result.Value))
            {
                var panel = new Panel(
                    new Markup($"[bold cyan]ProbotSharp[/] version [green]{result.Value}[/]\n\n" +
                              "[dim]A .NET GitHub App framework inspired by Probot[/]"))
                {
                    Border = BoxBorder.Rounded,
                    Padding = new Padding(2, 1),
                };
                AnsiConsole.Write(panel);
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
}
