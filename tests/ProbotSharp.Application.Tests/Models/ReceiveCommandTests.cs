// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Tests.Models;

public class ReceiveCommandTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var eventName = WebhookEventName.Create("issues");
        var payloadPath = "/path/to/payload.json";

        // Act
        var command = new ReceiveCommand(eventName, payloadPath);

        // Assert
        command.EventName.Should().Be(eventName);
        command.PayloadPath.Should().Be(payloadPath);
    }

    [Fact]
    public void Constructor_WithNullPayloadPath_ShouldThrowArgumentException()
    {
        // Arrange
        var eventName = WebhookEventName.Create("issues");

        // Act
        var act = () => new ReceiveCommand(eventName, null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("PayloadPath")
            .WithMessage("Payload path cannot be null or whitespace.*");
    }

    [Fact]
    public void Constructor_WithEmptyPayloadPath_ShouldThrowArgumentException()
    {
        // Arrange
        var eventName = WebhookEventName.Create("issues");

        // Act
        var act = () => new ReceiveCommand(eventName, string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("PayloadPath")
            .WithMessage("Payload path cannot be null or whitespace.*");
    }

    [Fact]
    public void Constructor_WithWhitespacePayloadPath_ShouldThrowArgumentException()
    {
        // Arrange
        var eventName = WebhookEventName.Create("issues");

        // Act
        var act = () => new ReceiveCommand(eventName, "   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("PayloadPath")
            .WithMessage("Payload path cannot be null or whitespace.*");
    }

    #endregion

    #region Property Tests

    [Fact]
    public void EventName_Property_ShouldReturnConstructorValue()
    {
        // Arrange
        var eventName = WebhookEventName.Create("pull_request");
        var payloadPath = "/path/to/payload.json";

        // Act
        var command = new ReceiveCommand(eventName, payloadPath);

        // Assert
        command.EventName.Should().Be(eventName);
        command.EventName.Value.Should().Be("pull_request");
    }

    [Fact]
    public void PayloadPath_Property_ShouldReturnConstructorValue()
    {
        // Arrange
        var eventName = WebhookEventName.Create("issues");
        var expectedPath = "/usr/local/payloads/issue-opened.json";

        // Act
        var command = new ReceiveCommand(eventName, expectedPath);

        // Assert
        command.PayloadPath.Should().Be(expectedPath);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equals_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var eventName = WebhookEventName.Create("issues");
        var payloadPath = "/path/to/payload.json";
        var command1 = new ReceiveCommand(eventName, payloadPath);
        var command2 = new ReceiveCommand(eventName, payloadPath);

        // Act & Assert
        command1.Should().Be(command2);
    }

    [Fact]
    public void Equals_WithDifferentEventName_ShouldNotBeEqual()
    {
        // Arrange
        var eventName1 = WebhookEventName.Create("issues");
        var eventName2 = WebhookEventName.Create("pull_request");
        var payloadPath = "/path/to/payload.json";
        var command1 = new ReceiveCommand(eventName1, payloadPath);
        var command2 = new ReceiveCommand(eventName2, payloadPath);

        // Act & Assert
        command1.Should().NotBe(command2);
    }

    [Fact]
    public void Equals_WithDifferentPayloadPath_ShouldNotBeEqual()
    {
        // Arrange
        var eventName = WebhookEventName.Create("issues");
        var command1 = new ReceiveCommand(eventName, "/path1/payload.json");
        var command2 = new ReceiveCommand(eventName, "/path2/payload.json");

        // Act & Assert
        command1.Should().NotBe(command2);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithRelativePayloadPath_ShouldAccept()
    {
        // Arrange
        var eventName = WebhookEventName.Create("issues");

        // Act
        var command = new ReceiveCommand(eventName, "./payloads/issue.json");

        // Assert
        command.PayloadPath.Should().Be("./payloads/issue.json");
    }

    [Fact]
    public void Constructor_WithAbsolutePayloadPath_ShouldAccept()
    {
        // Arrange
        var eventName = WebhookEventName.Create("issues");

        // Act
        var command = new ReceiveCommand(eventName, "/usr/local/payloads/issue.json");

        // Assert
        command.PayloadPath.Should().Be("/usr/local/payloads/issue.json");
    }

    [Fact]
    public void Constructor_WithWindowsPayloadPath_ShouldAccept()
    {
        // Arrange
        var eventName = WebhookEventName.Create("issues");

        // Act
        var command = new ReceiveCommand(eventName, "C:\\Payloads\\issue.json");

        // Assert
        command.PayloadPath.Should().Be("C:\\Payloads\\issue.json");
    }

    [Fact]
    public void Constructor_WithPayloadPathContainingSpaces_ShouldAccept()
    {
        // Arrange
        var eventName = WebhookEventName.Create("issues");

        // Act
        var command = new ReceiveCommand(eventName, "/path/to/my payloads/issue.json");

        // Assert
        command.PayloadPath.Should().Be("/path/to/my payloads/issue.json");
    }

    [Fact]
    public void Constructor_WithVariousEventTypes_ShouldAccept()
    {
        // Arrange
        var eventTypes = new[] { "issues", "pull_request", "push", "release", "check_run" };

        foreach (var eventType in eventTypes)
        {
            // Act
            var eventName = WebhookEventName.Create(eventType);
            var command = new ReceiveCommand(eventName, "/path/payload.json");

            // Assert
            command.EventName.Value.Should().Be(eventType);
        }
    }

    [Fact]
    public void Constructor_WithVeryLongPayloadPath_ShouldAccept()
    {
        // Arrange
        var eventName = WebhookEventName.Create("issues");
        var longPath = "/path/" + new string('a', 500) + "/payload.json";

        // Act
        var command = new ReceiveCommand(eventName, longPath);

        // Assert
        command.PayloadPath.Should().Be(longPath);
    }

    #endregion
}
