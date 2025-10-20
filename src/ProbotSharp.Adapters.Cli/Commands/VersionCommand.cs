// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;

using Spectre.Console;
using Spectre.Console.Cli;

namespace ProbotSharp.Adapters.Cli.Commands;

/// <summary>
/// CLI command to display version information.
/// </summary>
public sealed class VersionCommand : AsyncCommand
{
    private readonly ICliCommandPort _cliPort;

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionCommand"/> class.
    /// </summary>
    /// <param name="cliPort">The CLI command port for retrieving version information.</param>
    public VersionCommand(ICliCommandPort cliPort)
    {
        this._cliPort = cliPort;
    }

    /// <summary>
    /// Executes the version command to display version information.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <returns>Exit code: 0 for success, 1 for failure.</returns>
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        try
        {
            var result = await this._cliPort.GetVersionAsync(new GetVersionQuery(), CancellationToken.None).ConfigureAwait(false);

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
