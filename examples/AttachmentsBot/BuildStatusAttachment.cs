// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Octokit;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Attachments;
using ProbotSharp.Domain.Context;

namespace AttachmentsBot;

/// <summary>
/// Event handler that demonstrates adding structured attachments to comments.
/// When a user comments with "/build-status", this handler appends a build status card.
/// </summary>
[EventHandler("issue_comment", "created")]
public class BuildStatusAttachment : IEventHandler
{
    /// <summary>
    /// Handles the issue comment created event by adding a build status attachment.
    /// </summary>
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        var payload = context.GetPayload<IssueCommentPayload>();
        var comment = payload.Comment.Body;

        // Check if comment requests build status
        if (comment.Contains("/build-status"))
        {
            // Create the attachment service with the current context
            var attachments = new CommentAttachmentService(context);

            // Create a rich build status attachment
            await attachments.AddAsync(new CommentAttachment
            {
                Title = "Build Status",
                TitleLink = "https://ci.example.com/builds/123",
                Text = "Latest build completed successfully",
                Color = "green",
                Fields = new List<AttachmentField>
                {
                    new() { Title = "Duration", Value = "2m 34s", Short = true },
                    new() { Title = "Tests", Value = "142 passed", Short = true },
                    new() { Title = "Coverage", Value = "87%", Short = true },
                    new() { Title = "Branch", Value = "main", Short = true },
                },
            }, ct);

            context.Logger.LogInformation(
                "Added build status attachment to comment {CommentId}",
                payload.Comment.Id);
        }
    }
}
