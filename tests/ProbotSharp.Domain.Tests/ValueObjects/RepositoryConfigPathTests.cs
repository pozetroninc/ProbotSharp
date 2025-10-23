// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Tests.ValueObjects;

public class RepositoryConfigPathTests
{
    #region Create Tests

    [Fact]
    public void Create_WithValidArguments_ShouldSetProperties()
    {
        // Act
        var path = RepositoryConfigPath.Create("config.yml", "owner", "repo");

        // Assert
        path.Path.Should().Be("config.yml");
        path.Owner.Should().Be("owner");
        path.Repository.Should().Be("repo");
        path.Ref.Should().BeNull();
    }

    [Fact]
    public void Create_WithRef_ShouldSetRef()
    {
        // Act
        var path = RepositoryConfigPath.Create("config.yml", "owner", "repo", "main");

        // Assert
        path.Ref.Should().Be("main");
    }

    [Fact]
    public void Create_WithLeadingSlash_ShouldNormalizePath()
    {
        // Act
        var path = RepositoryConfigPath.Create("/config.yml", "owner", "repo");

        // Assert
        path.Path.Should().Be("config.yml");
    }

    [Fact]
    public void Create_WithMultipleLeadingSlashes_ShouldNormalizePath()
    {
        // Act
        var path = RepositoryConfigPath.Create("///config.yml", "owner", "repo");

        // Assert
        path.Path.Should().Be("config.yml");
    }

