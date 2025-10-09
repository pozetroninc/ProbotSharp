// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Infrastructure.Adapters.Workers;

/// <summary>
/// File-system backed implementation of <see cref="IDeadLetterQueuePort"/>.
/// Each dead-letter item is stored as an individual JSON file for durability and manual inspection.
/// </summary>
public sealed class FileSystemDeadLetterQueueAdapter : IDeadLetterQueuePort
{
    private const string FileExtension = ".json";
    private const string DeadLetterPrefix = "dlq-";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true, // Pretty-print for manual inspection
    };

    private readonly string _deadLetterDirectory;
    private readonly ILogger<FileSystemDeadLetterQueueAdapter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemDeadLetterQueueAdapter"/> class.
    /// </summary>
    /// <param name="deadLetterDirectory">Directory used to persist dead-letter items.</param>
    /// <param name="logger">Application logger.</param>
    public FileSystemDeadLetterQueueAdapter(string deadLetterDirectory, ILogger<FileSystemDeadLetterQueueAdapter> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deadLetterDirectory);
        ArgumentNullException.ThrowIfNull(logger);

        this._deadLetterDirectory = deadLetterDirectory;
        this._logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> MoveToDeadLetterAsync(EnqueueReplayCommand command, string reason, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        try
        {
            Directory.CreateDirectory(this._deadLetterDirectory);

            var id = string.Create(CultureInfo.InvariantCulture, $"{DeadLetterPrefix}{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}");
            var fileName = string.Create(CultureInfo.InvariantCulture, $"{id}{FileExtension}");
            var filePath = Path.Combine(this._deadLetterDirectory, fileName);

            var deadLetterItem = new DeadLetterItem(
                Id: id,
                Command: command,
                Reason: reason,
                FailedAt: DateTimeOffset.UtcNow,
                LastError: null);

            var payload = DeadLetterQueueItem.FromDeadLetterItem(deadLetterItem);
            await using (var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, payload, SerializerOptions, cancellationToken).ConfigureAwait(false);
            }

            DeadLetterLogMessages.DeadLetterItemCreated(this._logger, command.Command.DeliveryId.Value, id, reason);
            return Result.Success();
        }
        catch (IOException ex)
        {
            DeadLetterLogMessages.DeadLetterWriteFailed(this._logger, command.Command.DeliveryId.Value, ex);
            return Result.Failure("dead_letter_write_failed", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            DeadLetterLogMessages.DeadLetterWriteFailed(this._logger, command.Command.DeliveryId.Value, ex);
            return Result.Failure("dead_letter_write_failed", ex.Message);
        }
        catch (JsonException ex)
        {
            DeadLetterLogMessages.DeadLetterSerializationFailed(this._logger, command.Command.DeliveryId.Value, ex);
            return Result.Failure("dead_letter_write_failed", ex.Message);
        }
        catch (Exception ex)
        {
            DeadLetterLogMessages.DeadLetterWriteFailed(this._logger, command.Command.DeliveryId.Value, ex);
            return Result.Failure("dead_letter_write_failed", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DeadLetterItem>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(this._deadLetterDirectory);

            var files = Directory.EnumerateFiles(this._deadLetterDirectory, FormattableString.Invariant($"{DeadLetterPrefix}*{FileExtension}"))
                .OrderByDescending(path => path, StringComparer.Ordinal)
                .ToList();

            var items = new List<DeadLetterItem>(files.Count);

            foreach (var file in files)
            {
                var itemResult = await ReadDeadLetterItemAsync(file, cancellationToken).ConfigureAwait(false);
                if (itemResult.IsSuccess && itemResult.Value is not null)
                {
                    items.Add(itemResult.Value);
                }
            }

            return Result<IReadOnlyList<DeadLetterItem>>.Success(items);
        }
        catch (IOException ex)
        {
            DeadLetterLogMessages.DeadLetterReadFailed(this._logger, this._deadLetterDirectory, ex);
            return Result<IReadOnlyList<DeadLetterItem>>.Failure("dead_letter_read_failed", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            DeadLetterLogMessages.DeadLetterReadFailed(this._logger, this._deadLetterDirectory, ex);
            return Result<IReadOnlyList<DeadLetterItem>>.Failure("dead_letter_read_failed", ex.Message);
        }
        catch (Exception ex)
        {
            DeadLetterLogMessages.DeadLetterReadFailed(this._logger, this._deadLetterDirectory, ex);
            return Result<IReadOnlyList<DeadLetterItem>>.Failure("dead_letter_read_failed", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<DeadLetterItem?>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        try
        {
            Directory.CreateDirectory(this._deadLetterDirectory);

            var fileName = string.Create(CultureInfo.InvariantCulture, $"{id}{FileExtension}");
            var filePath = Path.Combine(this._deadLetterDirectory, fileName);

            if (!File.Exists(filePath))
            {
                return Result<DeadLetterItem?>.Success(null);
            }

            return await ReadDeadLetterItemAsync(filePath, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            DeadLetterLogMessages.DeadLetterReadFailed(this._logger, id, ex);
            return Result<DeadLetterItem?>.Failure("dead_letter_read_failed", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            DeadLetterLogMessages.DeadLetterReadFailed(this._logger, id, ex);
            return Result<DeadLetterItem?>.Failure("dead_letter_read_failed", ex.Message);
        }
        catch (Exception ex)
        {
            DeadLetterLogMessages.DeadLetterReadFailed(this._logger, id, ex);
            return Result<DeadLetterItem?>.Failure("dead_letter_read_failed", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<EnqueueReplayCommand?>> RequeueAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        try
        {
            var itemResult = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (!itemResult.IsSuccess)
            {
                return Result<EnqueueReplayCommand?>.Failure(itemResult.Error!.Value);
            }

            if (itemResult.Value is null)
            {
                return Result<EnqueueReplayCommand?>.Success(null);
            }

            var fileName = string.Create(CultureInfo.InvariantCulture, $"{id}{FileExtension}");
            var filePath = Path.Combine(this._deadLetterDirectory, fileName);

            try
            {
                File.Delete(filePath);
            }
            catch (IOException ex)
            {
                DeadLetterLogMessages.DeadLetterDeleteFailed(this._logger, id, ex);
                return Result<EnqueueReplayCommand?>.Failure("dead_letter_delete_failed", ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                DeadLetterLogMessages.DeadLetterDeleteFailed(this._logger, id, ex);
                return Result<EnqueueReplayCommand?>.Failure("dead_letter_delete_failed", ex.Message);
            }

            DeadLetterLogMessages.DeadLetterItemRequeued(this._logger, itemResult.Value.Command.Command.DeliveryId.Value, id);

            // Reset attempt count to 0 for manual retry
            var resetCommand = new EnqueueReplayCommand(itemResult.Value.Command.Command, 0);
            return Result<EnqueueReplayCommand?>.Success(resetCommand);
        }
        catch (Exception ex)
        {
            DeadLetterLogMessages.DeadLetterReadFailed(this._logger, id, ex);
            return Result<EnqueueReplayCommand?>.Failure("dead_letter_requeue_failed", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        try
        {
            var fileName = string.Create(CultureInfo.InvariantCulture, $"{id}{FileExtension}");
            var filePath = Path.Combine(this._deadLetterDirectory, fileName);

            if (!File.Exists(filePath))
            {
                return Result.Failure("dead_letter_not_found", $"Dead-letter item {id} not found");
            }

            File.Delete(filePath);
            DeadLetterLogMessages.DeadLetterItemDeleted(this._logger, id);
            return Result.Success();
        }
        catch (IOException ex)
        {
            DeadLetterLogMessages.DeadLetterDeleteFailed(this._logger, id, ex);
            return Result.Failure("dead_letter_delete_failed", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            DeadLetterLogMessages.DeadLetterDeleteFailed(this._logger, id, ex);
            return Result.Failure("dead_letter_delete_failed", ex.Message);
        }
        catch (Exception ex)
        {
            DeadLetterLogMessages.DeadLetterDeleteFailed(this._logger, id, ex);
            return Result.Failure("dead_letter_delete_failed", ex.Message);
        }
    }

    private async Task<Result<DeadLetterItem?>> ReadDeadLetterItemAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            DeadLetterQueueItem? item;
            await using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                item = await JsonSerializer.DeserializeAsync<DeadLetterQueueItem>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
            }

            if (item is null)
            {
                DeadLetterLogMessages.DeadLetterEmptyFile(this._logger, filePath);
                return Result<DeadLetterItem?>.Success(null);
            }

            var deadLetterItem = item.ToDeadLetterItem();
            return Result<DeadLetterItem?>.Success(deadLetterItem);
        }
        catch (JsonException ex)
        {
            DeadLetterLogMessages.DeadLetterDeserializationFailed(this._logger, filePath, ex);
            return Result<DeadLetterItem?>.Failure("dead_letter_deserialization_failed", ex.Message);
        }
    }

    /// <summary>
    /// Represents a serializable dead-letter queue item for persisting to disk.
    /// </summary>
    private sealed record DeadLetterQueueItem(
        string Id,
        string DeliveryId,
        string EventName,
        string Payload,
        long? InstallationIdValue,
        string Signature,
        string RawPayload,
        int Attempt,
        string Reason,
        DateTimeOffset FailedAt,
        string? LastError)
    {
        public static DeadLetterQueueItem FromDeadLetterItem(DeadLetterItem item)
            => new(
                item.Id,
                item.Command.Command.DeliveryId.Value,
                item.Command.Command.EventName.Value,
                item.Command.Command.Payload.RawBody,
                item.Command.Command.InstallationId?.Value,
                item.Command.Command.Signature.Value,
                item.Command.Command.RawPayload,
                item.Command.Attempt,
                item.Reason,
                item.FailedAt,
                item.LastError);

        public DeadLetterItem ToDeadLetterItem()
        {
            var processCommand = new ProcessWebhookCommand(
                Domain.ValueObjects.DeliveryId.Create(this.DeliveryId),
                WebhookEventName.Create(this.EventName),
                WebhookPayload.Create(this.RawPayload),
                this.InstallationIdValue is null ? null : InstallationId.Create(this.InstallationIdValue.Value),
                WebhookSignature.Create(this.Signature),
                this.RawPayload);

            var enqueueCommand = new EnqueueReplayCommand(processCommand, this.Attempt);

            return new DeadLetterItem(
                this.Id,
                enqueueCommand,
                this.Reason,
                this.FailedAt,
                this.LastError);
        }
    }
}
