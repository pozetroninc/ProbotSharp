// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Attachments;
using ProbotSharp.Domain.Context;

namespace ExtensionsBot;

/// <summary>
/// Event handler that generates an activity summary when an issue is closed.
/// Demonstrates using metadata storage and comment attachments together.
/// </summary>
[EventHandler("issues", "closed")]
public class SummaryGenerator : IEventHandler
{
    private readonly ILogger<SummaryGenerator> _logger;
    private readonly IMetadataPort _metadataPort;

    public SummaryGenerator(
        ILogger<SummaryGenerator> logger,
        IMetadataPort metadataPort)
    {
        _logger = logger;
        _metadataPort = metadataPort;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct = default)
    {
        // Create services with the current context
        var metadata = new MetadataService(_metadataPort, context);
        var attachments = new CommentAttachmentService(context);

        // Get all tracked metadata
        var allMetadata = await metadata.GetAllAsync(ct);

        if (!allMetadata.Any())
        {
            _logger.LogInformation("No metadata to summarize for closed issue");
            return;
        }

        // Extract common metrics
        var editCount = GetMetricValue(allMetadata, "edit_count", "0");
        var commentCount = GetMetricValue(allMetadata, "comment_count", "0");
        var labelCount = GetMetricValue(allMetadata, "label_count", "0");
        var lastActivity = GetMetricValue(allMetadata, "last_activity", "Unknown");

        // Build attachment fields
        var fields = new List<AttachmentField>
        {
            new() { Title = "Total Edits", Value = editCount, Short = true },
            new() { Title = "Total Comments", Value = commentCount, Short = true },
            new() { Title = "Label Changes", Value = labelCount, Short = true },
            new() { Title = "Last Activity", Value = FormatTimestamp(lastActivity), Short = true }
        };

        // Add custom tracked metrics
        foreach (var kvp in allMetadata.Where(x => !IsSystemMetric(x.Key)))
        {
            fields.Add(new AttachmentField
            {
                Title = FormatKey(kvp.Key),
                Value = kvp.Value,
                Short = true
            });
        }

        // Post summary attachment
        await attachments.AddAsync(new CommentAttachment
        {
            Title = "Issue Summary",
            Text = $"This issue had {allMetadata.Count} tracked metric(s) before being closed.",
            Color = "blue",
            Fields = fields
        }, ct);

        _logger.LogInformation(
            "Posted activity summary with {MetricCount} metrics",
            allMetadata.Count);
    }

    private static string GetMetricValue(IDictionary<string, string> metadata, string key, string defaultValue)
    {
        return metadata.TryGetValue(key, out var value) ? value : defaultValue;
    }

    private static bool IsSystemMetric(string key)
    {
        return key is "edit_count" or "comment_count" or "label_count" or "last_activity" or "label_changes" or "last_labeler";
    }

    private static string FormatKey(string key)
    {
        return string.Join(" ", key.Split('_'))
            .Replace("  ", " ")
            .Trim()
            .ToUpperInvariant();
    }

    private static string FormatTimestamp(string timestamp)
    {
        if (DateTime.TryParse(timestamp, out var dt))
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss UTC");
        }
        return timestamp;
    }
}
