// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Port for managing unit of work transactions.
/// </summary>
public interface IUnitOfWorkPort
{
    /// <summary>
    /// Executes an operation within a transaction boundary.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ExecuteAsync(Func<CancellationToken, Task<Result>> operation, CancellationToken cancellationToken = default);
}
