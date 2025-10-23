// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Globalization;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Infrastructure.Adapters.Workers;

/// <summary>
/// File-system backed implementation of <see cref="IWebhookReplayQueuePort"/>.
/// Each replay command is stored as an individual JSON file for simple durability across restarts.
/// Designed for local development and as a reference implementation for durable queue providers.
/// </summary>
public sealed class FileSystemWebhookReplayQueueAdapter : IWebhookReplayQueuePort
{
    private const string FileExtension = ".json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly string _queueDirectory;
    private readonly ILogger<FileSystemWebhookReplayQueueAdapter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemWebhookReplayQueueAdapter"/> class.
    /// </summary>
    /// <param name="queueDirectory">Directory used to persist replay commands.</param>
    /// <param name="logger">Application logger.</param>
    public FileSystemWebhookReplayQueueAdapter(string queueDirectory, ILogger<FileSystemWebhookReplayQueueAdapter> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueDirectory);
        ArgumentNullException.ThrowIfNull(logger);

        this._queueDirectory = queueDirectory;
        this._logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> EnqueueAsync(EnqueueReplayCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            Directory.CreateDirectory(this._queueDirectory);

            var fileName = string.Create(CultureInfo.InvariantCulture, $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{FileExtension}");
            var filePath = Path.Combine(this._queueDirectory, fileName);

            var payload = ReplayQueueItem.FromCommand(command);
            var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            try
            {
                await JsonSerializer.SerializeAsync(stream, payload, SerializerOptions, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }

            WorkerLogMessages.ReplayCommandPersisted(this._logger, command.Command.DeliveryId.Value, command.Attempt + 1, fileName);
            return Result.Success();
        }
        catch (IOException ex)
        {
            WorkerLogMessages.ReplayQueueWriteFailed(this._logger, command.Command.DeliveryId.Value, ex);
            return Result.Failure("replay_queue_write_failed", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            WorkerLogMessages.ReplayQueueWriteFailed(this._logger, command.Command.DeliveryId.Value, ex);
            return Result.Failure("replay_queue_write_failed", ex.Message);
        }
        catch (JsonException ex)
        {
            WorkerLogMessages.ReplayQueueSerializationFailed(this._logger, command.Command.DeliveryId.Value, ex);
            return Result.Failure("replay_queue_write_failed", ex.Message);
        }
        catch (Exception ex)
        {
            WorkerLogMessages.ReplayQueueWriteFailed(this._logger, command.Command.DeliveryId.Value, ex);
            return Result.Failure("replay_queue_write_failed", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<EnqueueReplayCommand?>> DequeueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(this._queueDirectory);

            var nextFile = Directory.EnumerateFiles(this._queueDirectory, FormattableString.Invariant($"*{FileExtension}"))
                .OrderBy(path => path, StringComparer.Ordinal)
                .FirstOrDefault();

            if (nextFile is null)
            {
                return Result<EnqueueReplayCommand?>.Success(null);
            }

            ReplayQueueItem? item;
            var stream = new FileStream(nextFile, FileMode.Open, FileAccess.Read, FileShare.None);
            try
            {
                item = await JsonSerializer.DeserializeAsync<ReplayQueueItem>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }

            if (item is null)
            {
                WorkerLogMessages.ReplayQueueEmptyFile(this._logger, nextFile);
                return Result<EnqueueReplayCommand?>.Failure("replay_queue_deserialization_failed", "Queue item could not be read.");
            }

            try
            {
                File.Delete(nextFile);
            }
            catch (IOException ex)
            {
                WorkerLogMessages.ReplayQueueDeleteFailed(this._logger, nextFile, ex);
                return Result<EnqueueReplayCommand?>.Failure("replay_queue_delete_failed", ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                WorkerLogMessages.ReplayQueueDeleteFailed(this._logger, nextFile, ex);
                return Result<EnqueueReplayCommand?>.Failure("replay_queue_delete_failed", ex.Message);
            }

            var command = item.ToEnqueueCommand();
            WorkerLogMessages.ReplayCommandDequeued(this._logger, command.Command.DeliveryId.Value, command.Attempt + 1);
            return Result<EnqueueReplayCommand?>.Success(command);
        }
        catch (JsonException ex)
        {
            WorkerLogMessages.ReplayQueueReadFailed(this._logger, this._queueDirectory, ex);
            return Result<EnqueueReplayCommand?>.Failure("replay_queue_read_failed", ex.Message);
        }
        catch (IOException ex)
        {
            WorkerLogMessages.ReplayQueueReadFailed(this._logger, this._queueDirectory, ex);
            return Result<EnqueueReplayCommand?>.Failure("replay_queue_read_failed", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            WorkerLogMessages.ReplayQueueReadFailed(this._logger, this._queueDirectory, ex);
            return Result<EnqueueReplayCommand?>.Failure("replay_queue_read_failed", ex.Message);
        }
        catch (Exception ex)
        {
            WorkerLogMessages.ReplayQueueReadFailed(this._logger, this._queueDirectory, ex);
            return Result<EnqueueReplayCommand?>.Failure("replay_queue_read_failed", ex.Message);
        }
    }

    /// <summary>
    /// Represents a serializable queue item for persisting replay commands to disk.
    /// </summary>
    /// <param name="DeliveryId">The webhook delivery identifier.</param>
    /// <param name="EventName">The GitHub event name.</param>
    /// <param name="Payload">The JSON payload body.</param>
    /// <param name="InstallationIdValue">The optional installation identifier.</param>
    /// <param name="Signature">The webhook signature.</param>
    /// <param name="RawPayload">The raw unprocessed payload.</param>
    /// <param name="Attempt">The retry attempt number.</param>
    /// <param name="EnqueuedAt">The timestamp when the item was enqueued.</param>
    private sealed record ReplayQueueItem(
        string DeliveryId,
        string EventName,
        string Payload,
        long? InstallationIdValue,
        string Signature,
        string RawPayload,
        int Attempt,
        DateTimeOffset EnqueuedAt)
    {
        /// <summary>
        /// Creates a queue item from an enqueue replay command.
        /// </summary>
        /// <param name="command">The command to serialize.</param>
        /// <returns>A serializable queue item.</returns>
        public static ReplayQueueItem FromCommand(EnqueueReplayCommand command)
            => new(
                command.Command.DeliveryId.Value,
                command.Command.EventName.Value,
                command.Command.Payload.RawBody,
                command.Command.InstallationId?.Value,
                command.Command.Signature.Value,
                command.Command.RawPayload,
                command.Attempt,
                DateTimeOffset.UtcNow);

        /// <summary>
        /// Converts the queue item back to an enqueue replay command.
        /// </summary>
        /// <returns>The deserialized command.</returns>
        public EnqueueReplayCommand ToEnqueueCommand()
        {
            var processCommand = new ProcessWebhookCommand(
                Domain.ValueObjects.DeliveryId.Create(this.DeliveryId),
                WebhookEventName.Create(this.EventName),
                WebhookPayload.Create(this.RawPayload),
                this.InstallationIdValue is null ? null : InstallationId.Create(this.InstallationIdValue.Value),
                WebhookSignature.Create(this.Signature),
                this.RawPayload);

            return new EnqueueReplayCommand(processCommand, this.Attempt);
        }
    }
}
