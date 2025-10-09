// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Octokit;
using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Context;

namespace DryRunBot;

/// <summary>
/// Example handler that demonstrates using the ExecuteOrLogAsync extension method
/// for cleaner dry-run logic with automatic logging.
/// </summary>
public class BulkCommentProcessor : IEventHandler
{
    /// <summary>
    /// Handles issues.labeled events by adding comments to related issues.
    /// Uses the ExecuteOrLogAsync helper for cleaner dry-run handling.
    /// </summary>
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        if (context.Repository == null)
        {
            context.Logger.LogWarning("No repository information available");
            return;
        }

        var owner = context.Repository.Owner;
        var repo = context.Repository.Name;

        // Get the label that was added
        var payload = context.GetPayload<dynamic>();
        string labelName = payload.label?.name;

        if (string.IsNullOrEmpty(labelName))
        {
            return;
        }

        // If "needs-review" label was added, notify reviewers on all open PRs
        if (labelName.Equals("needs-review", StringComparison.OrdinalIgnoreCase))
        {
            context.Logger.LogInformation("Processing 'needs-review' label for bulk notification");

            // Get all open pull requests
            var pullRequests = await context.GitHub.PullRequest.GetAllForRepository(owner, repo,
                new PullRequestRequest { State = ItemStateFilter.Open });

            context.Logger.LogInformation("Found {Count} open pull requests to process", pullRequests.Count);

            // Pattern 2: Using ExecuteOrLogAsync extension method
            // This automatically logs in dry-run mode or executes in normal mode
            foreach (var pr in pullRequests)
            {
                var commentBody = $"@{pr.User.Login} This PR has been marked as needing review. Please ensure all checks pass and request reviews from the team.";

                await context.ExecuteOrLogAsync(
                    actionDescription: $"Add review reminder comment to PR #{pr.Number}",
                    action: async () =>
                    {
                        await context.GitHub.Issue.Comment.Create(owner, repo, pr.Number, commentBody);
                        context.Logger.LogInformation("Added review reminder to PR #{Number}", pr.Number);
                    },
                    parameters: new
                    {
                        owner,
                        repo,
                        prNumber = pr.Number,
                        comment = commentBody,
                    });
            }

            context.Logger.LogInformation("Finished processing review notifications");
        }

        // If "high-priority" label was added, update all related issues
        if (labelName.Equals("high-priority", StringComparison.OrdinalIgnoreCase))
        {
            context.Logger.LogInformation("Processing 'high-priority' label for bulk updates");

            // Get all open issues with the same assignee
            int? issueNumber = (int?)payload.issue?.number;
            string? assignee = payload.issue?.assignee?.login?.ToString();

            if (issueNumber != null && !string.IsNullOrEmpty(assignee))
            {
                var allIssues = await context.GitHub.Issue.GetAllForRepository(owner, repo,
                    new RepositoryIssueRequest
                    {
                        State = ItemStateFilter.Open,
                        Assignee = assignee,
                    });

                int issueCount = allIssues.Count;
                context.Logger.LogInformation("Found {Count} issues assigned to {Assignee}", issueCount, assignee);

                foreach (var issue in allIssues)
                {
                    if (issue.Number == issueNumber)
                    {
                        continue; // Skip the current issue
                    }

                    var commentBody = $"Related issue #{issueNumber} was marked as high-priority. Please prioritize your work accordingly.";

                    await context.ExecuteOrLogAsync(
                        actionDescription: $"Add priority notification to issue #{issue.Number}",
                        action: async () =>
                        {
                            await context.GitHub.Issue.Comment.Create(owner, repo, issue.Number, commentBody);
                            context.Logger.LogInformation("Added priority notification to issue #{Number}", issue.Number);
                        },
                        parameters: new
                        {
                            owner,
                            repo,
                            issueNumber = issue.Number,
                            comment = commentBody,
                        });
                }

                context.Logger.LogInformation("Finished processing priority notifications");
            }
        }
    }
}
