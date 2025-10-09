// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Abstractions.Commands;

/// <summary>
/// Attribute to decorate slash command handler classes with command name filters.
/// Multiple attributes can be applied to a single handler class to respond to multiple commands.
/// </summary>
/// <example>
/// <code>
/// [SlashCommandHandler("label")]
/// [SlashCommandHandler("tag")]
/// public class LabelCommandHandler : ISlashCommandHandler
/// {
///     public Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
///     {
///         // Handle both "label" and "tag" commands
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class SlashCommandHandlerAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SlashCommandHandlerAttribute"/> class.
    /// </summary>
    /// <param name="commandName">The name of the slash command to handle (e.g., "label", "assign").</param>
    public SlashCommandHandlerAttribute(string commandName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);
        this.CommandName = commandName;
    }

    /// <summary>
    /// Gets the name of the slash command to handle.
    /// Command names are case-sensitive and should match exactly as they appear in comments.
    /// </summary>
    public string CommandName { get; }
}
