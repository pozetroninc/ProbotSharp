// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

public interface IAppConfigurationPort
{
    Task<Result<string>> GetWebhookSecretAsync(CancellationToken cancellationToken = default);
}
