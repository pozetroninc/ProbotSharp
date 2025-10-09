// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Commands;

namespace ProbotSharp.Domain.Tests.Commands;

public class SlashCommandParserTests
{
    [Fact]
    public void Parse_WithSingleCommand_ShouldReturnOneCommand()
    {
        // Arrange
        var commentBody = "/label bug";

        // Act
        var commands = SlashCommandParser.Parse(commentBody).ToList();

        // Assert
        commands.Should().HaveCount(1);
        commands[0].Name.Should().Be("label");
        commands[0].Arguments.Should().Be("bug");
        commands[0].FullText.Should().Be("/label bug");
        commands[0].LineNumber.Should().Be(1);
    }

    [Fact]
    public void Parse_WithMultipleCommands_ShouldReturnAllCommands()
    {
        // Arrange
        var commentBody = @"/label bug, enhancement
Some regular comment text
/assign @johndoe";

        // Act
        var commands = SlashCommandParser.Parse(commentBody).ToList();

        // Assert
        commands.Should().HaveCount(2);

        commands[0].Name.Should().Be("label");
        commands[0].Arguments.Should().Be("bug, enhancement");
        commands[0].LineNumber.Should().Be(1);

        commands[1].Name.Should().Be("assign");
        commands[1].Arguments.Should().Be("@johndoe");
        commands[1].LineNumber.Should().Be(3);
    }

    [Fact]
    public void Parse_WithCommandWithoutArguments_ShouldReturnCommandWithEmptyArguments()
    {
        // Arrange
        var commentBody = "/help";

        // Act
        var commands = SlashCommandParser.Parse(commentBody).ToList();

        // Assert
        commands.Should().HaveCount(1);
        commands[0].Name.Should().Be("help");
        commands[0].Arguments.Should().BeEmpty();
        commands[0].FullText.Should().Be("/help");
    }

    [Fact]
    public void Parse_WithLeadingWhitespace_ShouldParseCommand()
    {
        // Arrange
        var commentBody = "   /label bug";

        // Act
        var commands = SlashCommandParser.Parse(commentBody).ToList();

        // Assert
        commands.Should().HaveCount(1);
        commands[0].Name.Should().Be("label");
        commands[0].Arguments.Should().Be("bug");
    }

    [Fact]
    public void Parse_WithHyphenatedCommandName_ShouldParseCommand()
    {
        // Arrange
        var commentBody = "/do-something arg1 arg2";

        // Act
        var commands = SlashCommandParser.Parse(commentBody).ToList();

        // Assert
        commands.Should().HaveCount(1);
        commands[0].Name.Should().Be("do-something");
        commands[0].Arguments.Should().Be("arg1 arg2");
    }

    [Fact]
    public void Parse_WithUnderscoreCommandName_ShouldParseCommand()
    {
        // Arrange
        var commentBody = "/do_something arg1 arg2";

        // Act
        var commands = SlashCommandParser.Parse(commentBody).ToList();

        // Assert
        commands.Should().HaveCount(1);
        commands[0].Name.Should().Be("do_something");
        commands[0].Arguments.Should().Be("arg1 arg2");
    }

    [Fact]
    public void Parse_WithMixedAlphanumericCommandName_ShouldParseCommand()
    {
        // Arrange
        var commentBody = "/cmd123 arguments";

        // Act
        var commands = SlashCommandParser.Parse(commentBody).ToList();

        // Assert
        commands.Should().HaveCount(1);
        commands[0].Name.Should().Be("cmd123");
        commands[0].Arguments.Should().Be("arguments");
    }

