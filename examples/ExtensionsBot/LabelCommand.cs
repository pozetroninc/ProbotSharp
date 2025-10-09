// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Octokit;
using ProbotSharp.Application.Abstractions.Commands;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Commands;
using ProbotSharp.Domain.Context;

namespace ExtensionsBot;

/// <summary>
/// Slash command handler that adds labels to issues or pull requests and tracks label changes in metadata.
/// Usage: /label bug, enhancement
/// </summary>
[SlashCommandHandler("label")]
public class LabelCommand : ISlashCommandHandler
{
    private readonly ILogger<LabelCommand> _logger;
    private readonly IMetadataPort _metadataPort;

    public LabelCommand(ILogger<LabelCommand> logger, IMetadataPort metadataPort)
    {
        _logger = logger;
        _metadataPort = metadataPort;
    }

    public async Task HandleAsync(
        ProbotSharpContext context,
        SlashCommand command,
        CancellationToken cancellationToken = default)
    {
        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>();
        if (!issueNumber.HasValue || context.Repository == null)
        {
            _logger.LogWarning("Could not extract issue number or repository from payload");
            return;
        }

        if (string.IsNullOrWhiteSpace(command.Arguments))
        {
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
            _logger.LogWarning(
                "No valid labels parsed from arguments '{Arguments}'",
                command.Arguments);
            return;
        }

        try
        {
            // Create the metadata service with the current context
            var metadata = new MetadataService(_metadataPort, context);

            // Add labels via GitHub API
            await context.GitHub.Issue.Labels.AddToIssue(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                labels);

            // Track label additions in metadata
            var currentCount = await metadata.GetAsync("label_changes", cancellationToken);
            var count = int.TryParse(currentCount, out var c) ? c : 0;
            await metadata.SetAsync("label_changes", (count + 1).ToString(), cancellationToken);

            // Track last labeler
            var sender = context.Payload["sender"]?["login"]?.ToString() ?? "unknown";
            await metadata.SetAsync("last_labeler", sender, cancellationToken);

            _logger.LogInformation(
                "Added labels {Labels} to issue #{IssueNumber} via /label command (total label changes: {Count})",
                string.Join(", ", labels),
                issueNumber.Value,
                count + 1);

            // Post confirmation
            var labelList = string.Join(", ", labels.Select(l => $"`{l}`"));
            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                $"✅ Added labels: {labelList}");
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            _logger.LogError(
                ex,
                "Failed to add labels to issue #{IssueNumber}: Some labels may not exist",
                issueNumber.Value);

            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                "❌ Failed to add labels. Make sure all labels exist in this repository.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
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
