// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Inbound;

/// <summary>
/// Port for replaying webhook deliveries.
/// </summary>
public interface IReplayWebhookPort
{
    /// <summary>
    /// Replays a previously received webhook.
    /// </summary>
    /// <param name="command">The replay command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ReplayAsync(EnqueueReplayCommand command, CancellationToken cancellationToken = default);
}
