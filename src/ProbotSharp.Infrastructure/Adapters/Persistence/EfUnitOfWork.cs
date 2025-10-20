// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Shared.Abstractions;

#pragma warning disable CA1848 // Performance: LoggerMessage delegates - not performance-critical for this codebase

namespace ProbotSharp.Infrastructure.Adapters.Persistence;

/// <summary>
/// Entity Framework-based implementation of the unit of work pattern for coordinating database transactions.
/// </summary>
public sealed class EfUnitOfWork : IUnitOfWorkPort
{
    private readonly ProbotSharpDbContext _dbContext;
    private readonly ILogger<EfUnitOfWork> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfUnitOfWork"/> class.
    /// </summary>
    /// <param name="dbContext">The database context for transaction management.</param>
    /// <param name="logger">The logger instance.</param>
    public EfUnitOfWork(ProbotSharpDbContext dbContext, ILogger<EfUnitOfWork> logger)
    {
        this._dbContext = dbContext;
        this._logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> ExecuteAsync(Func<CancellationToken, Task<Result>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            var result = await operation(cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                return result;
            }

            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            // CA1031: Catching general exception is intentional here to convert infrastructure errors to Result type
            this._logger.LogError(ex, "Unit of work execution failed");
            return Result.Failure("unit_of_work_failed", ex.Message);
        }
    }
}

#pragma warning restore CA1848
