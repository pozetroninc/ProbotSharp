// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Context;

namespace MetadataBot;

/// <summary>
/// Tracks the number of edits made to issues and comments using metadata storage.
/// Demonstrates how to use MetadataService to persist state across webhook events.
/// </summary>
[EventHandler("issues", "edited")]
[EventHandler("issue_comment", "edited")]
public class EditCountTracker : IEventHandler
{
    private readonly IMetadataPort _metadataPort;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditCountTracker"/> class.
    /// </summary>
    /// <param name="metadataPort">The metadata port for storing edit counts.</param>
    public EditCountTracker(IMetadataPort metadataPort)
    {
        _metadataPort = metadataPort ?? throw new ArgumentNullException(nameof(metadataPort));
    }

    /// <summary>
    /// Handles edit events by incrementing the edit count stored in metadata.
    /// </summary>
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct = default)
    {
        // Don't track edits by bots
        if (context.IsBot())
        {
            context.Logger.LogDebug("Ignoring edit from bot");
            return;
        }

        try
        {
            // Create the metadata service with the current context
            var metadata = new MetadataService(_metadataPort, context);

            // Get the current edit count (or 0 if not set)
            var currentCountString = await metadata.GetAsync("edit_count", ct);
            var currentCount = int.TryParse(currentCountString, out var count) ? count : 0;

            // Increment the count
            var newCount = currentCount + 1;
            await metadata.SetAsync("edit_count", newCount.ToString(), ct);

            context.Logger.LogInformation(
                "Edit count for issue in {Repository}: {Count}",
                context.GetRepositoryFullName(),
                newCount);
        }
        catch (InvalidOperationException ex)
        {
            // This can happen if the event doesn't have issue context
            context.Logger.LogWarning(
                ex,
                "Could not track edit: {Message}",
                ex.Message);
        }
        catch (Exception ex)
        {
            context.Logger.LogError(
                ex,
                "Failed to update edit count: {Message}",
                ex.Message);
            throw;
        }
    }
}
