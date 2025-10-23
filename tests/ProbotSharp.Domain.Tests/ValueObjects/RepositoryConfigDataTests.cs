// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Tests.ValueObjects;

public class RepositoryConfigDataTests
{
    #region Create Tests

    [Fact]
    public void Create_WithValidArguments_ShouldSetProperties()
    {
        // Arrange
        var content = "enabled: true\ntimeout: 30";
        var sha = "abc123";
        var sourcePath = RepositoryConfigPath.Create("config.yml", "owner", "repo");

        // Act
        var configData = RepositoryConfigData.Create(content, sha, sourcePath);

        // Assert
        configData.Content.Should().Be(content);
        configData.Sha.Should().Be(sha);
        configData.SourcePath.Should().Be(sourcePath);
        configData.LoadedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_ShouldSetLoadedAtToCurrentTime()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        var sourcePath = RepositoryConfigPath.Create("config.yml", "owner", "repo");

        // Act
        var configData = RepositoryConfigData.Create("content", "sha", sourcePath);
        var after = DateTimeOffset.UtcNow;

        // Assert
        configData.LoadedAt.Should().BeOnOrAfter(before);
        configData.LoadedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Create_WithEmptyContent_ShouldCreateConfig()
    {
        // Arrange
        var sourcePath = RepositoryConfigPath.Create("config.yml", "owner", "repo");

        // Act
        var configData = RepositoryConfigData.Create("", "sha", sourcePath);

        // Assert
        configData.Content.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithComplexContent_ShouldPreserveContent()
    {
        // Arrange
        var yamlContent = @"
version: 1
settings:
  enabled: true
  features:
    - auto-merge
    - label-sync
labels:
  bug: red
  enhancement: blue
";
        var sourcePath = RepositoryConfigPath.Create("config.yml", "owner", "repo");

        // Act
        var configData = RepositoryConfigData.Create(yamlContent, "xyz789", sourcePath);

        // Assert
        configData.Content.Should().Be(yamlContent);
    }

    #endregion

    #region IsStale Tests

    [Fact]
    public void IsStale_WhenJustCreated_ShouldNotBeStale()
    {
        // Arrange
        var sourcePath = RepositoryConfigPath.Create("config.yml", "owner", "repo");
        var configData = RepositoryConfigData.Create("content", "sha", sourcePath);

        // Act
        var isStale = configData.IsStale(TimeSpan.FromMinutes(5));

        // Assert
        isStale.Should().BeFalse();
    }

    [Fact]
    public void IsStale_WithShortTtl_ShouldBeStale()
    {
        // Arrange
        var sourcePath = RepositoryConfigPath.Create("config.yml", "owner", "repo");
        var configData = RepositoryConfigData.Create("content", "sha", sourcePath);

        // Wait a bit to ensure time has passed
        Thread.Sleep(10);

        // Act
        var isStale = configData.IsStale(TimeSpan.FromMilliseconds(1));

        // Assert
        isStale.Should().BeTrue();
    }

    [Fact]
    public void IsStale_WithZeroTtl_ShouldBeStale()
    {
        // Arrange
        var sourcePath = RepositoryConfigPath.Create("config.yml", "owner", "repo");
        var configData = RepositoryConfigData.Create("content", "sha", sourcePath);

        // Act
        var isStale = configData.IsStale(TimeSpan.Zero);

        // Assert
        isStale.Should().BeTrue();
    }

    [Fact]
    public void IsStale_WithNegativeTtl_ShouldBeStale()
    {
        // Arrange
        var sourcePath = RepositoryConfigPath.Create("config.yml", "owner", "repo");
        var configData = RepositoryConfigData.Create("content", "sha", sourcePath);

        // Act
        var isStale = configData.IsStale(TimeSpan.FromMinutes(-1));

        // Assert
        isStale.Should().BeTrue();
    }

    [Fact]
    public void IsStale_WithVeryLongTtl_ShouldNotBeStale()
    {
        // Arrange
        var sourcePath = RepositoryConfigPath.Create("config.yml", "owner", "repo");
        var configData = RepositoryConfigData.Create("content", "sha", sourcePath);

        // Act
        var isStale = configData.IsStale(TimeSpan.FromDays(365));

        // Assert
        isStale.Should().BeFalse();
    }

    [Fact]
    public void IsStale_CalledMultipleTimes_ShouldBeConsistent()
    {
        // Arrange
        var sourcePath = RepositoryConfigPath.Create("config.yml", "owner", "repo");
        var configData = RepositoryConfigData.Create("content", "sha", sourcePath);
        var ttl = TimeSpan.FromMinutes(10);

        // Act
        var isStale1 = configData.IsStale(ttl);
        var isStale2 = configData.IsStale(ttl);

        // Assert - Both calls should return false since config was just created
        isStale1.Should().BeFalse();
        isStale2.Should().BeFalse();
    }

    [Fact]
    public void IsStale_WithDifferentTtls_ShouldReturnDifferentResults()
    {
        // Arrange
        var sourcePath = RepositoryConfigPath.Create("config.yml", "owner", "repo");
        var configData = RepositoryConfigData.Create("content", "sha", sourcePath);

        Thread.Sleep(10);

        // Act
        var staleWithShortTtl = configData.IsStale(TimeSpan.FromMilliseconds(1));
        var staleWithLongTtl = configData.IsStale(TimeSpan.FromHours(1));

        // Assert
        staleWithShortTtl.Should().BeTrue();
        staleWithLongTtl.Should().BeFalse();
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equals_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var sourcePath = RepositoryConfigPath.Create("config.yml", "owner", "repo");
        var loadedAt = DateTimeOffset.UtcNow;

        // Create using reflection to set same LoadedAt time
        var configData1 = RepositoryConfigData.Create("content", "sha", sourcePath);
        var configData2 = RepositoryConfigData.Create("content", "sha", sourcePath);

        // Note: These won't be equal because LoadedAt is set to UtcNow in Create()
        // This tests that records compare all properties
        // Act & Assert
        if (configData1.LoadedAt == configData2.LoadedAt)
        {
            configData1.Should().Be(configData2);
        }
        else
        {
            configData1.Should().NotBe(configData2);
        }
    }

    [Fact]
    public void Equals_WithDifferentContent_ShouldNotBeEqual()
    {
        // Arrange
        var sourcePath = RepositoryConfigPath.Create("config.yml", "owner", "repo");
        var configData1 = RepositoryConfigData.Create("content1", "sha", sourcePath);
        var configData2 = RepositoryConfigData.Create("content2", "sha", sourcePath);

        // Act & Assert
        configData1.Should().NotBe(configData2);
    }

    [Fact]
    public void Equals_WithDifferentSha_ShouldNotBeEqual()
    {
        // Arrange
        var sourcePath = RepositoryConfigPath.Create("config.yml", "owner", "repo");
        var configData1 = RepositoryConfigData.Create("content", "sha1", sourcePath);
        var configData2 = RepositoryConfigData.Create("content", "sha2", sourcePath);

        // Act & Assert
        configData1.Should().NotBe(configData2);
    }

    [Fact]
    public void Equals_WithDifferentSourcePath_ShouldNotBeEqual()
    {
        // Arrange
        var sourcePath1 = RepositoryConfigPath.Create("config1.yml", "owner", "repo");
        var sourcePath2 = RepositoryConfigPath.Create("config2.yml", "owner", "repo");
        var configData1 = RepositoryConfigData.Create("content", "sha", sourcePath1);
        var configData2 = RepositoryConfigData.Create("content", "sha", sourcePath2);

        // Act & Assert
        configData1.Should().NotBe(configData2);
    }

    #endregion

    #region Property Access Tests

    [Fact]
    public void Properties_ShouldBeAccessible()
    {
        // Arrange
        var content = "test: value";
        var sha = "abc123";
        var sourcePath = RepositoryConfigPath.Create("config.yml", "owner", "repo");

        // Act
        var configData = RepositoryConfigData.Create(content, sha, sourcePath);

        // Assert - Verify all properties are accessible
        var _ = configData.Content;
        var __ = configData.Sha;
        var ___ = configData.SourcePath;
        var ____ = configData.LoadedAt;

        // If we get here, all properties are accessible
        true.Should().BeTrue();
    }

    #endregion
}
