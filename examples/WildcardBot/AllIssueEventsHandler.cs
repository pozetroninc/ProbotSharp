// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Context;

namespace WildcardBot;

/// <summary>
/// Handles all issue events using event wildcard pattern.
/// Matches: issues.opened, issues.closed, issues.edited, issues.labeled, etc.
/// </summary>
[EventHandler("issues", "*")]
public class AllIssueEventsHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        // This handler receives all issue events (any action)
        var (owner, repo, issueNumber) = context.Issue();

        context.Logger.LogInformation(
            "[AllIssueEventsHandler] Issue #{IssueNumber} {Action} in {Owner}/{Repo}",
            issueNumber,
            context.EventAction,
            owner,
            repo);

        // Example: Track issue state transitions
        var issueTitle = context.Payload["issue"]?["title"]?.ToString() ?? "Unknown";
        var issueState = context.Payload["issue"]?["state"]?.ToString() ?? "Unknown";

        context.Logger.LogInformation(
            "[AllIssueEventsHandler] Issue: \"{Title}\" | State: {State}",
            issueTitle,
            issueState);

        // You could implement logic here that applies to all issue events:
        // - Update analytics dashboard
        // - Sync with external issue tracker
        // - Send notifications
        // - Update project metrics

        await Task.CompletedTask;
    }
}
