// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Octokit;
using ProbotSharp.Application.Abstractions.Commands;
using ProbotSharp.Domain.Commands;
using ProbotSharp.Domain.Context;

namespace SlashCommandsBot;

/// <summary>
/// Slash command handler that adds labels to issues or pull requests.
/// Usage: /label bug, enhancement
/// </summary>
[SlashCommandHandler("label")]
public class LabelCommand : ISlashCommandHandler
{
    /// <summary>
    /// Handles the /label command by parsing labels from arguments and adding them to the issue/PR.
    /// </summary>
    /// <param name="context">The ProbotSharp context containing event data and GitHub API client.</param>
    /// <param name="command">The parsed slash command containing the label names.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken cancellationToken = default)
    {
        // Extract issue/PR number from payload
        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>();
        if (!issueNumber.HasValue || context.Repository == null)
        {
            context.Logger.LogWarning(
                "Could not extract issue number or repository from payload for /label command");
            return;
        }

        // Parse labels from arguments (comma-separated)
        if (string.IsNullOrWhiteSpace(command.Arguments))
        {
            context.Logger.LogWarning(
                "No labels provided for /label command on issue #{IssueNumber}",
                issueNumber.Value);

            // Post a comment explaining usage
            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                "❌ No labels provided. Usage: `/label label1, label2, ...`");
            return;
        }

        var labels = command.Arguments
            .Split(',')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        if (labels.Length == 0)
        {
            context.Logger.LogWarning(
                "No valid labels parsed from arguments '{Arguments}' for issue #{IssueNumber}",
                command.Arguments,
                issueNumber.Value);
            return;
        }

        try
        {
            // Add labels to the issue/PR
            var issueUpdate = new IssueUpdate();
            foreach (var label in labels)
            {
                issueUpdate.AddLabel(label);
            }

            await context.GitHub.Issue.Labels.AddToIssue(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                labels);

            context.Logger.LogInformation(
                "Successfully added labels {Labels} to issue #{IssueNumber} via /label command",
                string.Join(", ", labels),
                issueNumber.Value);

            // Post a confirmation comment
            var labelList = string.Join(", ", labels.Select(l => $"`{l}`"));
            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                $"✅ Added labels: {labelList}");
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            // Handle case where labels don't exist
            context.Logger.LogError(
                ex,
                "Failed to add labels to issue #{IssueNumber}: Some labels may not exist in the repository",
                issueNumber.Value);

            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                "❌ Failed to add labels. Make sure all labels exist in this repository.");
        }
        catch (Exception ex)
        {
            context.Logger.LogError(
                ex,
                "Failed to add labels to issue #{IssueNumber}: {ErrorMessage}",
                issueNumber.Value,
                ex.Message);

            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                "❌ An error occurred while adding labels. Please try again.");
        }
    }
}
