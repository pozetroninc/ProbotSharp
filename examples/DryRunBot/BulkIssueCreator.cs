// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Octokit;
using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Context;

namespace DryRunBot;

/// <summary>
/// Example handler that demonstrates manual dry-run checks for bulk issue creation.
/// This pattern gives you full control over the dry-run logic.
/// </summary>
public class BulkIssueCreator : IEventHandler
{
    /// <summary>
    /// Handles repository.created events by creating a set of template issues.
    /// In dry-run mode, this will only log what it would do without actually creating issues.
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

        // Define the template issues we want to create
        var templateIssues = new[]
        {
            new { Title = "Setup CI/CD Pipeline", Body = "Configure continuous integration and deployment." },
            new { Title = "Add Contributing Guidelines", Body = "Create CONTRIBUTING.md with guidelines for contributors." },
            new { Title = "Setup Code Quality Tools", Body = "Configure linters, formatters, and static analysis tools." },
            new { Title = "Create Documentation", Body = "Add comprehensive README and API documentation." },
            new { Title = "Setup Issue Templates", Body = "Add templates for bug reports and feature requests." },
        };

        context.Logger.LogInformation("Processing bulk issue creation for {Owner}/{Repo}", owner, repo);

        // Pattern 1: Manual if/else check using context.IsDryRun
        if (context.IsDryRun)
        {
            context.Logger.LogInformation("[DRY-RUN] Would create {Count} template issues:", templateIssues.Length);
            foreach (var issue in templateIssues)
            {
                context.Logger.LogInformation("[DRY-RUN]   - {Title}: {Body}", issue.Title, issue.Body);
            }
            context.Logger.LogInformation("[DRY-RUN] No actual issues created in dry-run mode");
        }
        else
        {
            // Actually create the issues
            context.Logger.LogInformation("Creating {Count} template issues...", templateIssues.Length);
            foreach (var issue in templateIssues)
            {
                try
                {
                    var newIssue = new NewIssue(issue.Title)
                    {
                        Body = issue.Body,
                    };

                    var created = await context.GitHub.Issue.Create(owner, repo, newIssue);
                    context.Logger.LogInformation("Created issue #{Number}: {Title}", created.Number, created.Title);
                }
                catch (Exception ex)
                {
                    context.Logger.LogError(ex, "Failed to create issue: {Title}", issue.Title);
                }
            }
            context.Logger.LogInformation("Finished creating template issues");
        }
    }
}
