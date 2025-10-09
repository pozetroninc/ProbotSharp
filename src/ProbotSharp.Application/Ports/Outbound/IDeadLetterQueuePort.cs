// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Port for dead-letter queue operations.
/// Stores webhook replay commands that have exhausted all retry attempts or encountered permanent failures.
/// </summary>
public interface IDeadLetterQueuePort
{
    /// <summary>
    /// Moves a failed replay command to the dead-letter queue for manual investigation.
    /// </summary>
    /// <param name="command">The command that failed processing.</param>
    /// <param name="reason">The reason for moving to the dead-letter queue.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> MoveToDeadLetterAsync(EnqueueReplayCommand command, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all items from the dead-letter queue for manual review.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A result containing the list of dead-letter items.</returns>
    Task<Result<IReadOnlyList<DeadLetterItem>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific dead-letter item by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the dead-letter item.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A result containing the dead-letter item if found.</returns>
    Task<Result<DeadLetterItem?>> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a dead-letter item and returns it for manual retry.
    /// </summary>
    /// <param name="id">The unique identifier of the dead-letter item.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A result containing the replay command if found.</returns>
    Task<Result<EnqueueReplayCommand?>> RequeueAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a dead-letter item without retry.
    /// </summary>
    /// <param name="id">The unique identifier of the dead-letter item to delete.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
