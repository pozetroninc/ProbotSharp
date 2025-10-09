// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using ProbotSharp.Application.Abstractions.Commands;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Commands;
using ProbotSharp.Domain.Context;

namespace ExtensionsBot;

/// <summary>
/// Slash command handler that tracks custom metrics using metadata storage.
/// Usage: /track <metric> <value>
/// Examples:
///   /track progress 50%
///   /track reviewers alice,bob
///   /track priority high
/// </summary>
[SlashCommandHandler("track")]
public class TrackCommand : ISlashCommandHandler
{
    private readonly ILogger<TrackCommand> _logger;
    private readonly IMetadataPort _metadataPort;

    public TrackCommand(ILogger<TrackCommand> logger, IMetadataPort metadataPort)
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
                "❌ Usage: `/track <metric> <value>`\n\nExamples:\n- `/track progress 50%`\n- `/track priority high`");
            return;
        }

        // Parse arguments: first word is metric name, rest is value
        var parts = command.Arguments.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
        {
            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                "❌ Both metric name and value are required.\n\nUsage: `/track <metric> <value>`");
            return;
        }

        var metricName = parts[0].ToLowerInvariant();
        var metricValue = parts[1];

        try
        {
            // Create the metadata service with the current context
            var metadata = new MetadataService(_metadataPort, context);

            // Store metric in metadata
            await metadata.SetAsync(metricName, metricValue, cancellationToken);

            _logger.LogInformation(
                "Tracked metric '{Metric}' = '{Value}' for issue #{IssueNumber}",
                metricName,
                metricValue,
                issueNumber.Value);

            // Post confirmation
            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                $"✅ Tracked **{metricName}**: `{metricValue}`\n\nUse `/status` to see all tracked metrics.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to track metric '{Metric}' for issue #{IssueNumber}: {ErrorMessage}",
                metricName,
                issueNumber.Value,
                ex.Message);

            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                "❌ An error occurred while tracking the metric. Please try again.");
        }
    }
}
