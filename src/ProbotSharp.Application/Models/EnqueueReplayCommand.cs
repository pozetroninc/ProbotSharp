// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Command for enqueuing a webhook replay.
/// </summary>
public sealed record class EnqueueReplayCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnqueueReplayCommand"/> class.
    /// </summary>
    /// <param name="command">The webhook processing command to replay.</param>
    /// <param name="attempt">The attempt number (defaults to 0).</param>
    public EnqueueReplayCommand(ProcessWebhookCommand command, int attempt = 0)
    {
        this.Command = command ?? throw new ArgumentNullException(nameof(command));
        if (attempt < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt), "Attempt count cannot be negative.");
        }

        this.Attempt = attempt;
    }

    /// <summary>
    /// Gets the webhook processing command.
    /// </summary>
    public ProcessWebhookCommand Command { get; }

    /// <summary>
    /// Gets the attempt number.
    /// </summary>
    public int Attempt { get; }

    /// <summary>
    /// Creates a new command for the next retry attempt.
    /// </summary>
    /// <returns>A new command with incremented attempt count.</returns>
    public EnqueueReplayCommand NextAttempt() => new(this.Command, this.Attempt + 1);
}
