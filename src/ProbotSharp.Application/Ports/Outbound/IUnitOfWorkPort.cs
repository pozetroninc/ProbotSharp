// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

public interface IUnitOfWorkPort
{
    Task<Result> ExecuteAsync(Func<CancellationToken, Task<Result>> operation, CancellationToken cancellationToken = default);
}

