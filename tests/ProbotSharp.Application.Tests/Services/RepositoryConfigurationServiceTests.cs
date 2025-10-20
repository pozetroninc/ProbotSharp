// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using NSubstitute;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Models;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Tests.Services;

public class RepositoryConfigurationServiceTests : IDisposable
{
    private readonly IRepositoryContentPort _contentPort;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RepositoryConfigurationService> _logger;
    private readonly RepositoryConfigurationService _service;
    private bool _disposed;

    public RepositoryConfigurationServiceTests()
    {
        _contentPort = Substitute.For<IRepositoryContentPort>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _logger = Substitute.For<ILogger<RepositoryConfigurationService>>();
        _service = new RepositoryConfigurationService(_contentPort, _cache, _logger);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                (_cache as IDisposable)?.Dispose();
            }

            _disposed = true;
        }
    }

    [Fact]
    public async Task GetConfigAsync_WhenFileNotFound_ShouldReturnDefault()
    {
        // Arrange
        var defaultConfig = new TestConfig { Value = "default" };
        _contentPort.GetFileContentAsync(Arg.Any<RepositoryConfigPath>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result<RepositoryConfigData>.Failure("NotFound", "File not found"));

        // Act
        var result = await _service.GetConfigAsync(
            "owner",
            "repo",
            123L,
            "config.yml",
            defaultConfig);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("default", result.Value.Value);
    }

    [Fact]
    public async Task GetConfigAsync_WhenFileExists_ShouldLoadAndDeserialize()
    {
        // Arrange
        var yamlContent = "value: from-yaml";
        var configData = RepositoryConfigData.Create(
            yamlContent,
            "abc123",
            RepositoryConfigPath.ForRoot("config.yml", "owner", "repo"));

        _contentPort.GetFileContentAsync(Arg.Any<RepositoryConfigPath>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result<RepositoryConfigData>.Success(configData));

        // Act
        var result = await _service.GetConfigAsync<TestConfig>(
            "owner",
            "repo",
            123L,
            "config.yml");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("from-yaml", result.Value.Value);
    }

    [Fact]
    public async Task GetConfigAsync_WhenCascadeEnabled_ShouldTryMultipleLocations()
    {
        // Arrange
        var options = new RepositoryConfigurationOptions
        {
            EnableGitHubDirectoryCascade = true,
            EnableOrganizationConfig = true
        };

        var githubDirYaml = "value: from-github-dir";
        var githubDirData = RepositoryConfigData.Create(
            githubDirYaml,
            "def456",
            RepositoryConfigPath.ForGitHubDirectory("config.yml", "owner", "repo"));

        // Root fails, .github succeeds
        _contentPort.GetFileContentAsync(
            Arg.Is<RepositoryConfigPath>(p => p.Path == "config.yml"),
            Arg.Any<long>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<RepositoryConfigData>.Failure("NotFound", "Not found"));

        _contentPort.GetFileContentAsync(
            Arg.Is<RepositoryConfigPath>(p => p.Path == ".github/config.yml"),
            Arg.Any<long>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<RepositoryConfigData>.Success(githubDirData));

        // Act
        var result = await _service.GetConfigAsync<TestConfig>(
            "owner",
            "repo",
            123L,
            "config.yml",
            null,
            options);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("from-github-dir", result.Value.Value);
    }

    [Fact]
    public async Task GetConfigAsync_WithExtends_ShouldResolveParentConfig()
    {
        // Arrange
        var parentYaml = "value: from-parent\nextraField: parent-extra";
        var childYaml = "_extends: owner/parent-repo\nvalue: from-child";

        var parentData = RepositoryConfigData.Create(
            parentYaml,
            "parent123",
            RepositoryConfigPath.ForRoot("config.yml", "owner", "parent-repo"));

        var childData = RepositoryConfigData.Create(
            childYaml,
            "child456",
            RepositoryConfigPath.ForRoot("config.yml", "owner", "repo"));

        // Child repo returns config with _extends
        _contentPort.GetFileContentAsync(
            Arg.Is<RepositoryConfigPath>(p => p.Repository == "repo"),
            Arg.Any<long>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<RepositoryConfigData>.Success(childData));

        // Parent repo returns base config
        _contentPort.GetFileContentAsync(
            Arg.Is<RepositoryConfigPath>(p => p.Repository == "parent-repo"),
            Arg.Any<long>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<RepositoryConfigData>.Success(parentData));

        // Act
        var result = await _service.GetConfigAsync<TestConfigWithExtra>(
            "owner",
            "repo",
            123L,
            "config.yml",
            null,
            new RepositoryConfigurationOptions { EnableExtendsKey = true });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("from-child", result.Value.Value); // Child overrides parent
        Assert.Equal("parent-extra", result.Value.ExtraField); // Parent field inherited
    }

    [Fact]
    public async Task GetConfigAsync_WithDefaultConfig_ShouldMergeDefaults()
    {
        // Arrange
        var yamlContent = "value: from-yaml";
        var configData = RepositoryConfigData.Create(
            yamlContent,
            "abc123",
            RepositoryConfigPath.ForRoot("config.yml", "owner", "repo"));

        var defaultConfig = new TestConfigWithExtra
        {
            Value = "default-value",
            ExtraField = "default-extra"
        };

        _contentPort.GetFileContentAsync(Arg.Any<RepositoryConfigPath>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result<RepositoryConfigData>.Success(configData));

        // Act
        var result = await _service.GetConfigAsync(
            "owner",
            "repo",
            123L,
            "config.yml",
            defaultConfig);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("from-yaml", result.Value.Value); // Yaml overrides default
        Assert.Equal("default-extra", result.Value.ExtraField); // Default field preserved
    }

    [Fact]
    public async Task GetConfigAsync_InvalidYaml_ShouldReturnFailure()
    {
        // Arrange
        var invalidYaml = "this is not valid yaml: {]";
        var configData = RepositoryConfigData.Create(
            invalidYaml,
            "abc123",
            RepositoryConfigPath.ForRoot("config.yml", "owner", "repo"));

        _contentPort.GetFileContentAsync(Arg.Any<RepositoryConfigPath>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result<RepositoryConfigData>.Success(configData));

        // Act
        var result = await _service.GetConfigAsync<TestConfig>(
            "owner",
            "repo",
            123L,
            "config.yml");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetConfigAsync_UntypedDictionary_ShouldReturnDictionary()
    {
        // Arrange
        var yamlContent = "key1: value1\nkey2: 123";
        var configData = RepositoryConfigData.Create(
            yamlContent,
            "abc123",
            RepositoryConfigPath.ForRoot("config.yml", "owner", "repo"));

        _contentPort.GetFileContentAsync(Arg.Any<RepositoryConfigPath>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result<RepositoryConfigData>.Success(configData));

        // Act
        var result = await _service.GetConfigAsync(
            "owner",
            "repo",
            123L,
            "config.yml");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("value1", result.Value["key1"].ToString());
        Assert.Equal("123", result.Value["key2"].ToString());
    }

    [Fact]
    public async Task GetConfigAsync_ArrayMergeStrategyReplace_ShouldReplaceArrays()
    {
        // Arrange
        var parentYaml = "items:\n  - parent1\n  - parent2";
        var childYaml = "items:\n  - child1";

        var parentData = RepositoryConfigData.Create(
            parentYaml,
            "parent123",
            RepositoryConfigPath.ForRoot("config.yml", "owner", "parent-repo"));

        var childData = RepositoryConfigData.Create(
            "_extends: owner/parent-repo\n" + childYaml,
            "child456",
            RepositoryConfigPath.ForRoot("config.yml", "owner", "repo"));

        _contentPort.GetFileContentAsync(
            Arg.Is<RepositoryConfigPath>(p => p.Repository == "repo"),
            Arg.Any<long>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<RepositoryConfigData>.Success(childData));

        _contentPort.GetFileContentAsync(
            Arg.Is<RepositoryConfigPath>(p => p.Repository == "parent-repo"),
            Arg.Any<long>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<RepositoryConfigData>.Success(parentData));

        var options = new RepositoryConfigurationOptions
        {
            EnableExtendsKey = true,
            ArrayMergeStrategy = ArrayMergeStrategy.Replace
        };

        // Act
        var result = await _service.GetConfigAsync(
            "owner",
            "repo",
            123L,
            "config.yml",
            (Dictionary<string, object>?)null,
            options);

        // Assert
        Assert.True(result.IsSuccess);
        var items = result.Value!["items"] as List<object>;
        Assert.NotNull(items);
        Assert.Single(items); // Only child items (replaced)
        Assert.Equal("child1", items[0].ToString());
    }

    [Fact]
    public async Task GetConfigAsync_ArrayMergeStrategyConcatenate_ShouldCombineArrays()
    {
        // Arrange
        var parentYaml = "items:\n  - parent1\n  - parent2";
        var childYaml = "_extends: owner/parent-repo\nitems:\n  - child1";

        var parentData = RepositoryConfigData.Create(
            parentYaml,
            "parent123",
            RepositoryConfigPath.ForRoot("config.yml", "owner", "parent-repo"));

        var childData = RepositoryConfigData.Create(
            childYaml,
            "child456",
            RepositoryConfigPath.ForRoot("config.yml", "owner", "repo"));

        _contentPort.GetFileContentAsync(
            Arg.Is<RepositoryConfigPath>(p => p.Repository == "repo"),
            Arg.Any<long>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<RepositoryConfigData>.Success(childData));

        _contentPort.GetFileContentAsync(
            Arg.Is<RepositoryConfigPath>(p => p.Repository == "parent-repo"),
            Arg.Any<long>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<RepositoryConfigData>.Success(parentData));

        var options = new RepositoryConfigurationOptions
        {
            EnableExtendsKey = true,
            ArrayMergeStrategy = ArrayMergeStrategy.Concatenate
        };

        // Act
        var result = await _service.GetConfigAsync(
            "owner",
            "repo",
            123L,
            "config.yml",
            (Dictionary<string, object>?)null,
            options);

        // Assert
        Assert.True(result.IsSuccess);
        var items = result.Value!["items"] as List<object>;
        Assert.NotNull(items);
        Assert.True(items.Count >= 3, $"Expected at least 3 items, got {items.Count}"); // Parent + child items concatenated
        Assert.Contains("parent1", items.Select(i => i.ToString()));
        Assert.Contains("parent2", items.Select(i => i.ToString()));
        Assert.Contains("child1", items.Select(i => i.ToString()));
    }
}

public class TestConfig
{
    public string Value { get; set; } = string.Empty;
}

public class TestConfigWithExtra
{
    public string Value { get; set; } = string.Empty;
    public string ExtraField { get; set; } = string.Empty;
}
