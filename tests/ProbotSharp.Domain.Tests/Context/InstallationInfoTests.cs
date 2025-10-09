// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Context;
using ProbotSharp.Domain.Contracts;

namespace ProbotSharp.Domain.Tests.Context;

public class InstallationInfoTests
{
    [Fact]
    public void Constructor_WithValidArguments_ShouldSetProperties()
    {
        // Arrange & Act
        var installationInfo = new InstallationInfo(456, "test-account");

        // Assert
        installationInfo.Id.Should().Be(456);
        installationInfo.AccountLogin.Should().Be("test-account");
    }

    [Fact]
    public void Constructor_WithInvalidId_ShouldThrow()
    {
        // Act
        var act = () => new InstallationInfo(0, "test-account");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithNegativeId_ShouldThrow()
    {
        // Act
        var act = () => new InstallationInfo(-1, "test-account");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithNullAccountLogin_ShouldThrow()
    {
        // Act
        var act = () => new InstallationInfo(456, null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyAccountLogin_ShouldThrow()
    {
        // Act
        var act = () => new InstallationInfo(456, string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithWhitespaceAccountLogin_ShouldThrow()
    {
        // Act
        var act = () => new InstallationInfo(456, "   ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
