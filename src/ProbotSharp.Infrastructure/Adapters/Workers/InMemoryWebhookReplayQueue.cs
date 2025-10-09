// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Infrastructure.Adapters.Workers;

/// <summary>
/// In-memory implementation of <see cref="IWebhookReplayQueuePort"/> used for testing and non-durable scenarios.
/// </summary>
public sealed class InMemoryWebhookReplayQueueAdapter : IWebhookReplayQueuePort
{
    private readonly ConcurrentQueue<EnqueueReplayCommand> _queue = new();
    private readonly ILogger<InMemoryWebhookReplayQueueAdapter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryWebhookReplayQueueAdapter"/> class.
    /// </summary>
    /// <param name="logger">The application logger.</param>
    public InMemoryWebhookReplayQueueAdapter(ILogger<InMemoryWebhookReplayQueueAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        this._logger = logger;
    }

    /// <inheritdoc />
    public Task<Result> EnqueueAsync(EnqueueReplayCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        this._queue.Enqueue(command);
        WorkerLogMessages.ReplayCommandPersisted(this._logger, command.Command.DeliveryId.Value, command.Attempt + 1, string.Create(CultureInfo.InvariantCulture, $"memory:{Guid.NewGuid():N}"));
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<EnqueueReplayCommand?>> DequeueAsync(CancellationToken cancellationToken = default)
    {
        var hasItem = this._queue.TryDequeue(out var command);
        if (hasItem && command is not null)
        {
            WorkerLogMessages.ReplayCommandDequeued(this._logger, command.Command.DeliveryId.Value, command.Attempt + 1);
        }

        return Task.FromResult(Result<EnqueueReplayCommand?>.Success(hasItem ? command : null));
    }
}

