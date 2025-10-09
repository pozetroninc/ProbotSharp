// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

public sealed record class EnqueueReplayCommand
{
    public EnqueueReplayCommand(ProcessWebhookCommand command, int attempt = 0)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        if (attempt < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt), "Attempt count cannot be negative.");
        }

        Attempt = attempt;
    }

    public ProcessWebhookCommand Command { get; }

    public int Attempt { get; }

    public EnqueueReplayCommand NextAttempt() => new(Command, Attempt + 1);
}

