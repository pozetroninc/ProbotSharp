// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace ProbotSharp.Infrastructure.Adapters.Workers;

/// <summary>
/// Structured log messages for webhook replay queue worker operations.
/// </summary>
internal static partial class WorkerLogMessages
{
    /// <summary>
    /// Logs when a replay command is successfully persisted to the queue.
    /// </summary>
    [LoggerMessage(EventId = 2700, Level = LogLevel.Information, Message = "Persisted replay command {DeliveryId} (attempt {Attempt}) to {QueueFile}")]
    public static partial void ReplayCommandPersisted(ILogger logger, string deliveryId, int attempt, string queueFile);

    /// <summary>
    /// Logs when a replay queue file is empty after deserialization.
    /// </summary>
    [LoggerMessage(EventId = 2701, Level = LogLevel.Warning, Message = "Replay queue file {QueueFile} was empty after deserialization")]
    public static partial void ReplayQueueEmptyFile(ILogger logger, string queueFile);

    /// <summary>
    /// Logs when a replay command is successfully dequeued.
    /// </summary>
    [LoggerMessage(EventId = 2702, Level = LogLevel.Information, Message = "Dequeued replay command for delivery {DeliveryId} (attempt {Attempt})")]
    public static partial void ReplayCommandDequeued(ILogger logger, string deliveryId, int attempt);

    /// <summary>
    /// Logs when persisting a replay command to the queue fails.
    /// </summary>
    [LoggerMessage(EventId = 2703, Level = LogLevel.Error, Message = "Failed to persist replay command for delivery {DeliveryId}")]
    public static partial void ReplayQueueWriteFailed(ILogger logger, string deliveryId, Exception exception);

    /// <summary>
    /// Logs when reading from the replay queue directory fails.
    /// </summary>
    [LoggerMessage(EventId = 2704, Level = LogLevel.Error, Message = "Failed to read from replay queue directory {QueueDirectory}")]
    public static partial void ReplayQueueReadFailed(ILogger logger, string queueDirectory, Exception exception);

    /// <summary>
    /// Logs when deleting a replay queue file fails.
    /// </summary>
    [LoggerMessage(EventId = 2705, Level = LogLevel.Error, Message = "Failed to delete replay queue file {QueueFile}")]
    public static partial void ReplayQueueDeleteFailed(ILogger logger, string queueFile, Exception exception);

    /// <summary>
    /// Logs when serializing a replay command fails.
    /// </summary>
    [LoggerMessage(EventId = 2706, Level = LogLevel.Error, Message = "Failed to serialize replay command for delivery {DeliveryId}")]
    public static partial void ReplayQueueSerializationFailed(ILogger logger, string deliveryId, Exception exception);
}
