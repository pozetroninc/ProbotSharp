// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;

namespace ProbotSharp.Application.Tests.Models;

public class LoadAppFunctionCommandTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidAppPath_ShouldCreateInstance()
    {
        // Act
        var command = new LoadAppFunctionCommand("/path/to/app");

        // Assert
        command.AppPath.Should().Be("/path/to/app");
        command.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithAppPathAndIsDefault_ShouldSetBothProperties()
    {
        // Act
        var command = new LoadAppFunctionCommand("/path/to/app", IsDefault: true);

        // Assert
        command.AppPath.Should().Be("/path/to/app");
        command.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithNullAppPath_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new LoadAppFunctionCommand(null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("AppPath")
            .WithMessage("App path cannot be null or whitespace.*");
    }

    [Fact]
    public void Constructor_WithEmptyAppPath_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new LoadAppFunctionCommand(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("AppPath")
            .WithMessage("App path cannot be null or whitespace.*");
    }

    [Fact]
    public void Constructor_WithWhitespaceAppPath_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new LoadAppFunctionCommand("   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("AppPath")
            .WithMessage("App path cannot be null or whitespace.*");
    }

    #endregion

    #region Property Tests

    [Fact]
    public void AppPath_Property_ShouldReturnConstructorValue()
    {
        // Arrange
        var expectedPath = "/usr/local/apps/my-app";

        // Act
        var command = new LoadAppFunctionCommand(expectedPath);

        // Assert
        command.AppPath.Should().Be(expectedPath);
    }

    [Fact]
    public void IsDefault_WithFalseValue_ShouldRemainFalse()
    {
        // Act
        var command = new LoadAppFunctionCommand("/path", IsDefault: false);

        // Assert
        command.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void IsDefault_WithTrueValue_ShouldBeTrue()
    {
        // Act
        var command = new LoadAppFunctionCommand("/path", IsDefault: true);

        // Assert
        command.IsDefault.Should().BeTrue();
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equals_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var command1 = new LoadAppFunctionCommand("/path/to/app");
        var command2 = new LoadAppFunctionCommand("/path/to/app");

        // Act & Assert
        command1.Should().Be(command2);
    }

    [Fact]
    public void Equals_WithDifferentAppPath_ShouldNotBeEqual()
    {
        // Arrange
        var command1 = new LoadAppFunctionCommand("/path/to/app1");
        var command2 = new LoadAppFunctionCommand("/path/to/app2");

        // Act & Assert
        command1.Should().NotBe(command2);
    }

    [Fact]
    public void Equals_WithDifferentIsDefault_ShouldNotBeEqual()
    {
        // Arrange
        var command1 = new LoadAppFunctionCommand("/path", IsDefault: false);
        var command2 = new LoadAppFunctionCommand("/path", IsDefault: true);

        // Act & Assert
        command1.Should().NotBe(command2);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithRelativePath_ShouldAccept()
    {
        // Act
        var command = new LoadAppFunctionCommand("./apps/my-app");

        // Assert
        command.AppPath.Should().Be("./apps/my-app");
    }

    [Fact]
    public void Constructor_WithAbsolutePath_ShouldAccept()
    {
        // Act
        var command = new LoadAppFunctionCommand("/usr/local/apps/my-app");

        // Assert
        command.AppPath.Should().Be("/usr/local/apps/my-app");
    }

    [Fact]
    public void Constructor_WithWindowsPath_ShouldAccept()
    {
        // Act
        var command = new LoadAppFunctionCommand("C:\\Apps\\MyApp");

        // Assert
        command.AppPath.Should().Be("C:\\Apps\\MyApp");
    }

    [Fact]
    public void Constructor_WithPathContainingSpaces_ShouldAccept()
    {
        // Act
        var command = new LoadAppFunctionCommand("/path/to/my app");

        // Assert
        command.AppPath.Should().Be("/path/to/my app");
    }

    [Fact]
    public void Constructor_WithPathContainingSpecialCharacters_ShouldAccept()
    {
        // Act
        var command = new LoadAppFunctionCommand("/path/to/app-with-special_chars@123");

        // Assert
        command.AppPath.Should().Be("/path/to/app-with-special_chars@123");
    }

    [Fact]
    public void Constructor_WithVeryLongPath_ShouldAccept()
    {
        // Arrange
        var longPath = "/path/" + new string('a', 500);

        // Act
        var command = new LoadAppFunctionCommand(longPath);

        // Assert
        command.AppPath.Should().Be(longPath);
    }

    #endregion
}
