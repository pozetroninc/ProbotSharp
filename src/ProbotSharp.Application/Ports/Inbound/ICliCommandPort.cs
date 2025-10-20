// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Inbound;

/// <summary>
/// Inbound port for CLI command execution.
/// Handles run, receive, help, and version commands from the command-line interface.
/// </summary>
public interface ICliCommandPort
{
    /// <summary>
    /// Executes the 'run' command to start the Probot server with one or more app handlers.
    /// Loads app modules from the specified paths and starts the webhook server.
    /// </summary>
    /// <param name="command">Run command with server options and app paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or error result.</returns>
    Task<Result> ExecuteRunAsync(RunCliCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the 'receive' command to simulate a single webhook event delivery.
    /// Loads an app handler, reads a payload from file, and processes it as a webhook event.
    /// Used for local testing and debugging.
    /// </summary>
    /// <param name="command">Receive command with event name, payload path, and app path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or error result.</returns>
    Task<Result> ExecuteReceiveAsync(ReceiveEventCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the version information of the Probot application.
    /// </summary>
    /// <param name="query">Version query (parameter-less).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Version string if successful, error otherwise.</returns>
    Task<Result<string>> GetVersionAsync(GetVersionQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves help text for CLI commands.
    /// If a command name is specified, returns help for that command; otherwise returns general help.
    /// </summary>
    /// <param name="query">Help query with optional command name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Help text if successful, error otherwise.</returns>
    Task<Result<string>> GetHelpAsync(GetHelpQuery query, CancellationToken cancellationToken = default);
}
