// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Infrastructure.Adapters.Workers;

/// <summary>
/// In-memory implementation of <see cref="IDeadLetterQueuePort"/>.
/// Items are stored in memory and will be lost on restart.
/// Suitable for development and testing only.
/// </summary>
public sealed class InMemoryDeadLetterQueueAdapter : IDeadLetterQueuePort
{
    private readonly ConcurrentDictionary<string, DeadLetterItem> _items = new();
    private readonly ILogger<InMemoryDeadLetterQueueAdapter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryDeadLetterQueueAdapter"/> class.
    /// </summary>
    /// <param name="logger">Application logger.</param>
    public InMemoryDeadLetterQueueAdapter(ILogger<InMemoryDeadLetterQueueAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        this._logger = logger;
    }

    /// <inheritdoc />
    public Task<Result> MoveToDeadLetterAsync(EnqueueReplayCommand command, string reason, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var id = $"dlq-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
        var deadLetterItem = new DeadLetterItem(
            Id: id,
            Command: command,
            Reason: reason,
            FailedAt: DateTimeOffset.UtcNow,
            LastError: null);

        if (!this._items.TryAdd(id, deadLetterItem))
        {
            this._logger.LogWarning("Failed to add dead-letter item {Id} to in-memory store", id);
            return Task.FromResult(Result.Failure("dead_letter_add_failed", "Failed to add item to in-memory store"));
        }

        this._logger.LogInformation(
            "Dead-letter item created for delivery {DeliveryId} with ID {Id}: {Reason}",
            command.Command.DeliveryId.Value,
            id,
            reason);

        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<DeadLetterItem>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = this._items.Values
            .OrderByDescending(item => item.FailedAt)
            .ToList();

        return Task.FromResult(Result<IReadOnlyList<DeadLetterItem>>.Success(items));
    }

    /// <inheritdoc />
    public Task<Result<DeadLetterItem?>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (this._items.TryGetValue(id, out var item))
        {
            return Task.FromResult(Result<DeadLetterItem?>.Success(item));
        }

        return Task.FromResult(Result<DeadLetterItem?>.Success(null));
    }

    /// <inheritdoc />
    public Task<Result<EnqueueReplayCommand?>> RequeueAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!this._items.TryRemove(id, out var item))
        {
            return Task.FromResult(Result<EnqueueReplayCommand?>.Success(null));
        }

        this._logger.LogInformation(
            "Dead-letter item requeued for delivery {DeliveryId} with ID {Id}",
            item.Command.Command.DeliveryId.Value,
            id);

        // Reset attempt count to 0 for manual retry
        var resetCommand = new EnqueueReplayCommand(item.Command.Command, 0);
        return Task.FromResult(Result<EnqueueReplayCommand?>.Success(resetCommand));
    }

    /// <inheritdoc />
    public Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!this._items.TryRemove(id, out _))
        {
            return Task.FromResult(Result.Failure("dead_letter_not_found", $"Dead-letter item {id} not found"));
        }

        this._logger.LogInformation("Dead-letter item {Id} deleted from in-memory store", id);
        return Task.FromResult(Result.Success());
    }
}
