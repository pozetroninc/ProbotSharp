// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Port for accessing application configuration.
/// </summary>
public interface IAppConfigurationPort
{
    /// <summary>
    /// Retrieves the webhook secret from configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the webhook secret or an error.</returns>
    Task<Result<string>> GetWebhookSecretAsync(CancellationToken cancellationToken = default);
}
