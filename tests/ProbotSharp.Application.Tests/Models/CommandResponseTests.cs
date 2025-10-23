// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;

namespace ProbotSharp.Application.Tests.Models;

public class CommandResponseTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithMinimalParameters_ShouldCreateInstance()
    {
        // Act
        var response = new CommandResponse(IsSuccessful: true, ExitCode: 0);

        // Assert
        response.IsSuccessful.Should().BeTrue();
        response.ExitCode.Should().Be(0);
        response.Output.Should().BeNull();
        response.ErrorOutput.Should().BeNull();
        response.CommandName.Should().BeNull();
        response.Duration.Should().BeNull();
        response.Metadata.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldSetAllProperties()
    {
        // Arrange
        var executedAt = DateTimeOffset.UtcNow;
        var duration = TimeSpan.FromSeconds(5);
        var metadata = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        var response = new CommandResponse(
            IsSuccessful: true,
            ExitCode: 0,
            Output: "Success output",
            ErrorOutput: null,
            CommandName: "test-command",
            ExecutedAt: executedAt,
            Duration: duration,
            Metadata: metadata);

        // Assert
        response.IsSuccessful.Should().BeTrue();
        response.ExitCode.Should().Be(0);
        response.Output.Should().Be("Success output");
        response.ErrorOutput.Should().BeNull();
        response.CommandName.Should().Be("test-command");
        response.ExecutedAt.Should().Be(executedAt);
        response.Duration.Should().Be(duration);
        response.Metadata.Should().BeSameAs(metadata);
    }

    [Fact]
    public void DefaultConstructor_ShouldCreateUnsuccessfulResponse()
    {
        // Act
        var response = new CommandResponse();

        // Assert
        response.IsSuccessful.Should().BeFalse();
        response.ExitCode.Should().Be(0);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public void SuccessResponse_WithOutput_ShouldHaveCorrectProperties()
    {
        // Arrange
        var output = "Command executed successfully";

        // Act
        var response = new CommandResponse(
            IsSuccessful: true,
            ExitCode: 0,
            Output: output);

        // Assert
        response.IsSuccessful.Should().BeTrue();
        response.ExitCode.Should().Be(0);
        response.Output.Should().Be(output);
        response.ErrorOutput.Should().BeNull();
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public void FailureResponse_WithErrorOutput_ShouldHaveCorrectProperties()
    {
        // Arrange
        var errorOutput = "Command failed with error";

        // Act
        var response = new CommandResponse(
            IsSuccessful: false,
            ExitCode: 1,
            ErrorOutput: errorOutput);

        // Assert
        response.IsSuccessful.Should().BeFalse();
        response.ExitCode.Should().Be(1);
        response.ErrorOutput.Should().Be(errorOutput);
        response.Output.Should().BeNull();
    }

    [Fact]
    public void FailureResponse_WithNonZeroExitCode_ShouldBeValid()
    {
        // Act
        var response = new CommandResponse(IsSuccessful: false, ExitCode: 127);

        // Assert
        response.IsSuccessful.Should().BeFalse();
        response.ExitCode.Should().Be(127);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equals_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var response1 = new CommandResponse(true, 0, "output");
        var response2 = new CommandResponse(true, 0, "output");

        // Act & Assert
        response1.Should().Be(response2);
    }

    [Fact]
    public void Equals_WithDifferentIsSuccessful_ShouldNotBeEqual()
    {
        // Arrange
        var response1 = new CommandResponse(true, 0);
        var response2 = new CommandResponse(false, 0);

        // Act & Assert
        response1.Should().NotBe(response2);
    }

    [Fact]
    public void Equals_WithDifferentExitCode_ShouldNotBeEqual()
    {
        // Arrange
        var response1 = new CommandResponse(true, 0);
        var response2 = new CommandResponse(true, 1);

        // Act & Assert
        response1.Should().NotBe(response2);
    }

    [Fact]
    public void Equals_WithDifferentOutput_ShouldNotBeEqual()
    {
        // Arrange
        var response1 = new CommandResponse(true, 0, Output: "output1");
        var response2 = new CommandResponse(true, 0, Output: "output2");

        // Act & Assert
        response1.Should().NotBe(response2);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithNegativeExitCode_ShouldAccept()
    {
        // Act
        var response = new CommandResponse(false, -1);

        // Assert
        response.ExitCode.Should().Be(-1);
    }

    [Fact]
    public void Constructor_WithEmptyMetadata_ShouldPreserveEmptyDictionary()
    {
        // Arrange
        var metadata = new Dictionary<string, object>();

        // Act
        var response = new CommandResponse(true, 0, Metadata: metadata);

        // Assert
        response.Metadata.Should().BeEmpty();
        response.Metadata.Should().BeSameAs(metadata);
    }

    [Fact]
    public void Constructor_WithZeroDuration_ShouldAccept()
    {
        // Act
        var response = new CommandResponse(true, 0, Duration: TimeSpan.Zero);

        // Assert
        response.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Constructor_WithNullCommandName_ShouldAccept()
    {
        // Act
        var response = new CommandResponse(true, 0, CommandName: null);

        // Assert
        response.CommandName.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyCommandName_ShouldAccept()
    {
        // Act
        var response = new CommandResponse(true, 0, CommandName: string.Empty);

        // Assert
        response.CommandName.Should().BeEmpty();
    }

    #endregion
}
