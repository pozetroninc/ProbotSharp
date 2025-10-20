// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Commands;

/// <summary>
/// Represents a parsed slash command from a GitHub issue or pull request comment.
/// Slash commands are lines starting with '/' that allow users to interact with Probot apps.
/// </summary>
/// <example>
/// For a comment line "/label bug, enhancement", this would parse to:
/// - Name: "label".
/// - Arguments: "bug, enhancement".
/// - FullText: "/label bug, enhancement".
/// - LineNumber: 1.
/// </example>
public sealed class SlashCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SlashCommand"/> class.
    /// </summary>
    /// <param name="name">The command name (e.g., "label").</param>
    /// <param name="arguments">The command arguments (e.g., "bug, enhancement"), or empty string if no arguments.</param>
    /// <param name="fullText">The full text of the command line.</param>
    /// <param name="lineNumber">The line number in the comment where this command was found (1-indexed).</param>
    public SlashCommand(string name, string arguments, string fullText, int lineNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullText);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lineNumber);

        this.Name = name;
        this.Arguments = arguments;
        this.FullText = fullText;
        this.LineNumber = lineNumber;
    }

    /// <summary>
    /// Gets the command name (e.g., "label", "assign", "help").
    /// Command names are alphanumeric and may contain hyphens or underscores.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets the command arguments as a single string.
    /// This is everything after the first whitespace following the command name.
    /// Empty string if no arguments were provided.
    /// </summary>
    public string Arguments { get; init; }

    /// <summary>
    /// Gets the full text of the command line as it appeared in the comment.
    /// </summary>
    public string FullText { get; init; }

    /// <summary>
    /// Gets the line number in the comment where this command was found (1-indexed).
    /// </summary>
    public int LineNumber { get; init; }
}
