// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Octokit;

using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Context;

namespace MetadataBot;

/// <summary>
/// Reports the total edit count when an issue is closed.
/// Demonstrates retrieving and using metadata stored by other event handlers.
/// </summary>
[EventHandler("issues", "closed")]
public class EditCountReporter : IEventHandler
{
    private readonly IMetadataPort _metadataPort;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditCountReporter"/> class.
    /// </summary>
    /// <param name="metadataPort">The metadata port for retrieving edit counts.</param>
    public EditCountReporter(IMetadataPort metadataPort)
    {
        _metadataPort = metadataPort ?? throw new ArgumentNullException(nameof(metadataPort));
    }

    /// <summary>
    /// Handles issue closed events by posting a summary comment with the total edit count.
    /// </summary>
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct = default)
    {
        // Don't respond to bot actions
        if (context.IsBot())
        {
            context.Logger.LogDebug("Ignoring close event from bot");
            return;
        }

        try
        {
            // Create the metadata service with the current context
            var metadata = new MetadataService(_metadataPort, context);

            // Retrieve the edit count from metadata
            var editCount = await metadata.GetAsync("edit_count", ct);

            // Only post a comment if there were edits
            if (!string.IsNullOrEmpty(editCount) && int.TryParse(editCount, out var count) && count > 0)
            {
                var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>();
                if (!issueNumber.HasValue || context.Repository == null)
                {
                    context.Logger.LogWarning("Could not extract issue number from payload");
                    return;
                }

                var comment = $"This issue had **{count}** edit(s) before it was closed.";

                await context.GitHub.Issue.Comment.Create(
                    context.Repository.Owner,
                    context.Repository.Name,
                    issueNumber.Value,
                    comment);

                context.Logger.LogInformation(
                    "Posted edit count summary on issue #{IssueNumber}: {Count} edits",
                    issueNumber.Value,
                    count);
            }
            else
            {
                context.Logger.LogDebug("No edits tracked for this issue, skipping summary comment");
            }
        }
        catch (InvalidOperationException ex)
        {
            // This can happen if the event doesn't have issue context
            context.Logger.LogWarning(
                ex,
                "Could not report edit count: {Message}",
                ex.Message);
        }
        catch (Exception ex)
        {
            context.Logger.LogError(
                ex,
                "Failed to post edit count summary: {Message}",
                ex.Message);
            throw;
        }
    }
}
