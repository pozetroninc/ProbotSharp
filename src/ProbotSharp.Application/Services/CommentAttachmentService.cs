// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json.Linq;

using ProbotSharp.Domain.Attachments;
using ProbotSharp.Domain.Context;

namespace ProbotSharp.Application.Services;

/// <summary>
/// Service for adding structured attachments to GitHub issue and PR comments.
/// Attachments are appended to comments without modifying the original text,
/// and are identified by an HTML marker to enable idempotent updates.
/// </summary>
public class CommentAttachmentService
{
    private readonly ProbotSharpContext _context;
    private const string AttachmentMarker = "<!-- probot-sharp-attachments -->";

    /// <summary>
    /// Initializes a new instance of the <see cref="CommentAttachmentService"/> class.
    /// </summary>
    /// <param name="context">The current Probot context.</param>
    public CommentAttachmentService(ProbotSharpContext context)
    {
        this._context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Adds a single attachment to the comment in the current event payload.
    /// </summary>
    /// <param name="attachment">The attachment to add.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the payload does not contain a comment context.</exception>
    public async Task AddAsync(CommentAttachment attachment, CancellationToken ct = default)
    {
        await this.AddAsync(new[] { attachment }, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds multiple attachments to the comment in the current event payload.
    /// If attachments already exist, they will be replaced with the new ones.
    /// </summary>
    /// <param name="attachments">The attachments to add.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the payload does not contain a comment context.</exception>
    public async Task AddAsync(IEnumerable<CommentAttachment> attachments, CancellationToken ct = default)
    {
        // Get comment from payload
        var commentId = GetCommentIdFromPayload(this._context.Payload);
        if (commentId == null)
        {
            throw new InvalidOperationException("Attachments require a comment context");
        }

        if (this._context.Repository == null)
        {
            throw new InvalidOperationException("Repository information is required to add attachments");
        }

        // Fetch current comment body
        var comment = await this._context.GitHub.Issue.Comment.Get(
            this._context.Repository.Owner,
            this._context.Repository.Name,
            commentId.Value).ConfigureAwait(false);

        var currentBody = comment.Body;

        // Check if attachments already exist
        var attachmentSection = AttachmentMarker + "\n" + AttachmentRenderer.RenderAttachments(attachments);

        string newBody;
        if (currentBody.Contains(AttachmentMarker))
        {
            // Replace existing attachments section
            var markerIndex = currentBody.IndexOf(AttachmentMarker);
            newBody = currentBody.Substring(0, markerIndex) + attachmentSection;
        }
        else
        {
            // Append new attachments section
            newBody = currentBody + "\n\n" + attachmentSection;
        }

        // Update comment
        await this._context.GitHub.Issue.Comment.Update(
            this._context.Repository.Owner,
            this._context.Repository.Name,
            commentId.Value,
            newBody).ConfigureAwait(false);
    }

    /// <summary>
    /// Extracts the comment ID from various webhook payloads.
    /// </summary>
    /// <param name="payload">The webhook payload.</param>
    /// <returns>The comment ID if found, otherwise null.</returns>
    private static long? GetCommentIdFromPayload(JObject payload)
    {
        // Try issue comment
        var commentId = payload["comment"]?["id"]?.Value<long>();
        if (commentId != null)
        {
            return commentId;
        }

        // Try review comment
        commentId = payload["review_comment"]?["id"]?.Value<long>();
        if (commentId != null)
        {
            return commentId;
        }

        return null;
    }
}
