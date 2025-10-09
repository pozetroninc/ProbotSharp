// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Context;
using ProbotSharp.Domain.Contracts;

namespace ProbotSharp.Domain.Tests.Context;

public class RepositoryInfoTests
{
    [Fact]
    public void Constructor_WithValidArguments_ShouldSetProperties()
    {
        // Arrange & Act
        var repositoryInfo = new RepositoryInfo(123, "test-repo", "test-owner", "test-owner/test-repo");

        // Assert
        repositoryInfo.Id.Should().Be(123);
        repositoryInfo.Name.Should().Be("test-repo");
        repositoryInfo.Owner.Should().Be("test-owner");
        repositoryInfo.FullName.Should().Be("test-owner/test-repo");
    }

    [Fact]
    public void Constructor_WithInvalidId_ShouldThrow()
    {
        // Act
        var act = () => new RepositoryInfo(0, "test-repo", "test-owner", "test-owner/test-repo");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithNegativeId_ShouldThrow()
    {
        // Act
        var act = () => new RepositoryInfo(-1, "test-repo", "test-owner", "test-owner/test-repo");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithNullName_ShouldThrow()
    {
        // Act
        var act = () => new RepositoryInfo(123, null!, "test-owner", "test-owner/test-repo");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyOwner_ShouldThrow()
    {
        // Act
        var act = () => new RepositoryInfo(123, "test-repo", string.Empty, "test-owner/test-repo");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithWhitespaceFullName_ShouldThrow()
    {
        // Act
        var act = () => new RepositoryInfo(123, "test-repo", "test-owner", "   ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
