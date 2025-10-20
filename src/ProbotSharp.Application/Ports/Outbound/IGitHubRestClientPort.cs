// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Net.Http;

using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Port for GitHub REST API client operations.
/// </summary>
public interface IGitHubRestClientPort
{
    /// <summary>
    /// Sends an HTTP request using the GitHub REST API client.
    /// </summary>
    /// <param name="action">The action to execute with the HTTP client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the HTTP response or an error.</returns>
    Task<Result<HttpResponseMessage>> SendAsync(Func<HttpClient, Task<HttpResponseMessage>> action, CancellationToken cancellationToken = default);
}
