// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Application.Ports.Inbound;

/// <summary>
/// Inbound port for managing application lifecycle operations.
/// Handles server startup, shutdown, app loading, and lifecycle queries.
/// </summary>
public interface IAppLifecyclePort
{
    /// <summary>
    /// Starts the webhook server with the specified configuration.
    /// If APP_ID and PRIVATE_KEY are not provided, server starts in setup mode.
    /// </summary>
    /// <param name="command">Server configuration command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server information if successful, error otherwise.</returns>
    Task<Result<ServerInfo>> StartServerAsync(StartServerCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the running webhook server.
    /// </summary>
    /// <param name="command">Stop server command with graceful shutdown option.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or error result.</returns>
    Task<Result> StopServerAsync(StopServerCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a Probot application handler from a file path.
    /// </summary>
    /// <param name="command">Load app command with path and working directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or error result.</returns>
    Task<Result> LoadAppAsync(LoadAppCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves current application information including loaded apps and setup mode status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Application information if available, error otherwise.</returns>
    Task<Result<AppInfo>> GetAppInfoAsync(CancellationToken cancellationToken = default);
}
