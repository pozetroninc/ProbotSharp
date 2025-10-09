// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using ProbotSharp.Application.Abstractions.Commands;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Attachments;
using ProbotSharp.Domain.Commands;
using ProbotSharp.Domain.Context;

namespace ExtensionsBot;

/// <summary>
/// Slash command handler that displays issue status using metadata and attachments.
/// Demonstrates integration of all three extensions working together.
/// Usage: /status
/// </summary>
[SlashCommandHandler("status")]
public class StatusCommand : ISlashCommandHandler
{
    private readonly ILogger<StatusCommand> _logger;
    private readonly IMetadataPort _metadataPort;

    public StatusCommand(
        ILogger<StatusCommand> logger,
        IMetadataPort metadataPort)
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

        // Create services with the current context
        var metadata = new MetadataService(_metadataPort, context);
        var attachments = new CommentAttachmentService(context);

        // Retrieve all metadata for this issue
        var allMetadata = await metadata.GetAllAsync(cancellationToken);

        // Build attachment fields from metadata
        var fields = new List<AttachmentField>();

        if (allMetadata.Any())
        {
            foreach (var kvp in allMetadata.OrderBy(x => x.Key))
            {
                fields.Add(new AttachmentField
                {
                    Title = FormatKey(kvp.Key),
                    Value = kvp.Value,
                    Short = true
                });
            }
        }
        else
        {
            fields.Add(new AttachmentField
            {
                Title = "Status",
                Value = "No metadata tracked yet",
                Short = false
            });
        }

        // Create rich status attachment
        await attachments.AddAsync(new CommentAttachment
        {
            Title = $"Issue #{issueNumber} Status",
            Text = allMetadata.Any()
                ? $"Currently tracking {allMetadata.Count} metric(s)"
                : "No metrics tracked yet. Use `/track <metric> <value>` to start tracking.",
            Color = "blue",
            Fields = fields
        }, cancellationToken);

        _logger.LogInformation(
            "Displayed status with {MetadataCount} metadata entries on issue #{IssueNumber}",
            allMetadata.Count,
            issueNumber.Value);
    }

    /// <summary>
    /// Formats a metadata key for display (e.g., "edit_count" -> "Edit Count").
    /// </summary>
    private static string FormatKey(string key)
    {
        return string.Join(" ", key.Split('_'))
            .Replace("  ", " ")
            .Trim()
            .ToUpperInvariant();
    }
}
