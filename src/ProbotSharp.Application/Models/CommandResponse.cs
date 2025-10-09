// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Generic response for CLI command execution results.
/// Provides status, output, and error information for any CLI command.
/// </summary>
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
    public CommandResponse() : this(false, 0)
    {
    }
};
