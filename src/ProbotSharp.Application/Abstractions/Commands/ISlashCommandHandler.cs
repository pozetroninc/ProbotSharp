// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Commands;
using ProbotSharp.Domain.Context;

namespace ProbotSharp.Application.Abstractions.Commands;

/// <summary>
/// Defines a handler for slash commands parsed from GitHub issue and pull request comments.
/// Implementations of this interface are automatically discovered and registered
/// when decorated with <see cref="SlashCommandHandlerAttribute"/>.
/// </summary>
/// <example>
/// <code>
/// [SlashCommandHandler("label")]
/// public class LabelCommandHandler : ISlashCommandHandler
/// {
///     public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
///     {
///         var labels = command.Arguments.Split(',').Select(l => l.Trim()).ToArray();
///         // Add labels to issue...
///     }
/// }
/// </code>
/// </example>
public interface ISlashCommandHandler
{
    /// <summary>
    /// Handles a slash command with the provided context and command information.
    /// </summary>
    /// <param name="context">The Probot context containing event data and GitHub API client.</param>
    /// <param name="command">The parsed slash command containing name, arguments, and metadata.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken cancellationToken = default);
}
