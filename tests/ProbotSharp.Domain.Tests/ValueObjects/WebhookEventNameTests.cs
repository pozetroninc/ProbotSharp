// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Tests.ValueObjects;

/// <summary>
/// Tests for the WebhookEventName value object.
/// Validates creation, edge cases, and string representation.
/// </summary>
public class WebhookEventNameTests
{
    [Fact]
    public void Create_WithValidString_ShouldReturnInstance()
    {
        // Arrange & Act
        var eventName = WebhookEventName.Create("push");

        // Assert
        eventName.Value.Should().Be("push");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrimAndReturnInstance()
    {
        // Arrange & Act
        var eventName = WebhookEventName.Create("  pull_request  ");

        // Assert
        eventName.Value.Should().Be("pull_request");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidString_ShouldThrow(string value)
    {
        // Arrange & Act
        var act = () => WebhookEventName.Create(value!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Webhook event name cannot be null or whitespace.*")
            .And.ParamName.Should().Be("value");
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var eventName = WebhookEventName.Create("issues");

        // Act
        var result = eventName.ToString();

        // Assert
        result.Should().Be("issues");
    }

    [Fact]
    public void RecordEquality_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var event1 = WebhookEventName.Create("push");
        var event2 = WebhookEventName.Create("push");

        // Act & Assert
        event1.Should().Be(event2);
        (event1 == event2).Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var event1 = WebhookEventName.Create("push");
        var event2 = WebhookEventName.Create("pull_request");

        // Act & Assert
        event1.Should().NotBe(event2);
        (event1 != event2).Should().BeTrue();
    }

    [Theory]
    [InlineData("push")]
    [InlineData("pull_request")]
    [InlineData("issues")]
    [InlineData("issue_comment")]
    [InlineData("pull_request_review")]
    [InlineData("check_run")]
    [InlineData("check_suite")]
    public void Create_WithCommonGitHubEventNames_ShouldSucceed(string eventName)
    {
        // Arrange & Act
        var result = WebhookEventName.Create(eventName);

        // Assert
        result.Value.Should().Be(eventName);
    }
}
