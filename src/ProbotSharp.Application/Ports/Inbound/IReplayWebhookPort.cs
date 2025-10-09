// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Inbound;

public interface IReplayWebhookPort
{
    Task<Result> ReplayAsync(EnqueueReplayCommand command, CancellationToken cancellationToken = default);
}

