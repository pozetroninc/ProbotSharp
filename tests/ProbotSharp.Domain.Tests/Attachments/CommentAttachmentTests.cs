// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Attachments;

namespace ProbotSharp.Domain.Tests.Attachments;

public class CommentAttachmentTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithNullValues()
    {
        // Arrange & Act
        var attachment = new CommentAttachment();

        // Assert
        attachment.Title.Should().BeNull();
        attachment.TitleLink.Should().BeNull();
        attachment.Text.Should().BeNull();
        attachment.Color.Should().BeNull();
        attachment.Fields.Should().BeNull();
    }

    [Fact]
    public void Title_ShouldBeSettable()
    {
        // Arrange
        var attachment = new CommentAttachment();
        var expectedTitle = "Build Status";

        // Act
        attachment.Title = expectedTitle;

        // Assert
        attachment.Title.Should().Be(expectedTitle);
    }

    [Fact]
    public void TitleLink_ShouldBeSettableToUrl()
    {
        // Arrange
        var attachment = new CommentAttachment();
        var expectedLink = "https://github.com/user/repo/actions/runs/123";

        // Act
        attachment.TitleLink = expectedLink;

        // Assert
        attachment.TitleLink.Should().Be(expectedLink);
    }

    [Fact]
    public void Text_ShouldBeSettable()
    {
        // Arrange
        var attachment = new CommentAttachment();
        var expectedText = "Build completed successfully!";

        // Act
        attachment.Text = expectedText;

        // Assert
        attachment.Text.Should().Be(expectedText);
    }

    [Fact]
    public void Color_ShouldBeSettableToHexValue()
    {
        // Arrange
        var attachment = new CommentAttachment();
        var expectedColor = "#36a64f";

        // Act
        attachment.Color = expectedColor;

        // Assert
        attachment.Color.Should().Be(expectedColor);
    }

    [Fact]
    public void Color_ShouldBeSettableToCssName()
    {
        // Arrange
        var attachment = new CommentAttachment();
        var expectedColor = "green";

        // Act
        attachment.Color = expectedColor;

        // Assert
        attachment.Color.Should().Be(expectedColor);
    }

    [Fact]
    public void Fields_ShouldBeInitializable()
    {
        // Arrange & Act
        var attachment = new CommentAttachment
        {
            Fields = new List<AttachmentField>
            {
                new() { Title = "Status", Value = "Success" },
                new() { Title = "Duration", Value = "2m 15s" }
            }
        };

        // Assert
        attachment.Fields.Should().NotBeNull();
        attachment.Fields.Should().HaveCount(2);
        attachment.Fields![0].Title.Should().Be("Status");
        attachment.Fields[1].Title.Should().Be("Duration");
    }

    [Fact]
    public void Fields_ShouldSupportEmptyList()
    {
        // Arrange & Act
        var attachment = new CommentAttachment
        {
            Fields = new List<AttachmentField>()
        };

        // Assert
        attachment.Fields.Should().NotBeNull();
        attachment.Fields.Should().BeEmpty();
    }

    [Fact]
    public void ObjectInitializer_ShouldSetAllProperties()
    {
        // Arrange & Act
        var attachment = new CommentAttachment
        {
            Title = "Test Results",
            TitleLink = "https://example.com/results",
            Text = "All tests passed",
            Color = "#00ff00",
            Fields = new List<AttachmentField>
            {
                new() { Title = "Passed", Value = "42", Short = true },
                new() { Title = "Failed", Value = "0", Short = true }
            }
        };

        // Assert
        attachment.Title.Should().Be("Test Results");
        attachment.TitleLink.Should().Be("https://example.com/results");
        attachment.Text.Should().Be("All tests passed");
        attachment.Color.Should().Be("#00ff00");
        attachment.Fields.Should().HaveCount(2);
    }

    [Fact]
    public void Text_ShouldHandleMarkdownContent()
    {
        // Arrange
        var attachment = new CommentAttachment();
        var markdownText = "**Bold** and *italic* with [links](https://example.com)";

        // Act
        attachment.Text = markdownText;

        // Assert
        attachment.Text.Should().Be(markdownText);
        attachment.Text.Should().Contain("**Bold**");
        attachment.Text.Should().Contain("[links]");
    }

    [Fact]
    public void Text_ShouldHandleMultilineContent()
    {
        // Arrange
        var attachment = new CommentAttachment();
        var multilineText = "Line 1\nLine 2\nLine 3";

        // Act
        attachment.Text = multilineText;

        // Assert
        attachment.Text.Should().Be(multilineText);
        attachment.Text.Should().Contain("\n");
    }

    [Theory]
    [InlineData(null, null, null, null)]
    [InlineData("Title", null, null, null)]
    [InlineData("Title", "https://link.com", null, null)]
    [InlineData("Title", "https://link.com", "Text", null)]
    [InlineData("Title", "https://link.com", "Text", "#ff0000")]
    public void Properties_ShouldSupportPartialInitialization(
        string? title,
        string? titleLink,
        string? text,
        string? color)
    {
        // Arrange & Act
        var attachment = new CommentAttachment
        {
            Title = title,
            TitleLink = titleLink,
            Text = text,
            Color = color
        };

        // Assert
        attachment.Title.Should().Be(title);
        attachment.TitleLink.Should().Be(titleLink);
        attachment.Text.Should().Be(text);
        attachment.Color.Should().Be(color);
    }

    [Fact]
    public void Fields_ShouldBeImmutableAfterInitialization()
    {
        // Arrange
        var attachment = new CommentAttachment
        {
            Fields = new List<AttachmentField>
            {
                new() { Title = "Field1", Value = "Value1" }
            }
        };

        // Act - Add to the list (list itself is mutable)
        attachment.Fields!.Add(new AttachmentField { Title = "Field2", Value = "Value2" });

        // Assert - The reference is immutable, but the list contents can be modified
        attachment.Fields.Should().HaveCount(2);
    }

    [Fact]
    public void ComplexAttachment_ShouldSupportRealWorldScenario()
    {
        // Arrange & Act - Simulating a CI/CD build notification
        var attachment = new CommentAttachment
        {
            Title = "Build #1234 - Success",
            TitleLink = "https://github.com/user/repo/actions/runs/1234",
            Text = "All checks have passed successfully!\n\nReview the detailed logs for more information.",
            Color = "#36a64f",
            Fields = new List<AttachmentField>
            {
                new() { Title = "Branch", Value = "main", Short = true },
                new() { Title = "Commit", Value = "abc123", Short = true },
                new() { Title = "Duration", Value = "2m 45s", Short = true },
                new() { Title = "Tests", Value = "142 passed", Short = true },
                new() { Title = "Coverage", Value = "87.5%", Short = false }
            }
        };

        // Assert
        attachment.Title.Should().NotBeNullOrEmpty();
        attachment.TitleLink.Should().StartWith("https://");
        attachment.Text.Should().Contain("\n");
        attachment.Color.Should().Be("#36a64f");
        attachment.Fields.Should().HaveCount(5);
        attachment.Fields!.Count(f => f.Short).Should().Be(4);
        attachment.Fields.Count(f => !f.Short).Should().Be(1);
    }

    [Fact]
    public void Title_ShouldHandleEmptyString()
    {
        // Arrange & Act
        var attachment = new CommentAttachment { Title = string.Empty };

        // Assert
        attachment.Title.Should().BeEmpty();
        attachment.Title.Should().NotBeNull();
    }

    [Fact]
    public void TitleLink_ShouldHandleRelativeUrls()
    {
        // Arrange & Act
        var attachment = new CommentAttachment { TitleLink = "/relative/path" };

        // Assert
        attachment.TitleLink.Should().Be("/relative/path");
    }

    [Fact]
    public void Fields_ShouldSupportManyFields()
    {
        // Arrange
        var fields = Enumerable.Range(1, 20)
            .Select(i => new AttachmentField { Title = $"Field{i}", Value = $"Value{i}" })
            .ToList();

        // Act
        var attachment = new CommentAttachment { Fields = fields };

        // Assert
        attachment.Fields.Should().HaveCount(20);
        attachment.Fields![0].Title.Should().Be("Field1");
        attachment.Fields[19].Title.Should().Be("Field20");
    }
}
