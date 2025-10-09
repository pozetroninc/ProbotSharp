// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Octokit;
using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Context;

namespace DryRunBot;

/// <summary>
/// Example handler that demonstrates using ThrowIfNotDryRun for dangerous operations
/// that should only be tested, never executed automatically.
/// </summary>
public class LabelManager : IEventHandler
{
    /// <summary>
    /// Handles push events by performing dangerous bulk label operations.
    /// This handler REQUIRES dry-run mode and will throw if not enabled.
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

        context.Logger.LogInformation("Starting dangerous bulk label operations");

        // Pattern 3: Using ThrowIfNotDryRun for operations that are too dangerous
        // This ensures the operation can only be logged, never executed
        try
        {
            context.ThrowIfNotDryRun("Bulk label deletion is a dangerous operation and must be run in dry-run mode");
        }
        catch (InvalidOperationException ex)
        {
            context.Logger.LogError(ex, "Attempted to run dangerous operation without dry-run mode enabled");
            throw;
        }

        // Get all labels in the repository
        var allLabels = await context.GitHub.Issue.Labels.GetAllForRepository(owner, repo);
        context.Logger.LogInformation("Found {Count} labels in repository", allLabels.Count);

        // Example dangerous operation: Delete all labels that haven't been used in 30 days
        var unusedLabels = new List<Label>();

        foreach (var label in allLabels)
        {
            // In a real scenario, you'd check when the label was last used
            // For this example, we'll simulate finding unused labels
            if (label.Name.StartsWith("old-", StringComparison.OrdinalIgnoreCase))
            {
                unusedLabels.Add(label);
            }
        }

        context.Logger.LogInformation("Found {Count} unused labels to delete", unusedLabels.Count);

        // Use LogDryRun for detailed logging of what would be deleted
        context.LogDryRun("Delete unused labels", new
        {
            owner,
            repo,
            labelsToDelete = unusedLabels.Select(l => new { l.Name, l.Color, l.Description }).ToList(),
        });

        // Example dangerous operation: Bulk label rename
        var labelsToRename = allLabels
            .Where(l => l.Name.Contains("bug", StringComparison.OrdinalIgnoreCase))
            .ToList();

        context.Logger.LogInformation("Found {Count} labels to rename", labelsToRename.Count);

        foreach (var label in labelsToRename)
        {
            var newName = label.Name.Replace("bug", "defect", StringComparison.OrdinalIgnoreCase);

            context.LogDryRun($"Rename label '{label.Name}' to '{newName}'", new
            {
                owner,
                repo,
                oldName = label.Name,
                newName,
                color = label.Color,
                description = label.Description,
            });
        }

        // Example dangerous operation: Bulk apply label to all open issues
        var allOpenIssues = await context.GitHub.Issue.GetAllForRepository(owner, repo,
            new RepositoryIssueRequest
            {
                State = ItemStateFilter.Open,
                Filter = IssueFilter.All,
            });

        context.Logger.LogInformation("Found {Count} open issues", allOpenIssues.Count);

        var labelToApply = "needs-triage";
        context.LogDryRun($"Apply '{labelToApply}' label to all open issues", new
        {
            owner,
            repo,
            label = labelToApply,
            issueCount = allOpenIssues.Count,
            issueNumbers = allOpenIssues.Select(i => i.Number).ToList(),
        });

        context.Logger.LogInformation("[DRY-RUN] All dangerous operations logged successfully. " +
                                     "Review the logs carefully before considering execution.");
    }
}
