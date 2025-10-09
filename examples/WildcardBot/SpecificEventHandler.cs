// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Context;

namespace WildcardBot;

/// <summary>
/// Handles a specific event (issues.opened) to demonstrate how
/// multiple handlers execute for the same event.
/// When an issue is opened:
/// 1. AllEventsLogger logs it (wildcard *)
/// 2. AllIssueEventsHandler processes it (issues.*)
/// 3. SpecificEventHandler posts a greeting (issues.opened)
/// 4. MetricsCollector records metrics (wildcard *)
/// </summary>
[EventHandler("issues", "opened")]
public class SpecificEventHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        if (context.IsBot())
        {
            context.Logger.LogDebug("[SpecificEventHandler] Skipping bot-created issue");
            return;
        }

        var (owner, repo, issueNumber) = context.Issue();
        var authorLogin = context.Payload["issue"]?["user"]?["login"]?.ToObject<string>() ?? "there";
        var issueTitle = context.Payload["issue"]?["title"]?.ToString() ?? "Unknown";

        context.Logger.LogInformation(
            "[SpecificEventHandler] New issue #{IssueNumber}: \"{Title}\" by @{Author}",
            issueNumber,
            issueTitle,
            authorLogin);

        try
        {
            // Post a greeting comment
            var greeting = $"Hello @{authorLogin}! 👋\n\n" +
                          $"Thanks for opening this issue. This is **SpecificEventHandler** responding to `issues.opened`.\n\n" +
                          $"Note that multiple handlers can process the same event:\n" +
                          $"- `AllEventsLogger` logged this event (wildcard `*`)\n" +
                          $"- `AllIssueEventsHandler` processed it (event wildcard `issues.*`)\n" +
                          $"- `SpecificEventHandler` (this handler) posted this comment (`issues.opened`)\n" +
                          $"- `MetricsCollector` recorded metrics (wildcard `*`)\n\n" +
                          $"This demonstrates ProbotSharp's flexible event routing!";

            await context.GitHub.Issue.Comment.Create(owner, repo, issueNumber, greeting);

            context.Logger.LogInformation(
                "[SpecificEventHandler] Posted greeting comment on issue #{IssueNumber}",
                issueNumber);
        }
        catch (Exception ex)
        {
            context.Logger.LogError(
                ex,
                "[SpecificEventHandler] Failed to post greeting comment on issue #{IssueNumber}",
                issueNumber);
            throw;
        }
    }
}
