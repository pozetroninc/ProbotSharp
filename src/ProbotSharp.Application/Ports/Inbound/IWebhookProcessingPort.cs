// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Inbound;

public interface IWebhookProcessingPort
{
    Task<Result> ProcessAsync(ProcessWebhookCommand command, CancellationToken cancellationToken = default);
}

