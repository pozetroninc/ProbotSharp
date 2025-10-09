// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Attachments;

namespace ProbotSharp.Application.Tests.Services;

public class AttachmentRendererTests
{
    [Fact]
    public void RenderAttachment_WithTitleOnly_ShouldRenderCorrectly()
    {
        // Arrange
        var attachment = new CommentAttachment
        {
            Title = "Test Title",
        };

        // Act
        var result = AttachmentRenderer.RenderAttachment(attachment);

        // Assert
        Assert.Contains("### Test Title", result);
        Assert.Contains("---", result);
    }

    [Fact]
    public void RenderAttachment_WithTitleAndLink_ShouldRenderLinkedTitle()
    {
        // Arrange
        var attachment = new CommentAttachment
        {
            Title = "Build Status",
            TitleLink = "https://ci.example.com/builds/123",
        };

        // Act
        var result = AttachmentRenderer.RenderAttachment(attachment);

        // Assert
        Assert.Contains("### [Build Status](https://ci.example.com/builds/123)", result);
    }

    [Fact]
    public void RenderAttachment_WithText_ShouldIncludeText()
    {
        // Arrange
        var attachment = new CommentAttachment
        {
            Title = "Status",
            Text = "Build completed successfully",
        };

        // Act
        var result = AttachmentRenderer.RenderAttachment(attachment);

        // Assert
        Assert.Contains("Build completed successfully", result);
    }

    [Fact]
    public void RenderAttachment_WithFields_ShouldRenderFieldsAsBoldKeyValue()
    {
        // Arrange
        var attachment = new CommentAttachment
        {
            Title = "Test Results",
            Fields = new List<AttachmentField>
            {
                new() { Title = "Duration", Value = "2m 34s", Short = true },
                new() { Title = "Tests", Value = "142 passed", Short = true },
            },
        };

        // Act
        var result = AttachmentRenderer.RenderAttachment(attachment);

        // Assert
        Assert.Contains("**Duration**: 2m 34s", result);
        Assert.Contains("**Tests**: 142 passed", result);
    }

    [Fact]
    public void RenderAttachment_WithCompleteAttachment_ShouldRenderAllComponents()
    {
        // Arrange
        var attachment = new CommentAttachment
        {
            Title = "Build Status",
            TitleLink = "https://ci.example.com/builds/123",
            Text = "Latest build completed successfully",
            Color = "green",
            Fields = new List<AttachmentField>
            {
                new() { Title = "Duration", Value = "2m 34s", Short = true },
                new() { Title = "Branch", Value = "main", Short = true },
            },
        };

        // Act
        var result = AttachmentRenderer.RenderAttachment(attachment);

        // Assert
        Assert.Contains("---", result);
        Assert.Contains("### [Build Status](https://ci.example.com/builds/123)", result);
        Assert.Contains("Latest build completed successfully", result);
        Assert.Contains("**Duration**: 2m 34s", result);
        Assert.Contains("**Branch**: main", result);
    }

    [Fact]
    public void RenderAttachment_WithEmptyAttachment_ShouldRenderMinimalStructure()
    {
        // Arrange
        var attachment = new CommentAttachment();

        // Act
        var result = AttachmentRenderer.RenderAttachment(attachment);

        // Assert
        Assert.Contains("---", result);
        // Should still have the horizontal rules
        Assert.Equal(2, result.Split("---").Length - 1);
    }

    [Fact]
    public void RenderAttachments_WithMultipleAttachments_ShouldJoinWithNewlines()
    {
        // Arrange
        var attachments = new[]
        {
            new CommentAttachment { Title = "First" },
            new CommentAttachment { Title = "Second" },
        };

        // Act
        var result = AttachmentRenderer.RenderAttachments(attachments);

        // Assert
        Assert.Contains("### First", result);
        Assert.Contains("### Second", result);
        // Should have multiple attachment sections
        var sections = result.Split("---");
        Assert.True(sections.Length >= 4); // At least 2 attachments with opening/closing rules
    }

    [Fact]
    public void RenderAttachments_WithEmptyList_ShouldReturnEmptyString()
    {
        // Arrange
        var attachments = Array.Empty<CommentAttachment>();

        // Act
        var result = AttachmentRenderer.RenderAttachments(attachments);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RenderAttachment_WithSpecialCharactersInTitle_ShouldNotEscape()
    {
        // Arrange
        var attachment = new CommentAttachment
        {
            Title = "Test & Build <Status>",
        };

        // Act
        var result = AttachmentRenderer.RenderAttachment(attachment);

        // Assert
        // Markdown rendering is done by GitHub, we just pass through
        Assert.Contains("### Test & Build <Status>", result);
    }

    [Fact]
    public void RenderAttachment_WithMarkdownInText_ShouldPreserveMarkdown()
    {
        // Arrange
        var attachment = new CommentAttachment
        {
            Title = "Status",
            Text = "Build **passed** with `warnings`",
        };

        // Act
        var result = AttachmentRenderer.RenderAttachment(attachment);

        // Assert
        Assert.Contains("Build **passed** with `warnings`", result);
    }

    [Fact]
    public void RenderAttachment_Structure_ShouldStartAndEndWithHorizontalRules()
    {
        // Arrange
        var attachment = new CommentAttachment
        {
            Title = "Test",
        };

        // Act
        var result = AttachmentRenderer.RenderAttachment(attachment);

        // Assert
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("---", lines[0]);
        Assert.Contains("---", lines[^1]);
    }
}