    [Fact]
    public void Create_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Act
        var act = () => RepositoryConfigPath.Create("", "owner", "repo");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Path cannot be empty*")
            .And.ParamName.Should().Be("path");
    }

    [Fact]
    public void Create_WithWhitespacePath_ShouldThrowArgumentException()
    {
        // Act
        var act = () => RepositoryConfigPath.Create("   ", "owner", "repo");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Path cannot be empty*");
    }

    [Fact]
    public void Create_WithEmptyOwner_ShouldThrowArgumentException()
    {
        // Act
        var act = () => RepositoryConfigPath.Create("config.yml", "", "repo");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Owner cannot be empty*")
            .And.ParamName.Should().Be("owner");
    }

    [Fact]
    public void Create_WithWhitespaceOwner_ShouldThrowArgumentException()
    {
        // Act
        var act = () => RepositoryConfigPath.Create("config.yml", "   ", "repo");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Owner cannot be empty*");
    }

    [Fact]
    public void Create_WithEmptyRepository_ShouldThrowArgumentException()
    {
        // Act
        var act = () => RepositoryConfigPath.Create("config.yml", "owner", "");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Repository cannot be empty*")
            .And.ParamName.Should().Be("repository");
    }

    [Fact]
    public void Create_WithWhitespaceRepository_ShouldThrowArgumentException()
    {
        // Act
        var act = () => RepositoryConfigPath.Create("config.yml", "owner", "   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Repository cannot be empty*");
    }

    #endregion

    #region ForRoot Tests

    [Fact]
    public void ForRoot_WithValidArguments_ShouldCreatePathInRoot()
    {
        // Act
        var path = RepositoryConfigPath.ForRoot("mybot.yml", "owner", "repo");

        // Assert
        path.Path.Should().Be("mybot.yml");
        path.Owner.Should().Be("owner");
        path.Repository.Should().Be("repo");
        path.Ref.Should().BeNull();
    }

    [Fact]
    public void ForRoot_WithRef_ShouldSetRef()
    {
        // Act
        var path = RepositoryConfigPath.ForRoot("mybot.yml", "owner", "repo", "develop");

        // Assert
        path.Ref.Should().Be("develop");
    }

    [Fact]
    public void ForRoot_WithLeadingSlash_ShouldNormalizePath()
    {
        // Act
        var path = RepositoryConfigPath.ForRoot("/mybot.yml", "owner", "repo");

        // Assert
        path.Path.Should().Be("mybot.yml");
    }

    #endregion

    #region ForGitHubDirectory Tests

    [Fact]
    public void ForGitHubDirectory_WithValidArguments_ShouldCreatePathInGitHubDirectory()
    {
        // Act
        var path = RepositoryConfigPath.ForGitHubDirectory("bot.yml", "owner", "repo");

        // Assert
        path.Path.Should().Be(".github/bot.yml");
        path.Owner.Should().Be("owner");
        path.Repository.Should().Be("repo");
        path.Ref.Should().BeNull();
    }

    [Fact]
    public void ForGitHubDirectory_WithRef_ShouldSetRef()
    {
        // Act
        var path = RepositoryConfigPath.ForGitHubDirectory("bot.yml", "owner", "repo", "feature-branch");

        // Assert
        path.Path.Should().Be(".github/bot.yml");
        path.Ref.Should().Be("feature-branch");
    }

    [Fact]
    public void ForGitHubDirectory_WithSubdirectory_ShouldCreateNestedPath()
    {
        // Act
        var path = RepositoryConfigPath.ForGitHubDirectory("configs/bot.yml", "owner", "repo");

        // Assert
        path.Path.Should().Be(".github/configs/bot.yml");
    }

    #endregion

    #region ForOrganization Tests

    [Fact]
    public void ForOrganization_WithValidArguments_ShouldCreateOrgConfigPath()
    {
        // Act
        var path = RepositoryConfigPath.ForOrganization("probot.yml", "my-org");

        // Assert
        path.Path.Should().Be(".github/probot.yml");
        path.Owner.Should().Be("my-org");
        path.Repository.Should().Be(".github");
        path.Ref.Should().BeNull();
    }

    [Fact]
    public void ForOrganization_WithRef_ShouldSetRef()
    {
        // Act
        var path = RepositoryConfigPath.ForOrganization("probot.yml", "my-org", "main");

        // Assert
        path.Ref.Should().Be("main");
    }

    #endregion

    #region GetFullPath Tests

    [Fact]
    public void GetFullPath_ShouldReturnFullPath()
    {
        // Arrange
        var path = RepositoryConfigPath.Create("config.yml", "owner", "repo");

        // Act
        var fullPath = path.GetFullPath();

        // Assert
        fullPath.Should().Be("owner/repo/config.yml");
    }

    [Fact]
    public void GetFullPath_WithNestedPath_ShouldReturnFullPath()
    {
        // Arrange
        var path = RepositoryConfigPath.Create(".github/bot.yml", "owner", "repo");

        // Act
        var fullPath = path.GetFullPath();

        // Assert
        fullPath.Should().Be("owner/repo/.github/bot.yml");
    }

    #endregion

    #region GetCacheKey Tests

    [Fact]
    public void GetCacheKey_WithoutShaOrRef_ShouldUseDefaults()
    {
        // Arrange
        var path = RepositoryConfigPath.Create("config.yml", "owner", "repo");

        // Act
        var cacheKey = path.GetCacheKey();

        // Assert
        cacheKey.Should().Be("config:owner:repo:default:latest:config.yml");
    }

    [Fact]
    public void GetCacheKey_WithSha_ShouldIncludeSha()
    {
        // Arrange
        var path = RepositoryConfigPath.Create("config.yml", "owner", "repo");

        // Act
        var cacheKey = path.GetCacheKey("abc123");

        // Assert
        cacheKey.Should().Be("config:owner:repo:default:abc123:config.yml");
    }

    [Fact]
    public void GetCacheKey_WithRef_ShouldIncludeRef()
    {
        // Arrange
        var path = RepositoryConfigPath.Create("config.yml", "owner", "repo", "main");

        // Act
        var cacheKey = path.GetCacheKey();

        // Assert
        cacheKey.Should().Be("config:owner:repo:main:latest:config.yml");
    }

    [Fact]
    public void GetCacheKey_WithShaAndRef_ShouldIncludeBoth()
    {
        // Arrange
        var path = RepositoryConfigPath.Create("config.yml", "owner", "repo", "develop");

        // Act
        var cacheKey = path.GetCacheKey("xyz789");

        // Assert
        cacheKey.Should().Be("config:owner:repo:develop:xyz789:config.yml");
    }

    [Fact]
    public void GetCacheKey_WithNullSha_ShouldUseLatest()
    {
        // Arrange
        var path = RepositoryConfigPath.Create("config.yml", "owner", "repo", "main");

        // Act
        var cacheKey = path.GetCacheKey(null);

        // Assert
        cacheKey.Should().Be("config:owner:repo:main:latest:config.yml");
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldReturnFullPath()
    {
        // Arrange
        var path = RepositoryConfigPath.Create("config.yml", "owner", "repo");

        // Act
        var result = path.ToString();

        // Assert
        result.Should().Be("owner/repo/config.yml");
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equals_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var path1 = RepositoryConfigPath.Create("config.yml", "owner", "repo", "main");
        var path2 = RepositoryConfigPath.Create("config.yml", "owner", "repo", "main");

        // Act & Assert
        path1.Should().Be(path2);
        (path1 == path2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentPath_ShouldNotBeEqual()
    {
        // Arrange
        var path1 = RepositoryConfigPath.Create("config.yml", "owner", "repo");
        var path2 = RepositoryConfigPath.Create("other.yml", "owner", "repo");

        // Act & Assert
        path1.Should().NotBe(path2);
    }

    [Fact]
    public void Equals_WithDifferentOwner_ShouldNotBeEqual()
    {
        // Arrange
        var path1 = RepositoryConfigPath.Create("config.yml", "owner1", "repo");
        var path2 = RepositoryConfigPath.Create("config.yml", "owner2", "repo");

        // Act & Assert
        path1.Should().NotBe(path2);
    }

    [Fact]
    public void Equals_WithDifferentRepository_ShouldNotBeEqual()
    {
        // Arrange
        var path1 = RepositoryConfigPath.Create("config.yml", "owner", "repo1");
        var path2 = RepositoryConfigPath.Create("config.yml", "owner", "repo2");

        // Act & Assert
        path1.Should().NotBe(path2);
    }

    [Fact]
    public void Equals_WithDifferentRef_ShouldNotBeEqual()
    {
        // Arrange
        var path1 = RepositoryConfigPath.Create("config.yml", "owner", "repo", "main");
        var path2 = RepositoryConfigPath.Create("config.yml", "owner", "repo", "develop");

        // Act & Assert
        path1.Should().NotBe(path2);
    }

    [Fact]
    public void Equals_WithNullRefVsSetRef_ShouldNotBeEqual()
    {
        // Arrange
        var path1 = RepositoryConfigPath.Create("config.yml", "owner", "repo");
        var path2 = RepositoryConfigPath.Create("config.yml", "owner", "repo", "main");

        // Act & Assert
        path1.Should().NotBe(path2);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var path1 = RepositoryConfigPath.Create("config.yml", "owner", "repo", "main");
        var path2 = RepositoryConfigPath.Create("config.yml", "owner", "repo", "main");

        // Act & Assert
        path1.GetHashCode().Should().Be(path2.GetHashCode());
    }

    #endregion
}
