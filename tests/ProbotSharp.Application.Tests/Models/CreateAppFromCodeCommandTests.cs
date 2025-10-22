// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;

namespace ProbotSharp.Application.Tests.Models;

public class CreateAppFromCodeCommandTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidCode_ShouldCreateInstance()
    {
        // Act
        var command = new CreateAppFromCodeCommand("valid-code");

        // Assert
        command.Code.Should().Be("valid-code");
        command.GitHubEnterpriseHost.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithValidCodeAndHost_ShouldSetBothProperties()
    {
        // Act
        var command = new CreateAppFromCodeCommand("valid-code", "github.enterprise.com");

        // Assert
        command.Code.Should().Be("valid-code");
        command.GitHubEnterpriseHost.Should().Be("github.enterprise.com");
    }

    [Fact]
    public void Constructor_WithNullCode_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new CreateAppFromCodeCommand(null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("Code")
            .WithMessage("Code cannot be null or whitespace.*");
    }

    [Fact]
    public void Constructor_WithEmptyCode_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new CreateAppFromCodeCommand(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("Code")
            .WithMessage("Code cannot be null or whitespace.*");
    }

    [Fact]
    public void Constructor_WithWhitespaceCode_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new CreateAppFromCodeCommand("   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("Code")
            .WithMessage("Code cannot be null or whitespace.*");
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Code_Property_ShouldReturnConstructorValue()
    {
        // Arrange
        var expectedCode = "test-auth-code-123";

        // Act
        var command = new CreateAppFromCodeCommand(expectedCode);

        // Assert
        command.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void GitHubEnterpriseHost_WithNullValue_ShouldRemainNull()
    {
        // Act
        var command = new CreateAppFromCodeCommand("code", null);

        // Assert
        command.GitHubEnterpriseHost.Should().BeNull();
    }

    [Fact]
    public void GitHubEnterpriseHost_WithEmptyString_ShouldAccept()
    {
        // Act
        var command = new CreateAppFromCodeCommand("code", string.Empty);

        // Assert
        command.GitHubEnterpriseHost.Should().BeEmpty();
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equals_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var command1 = new CreateAppFromCodeCommand("code-123");
        var command2 = new CreateAppFromCodeCommand("code-123");

        // Act & Assert
        command1.Should().Be(command2);
    }

    [Fact]
    public void Equals_WithDifferentCode_ShouldNotBeEqual()
    {
        // Arrange
        var command1 = new CreateAppFromCodeCommand("code-123");
        var command2 = new CreateAppFromCodeCommand("code-456");

        // Act & Assert
        command1.Should().NotBe(command2);
    }

    [Fact]
    public void Equals_WithDifferentHost_ShouldNotBeEqual()
    {
        // Arrange
        var command1 = new CreateAppFromCodeCommand("code", "host1.com");
        var command2 = new CreateAppFromCodeCommand("code", "host2.com");

        // Act & Assert
        command1.Should().NotBe(command2);
    }

    [Fact]
    public void Equals_WithNullVsNotNullHost_ShouldNotBeEqual()
    {
        // Arrange
        var command1 = new CreateAppFromCodeCommand("code", null);
        var command2 = new CreateAppFromCodeCommand("code", "host.com");

        // Act & Assert
        command1.Should().NotBe(command2);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithCodeContainingSpecialCharacters_ShouldAccept()
    {
        // Act
        var command = new CreateAppFromCodeCommand("code-with-special!@#$%");

        // Assert
        command.Code.Should().Be("code-with-special!@#$%");
    }

    [Fact]
    public void Constructor_WithVeryLongCode_ShouldAccept()
    {
        // Arrange
        var longCode = new string('A', 1000);

        // Act
        var command = new CreateAppFromCodeCommand(longCode);

        // Assert
        command.Code.Should().Be(longCode);
    }

    [Fact]
    public void Constructor_WithHostUrl_ShouldAccept()
    {
        // Act
        var command = new CreateAppFromCodeCommand("code", "https://github.enterprise.com");

        // Assert
        command.GitHubEnterpriseHost.Should().Be("https://github.enterprise.com");
    }

    #endregion
}
