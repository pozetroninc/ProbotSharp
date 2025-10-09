// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Text.RegularExpressions;

namespace ProbotSharp.Domain.Commands;

/// <summary>
/// Parses slash commands from GitHub issue and pull request comment bodies.
/// Slash commands are lines that start with '/' followed by a command name and optional arguments.
/// </summary>
/// <example>
/// <code>
/// var commentBody = @"
/// /label bug, enhancement
/// Some regular comment text
/// /assign @johndoe
/// ";
///
/// var commands = SlashCommandParser.Parse(commentBody);
/// // Returns 2 commands: "label" and "assign"
/// </code>
/// </example>
public static class SlashCommandParser
{
    // Regex pattern: /command-name arguments
    // - Matches lines starting with '/' (with optional leading whitespace)
    // - Command name: alphanumeric, hyphens, underscores
    // - Arguments: everything after first whitespace (optional)
    private static readonly Regex CommandPattern = new(
        @"^\s*/(?<command>[a-zA-Z0-9_-]+)(?:\s+(?<args>.*))?$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Parses all slash commands from a comment body.
    /// </summary>
    /// <param name="commentBody">The full text of the comment.</param>
    /// <returns>An enumerable of parsed slash commands, in the order they appear.</returns>
    public static IEnumerable<SlashCommand> Parse(string commentBody)
    {
        if (string.IsNullOrWhiteSpace(commentBody))
        {
            yield break;
        }

        var lines = commentBody.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
        {
            var command = ParseLine(lines[i], i + 1); // Line numbers are 1-indexed
            if (command != null)
            {
                yield return command;
            }
        }
    }

    /// <summary>
    /// Checks if a given line is a slash command (starts with '/' after optional whitespace).
    /// </summary>
    /// <param name="line">The line to check.</param>
    /// <returns>True if the line is a slash command, false otherwise.</returns>
    public static bool IsSlashCommand(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        return CommandPattern.IsMatch(line);
    }

    /// <summary>
    /// Parses a single line into a slash command, if it matches the pattern.
    /// </summary>
    /// <param name="line">The line to parse.</param>
    /// <param name="lineNumber">The 1-indexed line number in the comment.</param>
    /// <returns>A <see cref="SlashCommand"/> if the line matches, null otherwise.</returns>
    private static SlashCommand? ParseLine(string line, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var match = CommandPattern.Match(line);
        if (!match.Success)
        {
            return null;
        }

        var commandName = match.Groups["command"].Value;
        var arguments = match.Groups["args"].Success ? match.Groups["args"].Value.Trim() : string.Empty;

        return new SlashCommand(
            name: commandName,
            arguments: arguments,
            fullText: line.Trim(),
            lineNumber: lineNumber);
    }
}
