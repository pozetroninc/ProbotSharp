// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Inbound;

/// <summary>
/// Port for processing incoming webhooks.
/// </summary>
public interface IWebhookProcessingPort
{
    /// <summary>
    /// Processes an incoming webhook delivery.
    /// </summary>
    /// <param name="command">The webhook processing command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ProcessAsync(ProcessWebhookCommand command, CancellationToken cancellationToken = default);
}
