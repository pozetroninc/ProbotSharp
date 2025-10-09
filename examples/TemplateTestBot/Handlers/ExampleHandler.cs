// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Octokit;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Context;

namespace TemplateTestBot.Handlers;

/// <summary>
/// Example event handler that responds to issue opened events.
/// This demonstrates how to handle GitHub webhook events and interact with the GitHub API.
/// </summary>
[EventHandler("issues", "opened")]
public class ExampleHandler : IEventHandler
{
    /// <summary>
    /// Handles the issue opened event.
    /// </summary>
    /// <param name="context">The webhook context containing the payload and GitHub client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        // Log that we received the event
        context.Logger.LogInformation(
            "Received issue opened event for repository: {Repository}",
            context.GetRepositoryFullName());

        // Don't respond to issues created by bots to avoid infinite loops
        if (context.IsBot())
        {
            context.Logger.LogDebug("Issue was created by a bot, skipping response");
            return;
        }

        // Extract information from the webhook payload
        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>();
        var issueTitle = context.Payload["issue"]?["title"]?.ToObject<string>();
        var authorLogin = context.Payload["issue"]?["user"]?["login"]?.ToObject<string>();

        // Validate required fields
        if (!issueNumber.HasValue || context.Repository == null)
        {
            context.Logger.LogWarning("Could not extract issue number or repository from payload");
            return;
        }

        context.Logger.LogInformation(
            "Processing issue #{IssueNumber}: {Title} by @{Author}",
            issueNumber.Value,
            issueTitle ?? "(no title)",
            authorLogin ?? "unknown");

        try
        {
            // Example: Post a comment on the issue using the GitHub API
            // The context.GitHub client is already authenticated for this installation
            var comment = $"Hello @{authorLogin ?? "there"}! 👋\n\n" +
                         $"Thank you for opening this issue. This is an automated response from **TemplateTestBot**.\n\n" +
                         $"Bot created from template for testing\n\n" +
                         $"Someone will review your issue soon!";

            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                comment);

            context.Logger.LogInformation(
                "Successfully posted comment on issue #{IssueNumber}",
                issueNumber.Value);

            // Example: Add a label to the issue
            // await context.GitHub.Issue.Labels.AddToIssue(
            //     context.Repository.Owner,
            //     context.Repository.Name,
            //     issueNumber.Value,
            //     new[] { "bot-processed" });
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            // Handle permission errors gracefully
            context.Logger.LogWarning(
                "Bot doesn't have permission to comment on issue #{IssueNumber}. " +
                "Make sure the GitHub App has 'issues:write' permission.",
                issueNumber.Value);
        }
        catch (Exception ex)
        {
            context.Logger.LogError(
                ex,
                "Failed to process issue #{IssueNumber}: {ErrorMessage}",
                issueNumber.Value,
                ex.Message);
            throw;
        }
    }
}
