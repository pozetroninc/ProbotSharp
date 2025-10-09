// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Represents a webhook replay command that has failed processing and moved to the dead-letter queue.
/// </summary>
/// <param name="Id">Unique identifier for the dead-letter item.</param>
/// <param name="Command">The original replay command that failed.</param>
/// <param name="Reason">The reason for failure (error message or description).</param>
/// <param name="FailedAt">Timestamp when the item was moved to the dead-letter queue.</param>
/// <param name="LastError">The last error encountered during processing.</param>
public sealed record DeadLetterItem(
    string Id,
    EnqueueReplayCommand Command,
    string Reason,
    DateTimeOffset FailedAt,
    string? LastError = null);
