// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Port for creating webhook proxy channels for local development.
/// Enables webhook delivery to localhost during development via services like Smee.io.
/// </summary>
public interface IWebhookChannelPort
{
    /// <summary>
    /// Creates a new webhook proxy channel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Webhook channel response with proxy URL if successful, error otherwise</returns>
    Task<Result<CreateWebhookChannelResponse>> CreateChannelAsync(
        CancellationToken cancellationToken = default);
}
