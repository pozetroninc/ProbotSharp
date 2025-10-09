// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Globalization;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Infrastructure.Adapters.Persistence;
using ProbotSharp.Infrastructure.Adapters.Persistence.Models;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Infrastructure.Adapters.Workers;

/// <summary>
/// Database-backed implementation of <see cref="IDeadLetterQueuePort"/>.
/// Items are stored in a database table for durability and scalability.
/// </summary>
public sealed class DatabaseDeadLetterQueueAdapter : IDeadLetterQueuePort
{
    private const string DeadLetterPrefix = "dlq-";

    private readonly ProbotSharpDbContext _dbContext;
    private readonly ILogger<DatabaseDeadLetterQueueAdapter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseDeadLetterQueueAdapter"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">Application logger.</param>
    public DatabaseDeadLetterQueueAdapter(ProbotSharpDbContext dbContext, ILogger<DatabaseDeadLetterQueueAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(logger);

        this._dbContext = dbContext;
        this._logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> MoveToDeadLetterAsync(EnqueueReplayCommand command, string reason, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        try
        {
            var id = string.Create(CultureInfo.InvariantCulture, $"{DeadLetterPrefix}{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}");

            var deadLetterItem = new DeadLetterItem(
                Id: id,
                Command: command,
                Reason: reason,
                FailedAt: DateTimeOffset.UtcNow,
                LastError: null);

            var entity = DeadLetterQueueItemEntity.FromDomain(deadLetterItem);
            await this._dbContext.DeadLetterQueueItems.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation(
                "Dead-letter item created in database for delivery {DeliveryId} with ID {Id}: {Reason}",
                command.Command.DeliveryId.Value,
                id,
                reason);

            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            this._logger.LogError(ex, "Database error writing dead-letter item for delivery {DeliveryId}", command.Command.DeliveryId.Value);
            return Result.Failure("dead_letter_write_failed", ex.Message);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Unexpected error writing dead-letter item for delivery {DeliveryId}", command.Command.DeliveryId.Value);
            return Result.Failure("dead_letter_write_failed", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DeadLetterItem>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await this._dbContext.DeadLetterQueueItems
                .OrderByDescending(x => x.FailedAt)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var items = entities.Select(e => e.ToDomain()).ToList();
            return Result<IReadOnlyList<DeadLetterItem>>.Success(items);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error retrieving all dead-letter items from database");
            return Result<IReadOnlyList<DeadLetterItem>>.Failure("dead_letter_read_failed", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<DeadLetterItem?>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        try
        {
            var entity = await this._dbContext.DeadLetterQueueItems
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                .ConfigureAwait(false);

            if (entity == null)
            {
                return Result<DeadLetterItem?>.Success(null);
            }

            var item = entity.ToDomain();
            return Result<DeadLetterItem?>.Success(item);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error retrieving dead-letter item {Id} from database", id);
            return Result<DeadLetterItem?>.Failure("dead_letter_read_failed", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<EnqueueReplayCommand?>> RequeueAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        try
        {
            var entity = await this._dbContext.DeadLetterQueueItems
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                .ConfigureAwait(false);

            if (entity == null)
            {
                return Result<EnqueueReplayCommand?>.Success(null);
            }

            var item = entity.ToDomain();

            this._dbContext.DeadLetterQueueItems.Remove(entity);
            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation(
                "Dead-letter item requeued from database for delivery {DeliveryId} with ID {Id}",
                item.Command.Command.DeliveryId.Value,
                id);

            // Reset attempt count to 0 for manual retry
            var resetCommand = new EnqueueReplayCommand(item.Command.Command, 0);
            return Result<EnqueueReplayCommand?>.Success(resetCommand);
        }
        catch (DbUpdateException ex)
        {
            this._logger.LogError(ex, "Database error removing dead-letter item {Id}", id);
            return Result<EnqueueReplayCommand?>.Failure("dead_letter_requeue_failed", ex.Message);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error requeuing dead-letter item {Id}", id);
            return Result<EnqueueReplayCommand?>.Failure("dead_letter_requeue_failed", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        try
        {
            var entity = await this._dbContext.DeadLetterQueueItems
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                .ConfigureAwait(false);

            if (entity == null)
            {
                return Result.Failure("dead_letter_not_found", $"Dead-letter item {id} not found");
            }

            this._dbContext.DeadLetterQueueItems.Remove(entity);
            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation("Dead-letter item {Id} deleted from database", id);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            this._logger.LogError(ex, "Database error deleting dead-letter item {Id}", id);
            return Result.Failure("dead_letter_delete_failed", ex.Message);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error deleting dead-letter item {Id}", id);
            return Result.Failure("dead_letter_delete_failed", ex.Message);
        }
    }
}
