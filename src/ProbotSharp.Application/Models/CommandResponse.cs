// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Generic response for CLI command execution results.
/// Provides status, output, and error information for any CLI command.
/// </summary>
/// <param name="IsSuccessful">Indicates whether the command executed successfully.</param>
/// <param name="ExitCode">The command exit code.</param>
/// <param name="Output">The standard output from the command.</param>
/// <param name="ErrorOutput">The error output from the command.</param>
/// <param name="CommandName">The name of the executed command.</param>
/// <param name="ExecutedAt">The timestamp when the command was executed.</param>
/// <param name="Duration">The execution duration.</param>
/// <param name="Metadata">Additional metadata about the command execution.</param>
public sealed record class CommandResponse(
    bool IsSuccessful,
    int ExitCode,
    string? Output = null,
    string? ErrorOutput = null,
    string? CommandName = null,
    DateTimeOffset ExecutedAt = default,
    TimeSpan? Duration = null,
    Dictionary<string, object>? Metadata = null)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandResponse"/> class.
    /// </summary>
    public CommandResponse() : this(false, 0)
    {
    }
};
