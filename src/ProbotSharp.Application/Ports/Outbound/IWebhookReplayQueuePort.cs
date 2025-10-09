// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

public interface IWebhookReplayQueuePort
{
    Task<Result> EnqueueAsync(EnqueueReplayCommand command, CancellationToken cancellationToken = default);
    Task<Result<EnqueueReplayCommand?>> DequeueAsync(CancellationToken cancellationToken = default);
}

