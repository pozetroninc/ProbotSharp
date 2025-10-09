// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using ProbotSharp.Domain.Attachments;

namespace ProbotSharp.Application.Services;

/// <summary>
/// Renders comment attachments as Markdown-formatted text.
/// </summary>
public static class AttachmentRenderer
{
    /// <summary>
    /// Renders a single comment attachment as Markdown.
    /// </summary>
    /// <param name="attachment">The attachment to render.</param>
    /// <returns>A Markdown-formatted string representation of the attachment.</returns>
    public static string RenderAttachment(CommentAttachment attachment)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Title with optional link
        if (!string.IsNullOrEmpty(attachment.Title))
        {
            if (!string.IsNullOrEmpty(attachment.TitleLink))
            {
                sb.AppendLine($"### [{attachment.Title}]({attachment.TitleLink})");
            }
            else
            {
                sb.AppendLine($"### {attachment.Title}");
            }
        }

        // Main text
        if (!string.IsNullOrEmpty(attachment.Text))
        {
            sb.AppendLine();
            sb.AppendLine(attachment.Text);
        }

        // Fields as table or list
        if (attachment.Fields != null && attachment.Fields.Any())
        {
            sb.AppendLine();
            foreach (var field in attachment.Fields)
            {
                sb.AppendLine($"**{field.Title}**: {field.Value}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("---");

        return sb.ToString();
    }

    /// <summary>
    /// Renders multiple comment attachments as Markdown.
    /// </summary>
    /// <param name="attachments">The attachments to render.</param>
    /// <returns>A Markdown-formatted string representation of all attachments.</returns>
    public static string RenderAttachments(IEnumerable<CommentAttachment> attachments)
    {
        return string.Join("\n", attachments.Select(RenderAttachment));
    }
}
