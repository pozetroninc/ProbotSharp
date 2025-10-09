// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Commands;
using ProbotSharp.Domain.Context;

namespace ProbotSharp.Application.EventHandlers;

/// <summary>
/// Automatically processes slash commands from GitHub issue and pull request comments.
/// This handler listens for comment creation events and routes any slash commands
/// found in the comment body to registered command handlers.
/// </summary>
[EventHandler("issue_comment", "created")]
[EventHandler("pull_request_review_comment", "created")]
public sealed class SlashCommandEventHandler : IEventHandler
{
    private readonly SlashCommandRouter _router;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SlashCommandEventHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashCommandEventHandler"/> class.
    /// </summary>
    /// <param name="router">The slash command router for dispatching commands.</param>
    /// <param name="serviceProvider">Service provider for resolving command handlers.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public SlashCommandEventHandler(
        SlashCommandRouter router,
        IServiceProvider serviceProvider,
        ILogger<SlashCommandEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);

        this._router = router;
        this._serviceProvider = serviceProvider;
        this._logger = logger;
    }

    /// <summary>
    /// Handles comment creation events by parsing and routing slash commands.
    /// </summary>
    /// <param name="context">The Probot context containing event data and GitHub API client.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        // Don't process comments from bots to avoid infinite loops
        if (context.IsBot())
        {
            this._logger.LogDebug("Comment was created by a bot, skipping slash command processing");
            return;
        }

        // Extract comment body from payload
        var commentBody = context.Payload["comment"]?["body"]?.ToObject<string>();
        if (string.IsNullOrWhiteSpace(commentBody))
        {
            this._logger.LogDebug("Comment body is empty, skipping slash command processing");
            return;
        }

        // Check if comment contains any slash commands
        var commands = SlashCommandParser.Parse(commentBody).ToList();
        if (commands.Count == 0)
        {
            this._logger.LogDebug("No slash commands found in comment");
            return;
        }

        this._logger.LogInformation(
            "Found {CommandCount} slash command(s) in {EventName} event",
            commands.Count,
            context.EventName);

        // Route commands to registered handlers
        await this._router.RouteAsync(context, commentBody, this._serviceProvider, cancellationToken);
    }
}
