// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Net.Http;

using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

public interface IGitHubRestClientPort
{
    Task<Result<HttpResponseMessage>> SendAsync(Func<HttpClient, Task<HttpResponseMessage>> action, CancellationToken cancellationToken = default);
}

