// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace ProbotSharp.Infrastructure.Adapters.Workers;

/// <summary>
/// Structured log messages for dead-letter queue operations.
/// </summary>
internal static partial class DeadLetterLogMessages
{
    /// <summary>
    /// Logs when a dead-letter item is successfully created.
    /// </summary>
    [LoggerMessage(EventId = 2800, Level = LogLevel.Warning, Message = "Moved delivery {DeliveryId} to dead-letter queue with ID {DeadLetterId}. Reason: {Reason}")]
    public static partial void DeadLetterItemCreated(ILogger logger, string deliveryId, string deadLetterId, string reason);

    /// <summary>
    /// Logs when a dead-letter item is successfully requeued.
    /// </summary>
    [LoggerMessage(EventId = 2801, Level = LogLevel.Information, Message = "Requeued dead-letter item {DeadLetterId} for delivery {DeliveryId}")]
    public static partial void DeadLetterItemRequeued(ILogger logger, string deliveryId, string deadLetterId);

    /// <summary>
    /// Logs when a dead-letter item is permanently deleted.
    /// </summary>
    [LoggerMessage(EventId = 2802, Level = LogLevel.Information, Message = "Permanently deleted dead-letter item {DeadLetterId}")]
    public static partial void DeadLetterItemDeleted(ILogger logger, string deadLetterId);

    /// <summary>
    /// Logs when a dead-letter file is empty after deserialization.
    /// </summary>
    [LoggerMessage(EventId = 2803, Level = LogLevel.Warning, Message = "Dead-letter file {FilePath} was empty after deserialization")]
    public static partial void DeadLetterEmptyFile(ILogger logger, string filePath);

    /// <summary>
    /// Logs when writing to the dead-letter queue fails.
    /// </summary>
    [LoggerMessage(EventId = 2804, Level = LogLevel.Error, Message = "Failed to write dead-letter item for delivery {DeliveryId}")]
    public static partial void DeadLetterWriteFailed(ILogger logger, string deliveryId, Exception exception);

    /// <summary>
    /// Logs when reading from the dead-letter queue fails.
    /// </summary>
    [LoggerMessage(EventId = 2805, Level = LogLevel.Error, Message = "Failed to read from dead-letter queue {Path}")]
    public static partial void DeadLetterReadFailed(ILogger logger, string path, Exception exception);

    /// <summary>
    /// Logs when deleting a dead-letter item fails.
    /// </summary>
    [LoggerMessage(EventId = 2806, Level = LogLevel.Error, Message = "Failed to delete dead-letter item {DeadLetterId}")]
    public static partial void DeadLetterDeleteFailed(ILogger logger, string deadLetterId, Exception exception);

    /// <summary>
    /// Logs when serializing a dead-letter item fails.
    /// </summary>
    [LoggerMessage(EventId = 2807, Level = LogLevel.Error, Message = "Failed to serialize dead-letter item for delivery {DeliveryId}")]
    public static partial void DeadLetterSerializationFailed(ILogger logger, string deliveryId, Exception exception);

    /// <summary>
    /// Logs when deserializing a dead-letter item fails.
    /// </summary>
    [LoggerMessage(EventId = 2808, Level = LogLevel.Error, Message = "Failed to deserialize dead-letter item from {FilePath}")]
    public static partial void DeadLetterDeserializationFailed(ILogger logger, string filePath, Exception exception);
}
