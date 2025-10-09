// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Context;

namespace ExtensionsBot;

/// <summary>
/// Event handler that tracks issue activity using metadata storage.
/// Monitors edits, comments, and label changes.
/// </summary>
[EventHandler("issues", "edited")]
[EventHandler("issue_comment", "created")]
[EventHandler("issues", "labeled")]
public class ActivityTracker : IEventHandler
{
    private readonly ILogger<ActivityTracker> _logger;
    private readonly IMetadataPort _metadataPort;

    public ActivityTracker(ILogger<ActivityTracker> logger, IMetadataPort metadataPort)
    {
        _logger = logger;
        _metadataPort = metadataPort;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct = default)
    {
        // Ignore bot events to prevent infinite loops
        if (context.IsBot())
        {
            return;
        }

        var eventName = context.Payload["action"]?.ToString() ?? "unknown";

        // Increment activity counter based on event type
        var metricKey = eventName switch
        {
            "edited" => "edit_count",
            "created" => "comment_count",
            "labeled" => "label_count",
            _ => null
        };

        if (metricKey == null)
        {
            return;
        }

        // Create the metadata service with the current context
        var metadata = new MetadataService(_metadataPort, context);

        // Get current count and increment
        var currentCount = await metadata.GetAsync(metricKey, ct);
        var count = int.TryParse(currentCount, out var c) ? c : 0;
        await metadata.SetAsync(metricKey, (count + 1).ToString(), ct);

        // Update last activity timestamp
        await metadata.SetAsync("last_activity", DateTime.UtcNow.ToString("o"), ct);

        _logger.LogInformation(
            "Tracked {EventName} activity for issue (total {MetricKey}: {Count})",
            eventName,
            metricKey,
            count + 1);
    }
}
