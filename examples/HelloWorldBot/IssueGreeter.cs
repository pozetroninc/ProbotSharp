// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Octokit;

using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Context;

namespace HelloWorldBot;

/// <summary>
/// Event handler that greets users when they open a new issue.
/// Demonstrates basic event handling and GitHub API interaction.
/// </summary>
[EventHandler("issues", "opened")]
public class IssueGreeter : IEventHandler
{
    /// <summary>
    /// Handles the issue opened event by posting a greeting comment.
    /// </summary>
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        // Log that we received the event
        context.Logger.LogInformation(
            "Received issue opened event for {Repository}",
            context.GetRepositoryFullName());

        // Don't respond to bot-created issues to avoid loops
        if (context.IsBot())
        {
            context.Logger.LogDebug("Issue was created by a bot, skipping greeting");
            return;
        }

        // Extract issue parameters using context helper
        var (owner, repo, issueNumber) = context.Issue();

        // Get the issue author's name
        var authorLogin = context.Payload["issue"]?["user"]?["login"]?.ToObject<string>() ?? "there";

        try
        {
            // Post a greeting comment using the GitHub API
            var comment = $"Hello @{authorLogin}! 👋\n\n" +
                         $"Thanks for opening this issue. This is an automated greeting from HelloWorldBot, " +
                         $"a simple ProbotSharp example.\n\n" +
                         $"Someone will take a look at your issue soon!";

            await context.GitHub.Issue.Comment.Create(
                owner,
                repo,
                issueNumber,
                comment);

            context.Logger.LogInformation(
                "Successfully posted greeting comment on issue #{IssueNumber}",
                issueNumber);
        }
        catch (Exception ex)
        {
            context.Logger.LogError(
                ex,
                "Failed to post greeting comment on issue #{IssueNumber}: {ErrorMessage}",
                issueNumber,
                ex.Message);
            throw;
        }
    }
}
