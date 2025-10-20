// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Port for queuing webhook replays.
/// </summary>
public interface IWebhookReplayQueuePort
{
    /// <summary>
    /// Enqueues a webhook replay command.
    /// </summary>
    /// <param name="command">The replay command to enqueue.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> EnqueueAsync(EnqueueReplayCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeues a webhook replay command.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The replay command if available; otherwise, null.</returns>
    Task<Result<EnqueueReplayCommand?>> DequeueAsync(CancellationToken cancellationToken = default);
}
