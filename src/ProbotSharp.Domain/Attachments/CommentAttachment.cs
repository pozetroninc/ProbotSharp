// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ProbotSharp.Domain.Attachments;

/// <summary>
/// Represents a structured attachment that can be appended to GitHub comments.
/// Attachments enable bots to add rich, formatted content without modifying the original comment text.
/// </summary>
public class CommentAttachment
{
    /// <summary>
    /// Gets or sets the title of the attachment.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the URL that the title should link to.
    /// </summary>
    public string? TitleLink { get; set; }

    /// <summary>
    /// Gets or sets the main text content of the attachment.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the color for the attachment (CSS color name or hex).
    /// Note: This is primarily for semantic purposes; GitHub Markdown rendering may not display colors.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the collection of fields to display in the attachment.
    /// </summary>
    public List<AttachmentField>? Fields { get; init; }
}