    [Fact]
    public void Parse_WithEmptyComment_ShouldReturnEmptyList()
    {
        // Arrange
        var commentBody = string.Empty;

        // Act
        var commands = SlashCommandParser.Parse(commentBody).ToList();

        // Assert
        commands.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithNullComment_ShouldReturnEmptyList()
    {
        // Arrange
        string? commentBody = null;

        // Act
        var commands = SlashCommandParser.Parse(commentBody!).ToList();

        // Assert
        commands.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithWhitespaceOnlyComment_ShouldReturnEmptyList()
    {
        // Arrange
        var commentBody = "   \n\t  \r\n  ";

        // Act
        var commands = SlashCommandParser.Parse(commentBody).ToList();

        // Assert
        commands.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithOnlyNonSlashLines_ShouldReturnEmptyList()
    {
        // Arrange
        var commentBody = @"This is a regular comment
Without any slash commands
Just plain text";

        // Act
        var commands = SlashCommandParser.Parse(commentBody).ToList();

        // Assert
        commands.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithSlashInMiddleOfLine_ShouldNotParseAsCommand()
    {
        // Arrange
        var commentBody = "This is not a /command";

        // Act
        var commands = SlashCommandParser.Parse(commentBody).ToList();

        // Assert
        commands.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithOnlySlash_ShouldNotParseAsCommand()
    {
        // Arrange
        var commentBody = "/";

        // Act
        var commands = SlashCommandParser.Parse(commentBody).ToList();

        // Assert
        commands.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithMultipleSpacesBetweenCommandAndArguments_ShouldTrimCorrectly()
    {
        // Arrange
        var commentBody = "/label    bug   enhancement";

        // Act
        var commands = SlashCommandParser.Parse(commentBody).ToList();

        // Assert
        commands.Should().HaveCount(1);
        commands[0].Name.Should().Be("label");
        commands[0].Arguments.Should().Be("bug   enhancement"); // Preserves internal spacing
    }

    [Fact]
    public void Parse_WithTrailingWhitespace_ShouldTrimCorrectly()
    {
        // Arrange
        var commentBody = "/label bug   \n";

        // Act
        var commands = SlashCommandParser.Parse(commentBody).ToList();

        // Assert
        commands.Should().HaveCount(1);
        commands[0].Arguments.Should().Be("bug");
    }

    [Fact]
    public void Parse_WithWindowsLineEndings_ShouldParseMultipleCommands()
    {
        // Arrange
        var commentBody = "/label bug\r\n/assign @user";

        // Act
        var commands = SlashCommandParser.Parse(commentBody).ToList();

        // Assert
        commands.Should().HaveCount(2);
        commands[0].Name.Should().Be("label");
        commands[1].Name.Should().Be("assign");
    }

    [Fact]
    public void Parse_WithUnixLineEndings_ShouldParseMultipleCommands()
    {
        // Arrange
        var commentBody = "/label bug\n/assign @user";

        // Act
        var commands = SlashCommandParser.Parse(commentBody).ToList();

        // Assert
        commands.Should().HaveCount(2);
        commands[0].Name.Should().Be("label");
        commands[1].Name.Should().Be("assign");
    }

    [Fact]
    public void Parse_WithMacLineEndings_ShouldParseMultipleCommands()
    {
        // Arrange
        var commentBody = "/label bug\r/assign @user";

        // Act
        var commands = SlashCommandParser.Parse(commentBody).ToList();

        // Assert
        commands.Should().HaveCount(2);
        commands[0].Name.Should().Be("label");
        commands[1].Name.Should().Be("assign");
    }

    [Fact]
    public void Parse_WithComplexRealWorldComment_ShouldParseCorrectly()
    {
        // Arrange
        var commentBody = @"Thanks for the contribution!

/label enhancement, good-first-issue
/assign @reviewer1

Let me know if you need help with this.
/cc @team-lead";

        // Act
        var commands = SlashCommandParser.Parse(commentBody).ToList();

        // Assert
        commands.Should().HaveCount(3);

        commands[0].Name.Should().Be("label");
        commands[0].Arguments.Should().Be("enhancement, good-first-issue");
        commands[0].LineNumber.Should().Be(3);

        commands[1].Name.Should().Be("assign");
        commands[1].Arguments.Should().Be("@reviewer1");
        commands[1].LineNumber.Should().Be(4);

        commands[2].Name.Should().Be("cc");
        commands[2].Arguments.Should().Be("@team-lead");
        commands[2].LineNumber.Should().Be(7);
    }

    [Fact]
    public void IsSlashCommand_WithValidCommand_ShouldReturnTrue()
    {
        // Arrange
        var line = "/label bug";

        // Act
        var result = SlashCommandParser.IsSlashCommand(line);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSlashCommand_WithLeadingWhitespace_ShouldReturnTrue()
    {
        // Arrange
        var line = "  /label bug";

        // Act
        var result = SlashCommandParser.IsSlashCommand(line);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSlashCommand_WithoutSlash_ShouldReturnFalse()
    {
        // Arrange
        var line = "This is not a command";

        // Act
        var result = SlashCommandParser.IsSlashCommand(line);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSlashCommand_WithSlashInMiddle_ShouldReturnFalse()
    {
        // Arrange
        var line = "This is /not a command";

        // Act
        var result = SlashCommandParser.IsSlashCommand(line);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSlashCommand_WithOnlySlash_ShouldReturnFalse()
    {
        // Arrange
        var line = "/";

        // Act
        var result = SlashCommandParser.IsSlashCommand(line);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSlashCommand_WithEmptyString_ShouldReturnFalse()
    {
        // Arrange
        var line = string.Empty;

        // Act
        var result = SlashCommandParser.IsSlashCommand(line);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSlashCommand_WithNullString_ShouldReturnFalse()
    {
        // Arrange
        string? line = null;

        // Act
        var result = SlashCommandParser.IsSlashCommand(line!);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("/label")]
    [InlineData("/assign")]
    [InlineData("/close")]
    [InlineData("/reopen")]
    [InlineData("/lock")]
    [InlineData("/unlock")]
    public void IsSlashCommand_WithCommonCommands_ShouldReturnTrue(string command)
    {
        // Act
        var result = SlashCommandParser.IsSlashCommand(command);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("/do-something")]
    [InlineData("/do_something")]
    [InlineData("/cmd123")]
    [InlineData("/a")]
    [InlineData("/Z")]
    public void IsSlashCommand_WithVariousValidNames_ShouldReturnTrue(string command)
    {
        // Act
        var result = SlashCommandParser.IsSlashCommand(command);

        // Assert
        result.Should().BeTrue();
    }
}
