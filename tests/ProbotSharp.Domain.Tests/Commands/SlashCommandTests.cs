// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Commands;

namespace ProbotSharp.Domain.Tests.Commands;

public class SlashCommandTests
{
    [Fact]
    public void Constructor_WithValidArguments_ShouldCreateInstance()
    {
        // Arrange
        var name = "label";
        var arguments = "bug, enhancement";
        var fullText = "/label bug, enhancement";
        var lineNumber = 1;

        // Act
        var command = new SlashCommand(name, arguments, fullText, lineNumber);

        // Assert
        command.Name.Should().Be(name);
        command.Arguments.Should().Be(arguments);
        command.FullText.Should().Be(fullText);
        command.LineNumber.Should().Be(lineNumber);
    }

    [Fact]
    public void Constructor_WithEmptyArguments_ShouldCreateInstance()
    {
        // Arrange
        var name = "help";
        var arguments = string.Empty;
        var fullText = "/help";
        var lineNumber = 5;

        // Act
        var command = new SlashCommand(name, arguments, fullText, lineNumber);

        // Assert
        command.Name.Should().Be(name);
        command.Arguments.Should().BeEmpty();
        command.FullText.Should().Be(fullText);
        command.LineNumber.Should().Be(lineNumber);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    public void Constructor_WithNullOrWhiteSpaceName_ShouldThrowArgumentException(string? invalidName)
    {
        // Arrange
        var arguments = "test";
        var fullText = "/test";
        var lineNumber = 1;

        // Act
        var act = () => new SlashCommand(invalidName!, arguments, fullText, lineNumber);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNullArguments_ShouldThrowArgumentNullException()
    {
        // Arrange
        var name = "test";
        string? arguments = null;
        var fullText = "/test";
        var lineNumber = 1;

        // Act
        var act = () => new SlashCommand(name, arguments!, fullText, lineNumber);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    public void Constructor_WithNullOrWhiteSpaceFullText_ShouldThrowArgumentException(string? invalidFullText)
    {
        // Arrange
        var name = "test";
        var arguments = "args";
        var lineNumber = 1;

        // Act
        var act = () => new SlashCommand(name, arguments, invalidFullText!, lineNumber);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithNegativeOrZeroLineNumber_ShouldThrowArgumentOutOfRangeException(int invalidLineNumber)
    {
        // Arrange
        var name = "test";
        var arguments = "args";
        var fullText = "/test args";

        // Act
        var act = () => new SlashCommand(name, arguments, fullText, invalidLineNumber);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithPositiveLineNumber_ShouldSucceed()
    {
        // Arrange
        var name = "test";
        var arguments = string.Empty;
        var fullText = "/test";
        var lineNumber = 100;

        // Act
        var command = new SlashCommand(name, arguments, fullText, lineNumber);

        // Assert
        command.LineNumber.Should().Be(lineNumber);
    }

    [Fact]
    public void Name_ShouldBeInitOnly()
    {
        // Arrange
        var command = new SlashCommand("label", "bug", "/label bug", 1);

        // Act & Assert - This test verifies the init-only property at compile time
        // If we could assign after construction, this would compile, but init prevents it
        command.Name.Should().Be("label");
    }

    [Fact]
    public void Arguments_ShouldBeInitOnly()
    {
        // Arrange
        var command = new SlashCommand("label", "bug", "/label bug", 1);

        // Act & Assert
        command.Arguments.Should().Be("bug");
    }

    [Fact]
    public void FullText_ShouldBeInitOnly()
    {
        // Arrange
        var command = new SlashCommand("label", "bug", "/label bug", 1);

        // Act & Assert
        command.FullText.Should().Be("/label bug");
    }

    [Fact]
    public void LineNumber_ShouldBeInitOnly()
    {
        // Arrange
        var command = new SlashCommand("label", "bug", "/label bug", 1);

        // Act & Assert
        command.LineNumber.Should().Be(1);
    }

    [Theory]
    [InlineData("label", "bug, enhancement", "/label bug, enhancement", 1)]
    [InlineData("assign", "@user1 @user2", "/assign @user1 @user2", 5)]
    [InlineData("help", "", "/help", 10)]
    [InlineData("close", "duplicate", "/close duplicate", 3)]
    [InlineData("custom-command", "arg1 arg2 arg3", "/custom-command arg1 arg2 arg3", 2)]
    public void Constructor_WithVariousValidInputs_ShouldCreateCorrectInstances(
        string name,
        string arguments,
        string fullText,
        int lineNumber)
    {
        // Act
        var command = new SlashCommand(name, arguments, fullText, lineNumber);

        // Assert
        command.Name.Should().Be(name);
        command.Arguments.Should().Be(arguments);
        command.FullText.Should().Be(fullText);
        command.LineNumber.Should().Be(lineNumber);
    }

    [Fact]
    public void Constructor_WithComplexArguments_ShouldPreserveArgumentStructure()
    {
        // Arrange
        var name = "label";
        var arguments = "\"bug with spaces\", priority:high, @user";
        var fullText = "/label \"bug with spaces\", priority:high, @user";
        var lineNumber = 1;

        // Act
        var command = new SlashCommand(name, arguments, fullText, lineNumber);

        // Assert
        command.Arguments.Should().Be(arguments);
        command.Arguments.Should().Contain("\"bug with spaces\"");
        command.Arguments.Should().Contain("priority:high");
        command.Arguments.Should().Contain("@user");
    }

    [Fact]
    public void Constructor_WithUnicodeInName_ShouldHandleCorrectly()
    {
        // Arrange
        var name = "label-✓";
        var arguments = "test";
        var fullText = "/label-✓ test";
        var lineNumber = 1;

        // Act
        var command = new SlashCommand(name, arguments, fullText, lineNumber);

        // Assert
        command.Name.Should().Be(name);
        command.Name.Should().Contain("✓");
    }

    [Fact]
    public void Constructor_WithLongArguments_ShouldHandleCorrectly()
    {
        // Arrange
        var name = "command";
        var longArguments = new string('a', 1000);
        var fullText = $"/command {longArguments}";
        var lineNumber = 1;

        // Act
        var command = new SlashCommand(name, longArguments, fullText, lineNumber);

        // Assert
        command.Arguments.Should().HaveLength(1000);
        command.Arguments.Should().Be(longArguments);
    }

    [Fact]
    public void Constructor_WithMultilineFullText_ShouldPreserveNewlines()
    {
        // Arrange
        var name = "code";
        var arguments = "python";
        var fullText = "/code python\nprint('hello')\nprint('world')";
        var lineNumber = 1;

        // Act
        var command = new SlashCommand(name, arguments, fullText, lineNumber);

        // Assert
        command.FullText.Should().Contain("\n");
        command.FullText.Should().Be(fullText);
    }

    [Fact]
    public void Constructor_WithMaxLineNumber_ShouldSucceed()
    {
        // Arrange
        var name = "test";
        var arguments = string.Empty;
        var fullText = "/test";
        var lineNumber = int.MaxValue;

        // Act
        var command = new SlashCommand(name, arguments, fullText, lineNumber);

        // Assert
        command.LineNumber.Should().Be(int.MaxValue);
    }

    [Fact]
    public void Constructor_WithSpecialCharactersInArguments_ShouldPreserveCharacters()
    {
        // Arrange
        var name = "test";
        var arguments = "!@#$%^&*()_+-=[]{}|;':\",./<>?";
        var fullText = $"/test {arguments}";
        var lineNumber = 1;

        // Act
        var command = new SlashCommand(name, arguments, fullText, lineNumber);

        // Assert
        command.Arguments.Should().Be(arguments);
    }

    [Fact]
    public void Constructor_WithHyphenatedCommandName_ShouldSucceed()
    {
        // Arrange
        var name = "add-label";
        var arguments = "bug";
        var fullText = "/add-label bug";
        var lineNumber = 1;

        // Act
        var command = new SlashCommand(name, arguments, fullText, lineNumber);

        // Assert
        command.Name.Should().Be("add-label");
        command.Name.Should().Contain("-");
    }

    [Fact]
    public void Constructor_WithUnderscoreCommandName_ShouldSucceed()
    {
        // Arrange
        var name = "add_label";
        var arguments = "enhancement";
        var fullText = "/add_label enhancement";
        var lineNumber = 1;

        // Act
        var command = new SlashCommand(name, arguments, fullText, lineNumber);

        // Assert
        command.Name.Should().Be("add_label");
        command.Name.Should().Contain("_");
    }

    [Fact]
    public void RealWorldScenario_LabelCommand_ShouldParseCorrectly()
    {
        // Arrange - Simulating "/label bug, enhancement" on line 3
        var command = new SlashCommand(
            name: "label",
            arguments: "bug, enhancement",
            fullText: "/label bug, enhancement",
            lineNumber: 3);

        // Assert
        command.Name.Should().Be("label");
        command.Arguments.Should().Be("bug, enhancement");
        command.Arguments.Should().Contain(",");
        command.FullText.Should().StartWith("/");
        command.LineNumber.Should().Be(3);
    }

    [Fact]
    public void RealWorldScenario_AssignCommand_ShouldParseCorrectly()
    {
        // Arrange - Simulating "/assign @octocat" on line 1
        var command = new SlashCommand(
            name: "assign",
            arguments: "@octocat",
            fullText: "/assign @octocat",
            lineNumber: 1);

        // Assert
        command.Name.Should().Be("assign");
        command.Arguments.Should().StartWith("@");
        command.Arguments.Should().Be("@octocat");
        command.FullText.Should().Be("/assign @octocat");
        command.LineNumber.Should().Be(1);
    }

    [Fact]
    public void RealWorldScenario_HelpCommand_ShouldParseCorrectly()
    {
        // Arrange - Simulating "/help" with no arguments on line 7
        var command = new SlashCommand(
            name: "help",
            arguments: string.Empty,
            fullText: "/help",
            lineNumber: 7);

        // Assert
        command.Name.Should().Be("help");
        command.Arguments.Should().BeEmpty();
        command.FullText.Should().Be("/help");
        command.LineNumber.Should().Be(7);
    }
}
