// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Octokit;
using ProbotSharp.Application.Extensions;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Context;
using ProbotSharp.Domain.Models;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Tests.Extensions;

public class ProbotSharpContextConfigExtensionsTests
{
    [Fact]
    public async Task GetConfigAsync_WithNoServiceAttached_ShouldReturnDefault()
    {
        // Arrange
        var context = CreateContext();
        var defaultConfig = new TestSettings { Name = "default" };

        // Act
        var result = await context.GetConfigAsync("config.yml", defaultConfig);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("default", result.Name);
    }

    [Fact]
    public async Task GetConfigAsync_WithServiceAttached_ShouldUseService()
    {
        // Arrange
        var context = CreateContext();
        var contentPort = Substitute.For<ProbotSharp.Application.Ports.Outbound.IRepositoryContentPort>();
        var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(
            new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var logger = Substitute.For<ILogger<RepositoryConfigurationService>>();

        var yamlContent = "name: from-service";
        var configData = RepositoryConfigData.Create(
            yamlContent,
            "abc123",
            RepositoryConfigPath.ForRoot("config.yml", "test-owner", "test-repo"));

        contentPort.GetFileContentAsync(Arg.Any<RepositoryConfigPath>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result<RepositoryConfigData>.Success(configData));

        var configService = new RepositoryConfigurationService(contentPort, cache, logger);
        context.SetConfigurationService(configService);

        // Act
        var result = await context.GetConfigAsync<TestSettings>("config.yml");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("from-service", result.Name);
    }

    [Fact]
    public async Task GetConfigAsync_WithMissingRepository_ShouldReturnDefault()
    {
        // Arrange
        var payload = JObject.Parse("{}"); // No repository
        var context = CreateContext(payload);
        var defaultConfig = new TestSettings { Name = "default" };

        // Act (no service attached, should return default)
        var result = await context.GetConfigAsync("config.yml", defaultConfig);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("default", result.Name);
    }

    [Fact]
    public async Task GetConfigAsync_WithMissingInstallation_ShouldReturnDefault()
    {
        // Arrange
        var payload = JObject.Parse(@"{
            ""repository"": {
                ""name"": ""test-repo"",
                ""owner"": { ""login"": ""test-owner"" }
            }
        }");
        var context = CreateContext(payload);
        var contentPort = Substitute.For<ProbotSharp.Application.Ports.Outbound.IRepositoryContentPort>();
        var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(
            new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var logger = Substitute.For<ILogger<RepositoryConfigurationService>>();

        var configService = new RepositoryConfigurationService(contentPort, cache, logger);
        context.SetConfigurationService(configService);

        var defaultConfig = new TestSettings { Name = "default" };

        // Act (missing installation, should return default)
        var result = await context.GetConfigAsync("config.yml", defaultConfig);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("default", result.Name);
    }

    private static ProbotSharpContext CreateContext(JObject? payload = null)
    {
        payload ??= JObject.Parse(@"{
            ""repository"": {
                ""name"": ""test-repo"",
                ""owner"": { ""login"": ""test-owner"" }
            },
            ""installation"": { ""id"": 123 }
        }");

        return new ProbotSharpContext(
            id: "test-delivery-id",
            eventName: "issues",
            eventAction: "opened",
            payload: payload,
            logger: Substitute.For<ILogger>(),
            gitHub: Substitute.For<IGitHubClient>(),
            graphQL: Substitute.For<ProbotSharp.Domain.Contracts.IGitHubGraphQlClient>(),
            repository: null,
            installation: null,
            isDryRun: false);
    }
}

public class TestSettings
{
    public string Name { get; set; } = string.Empty;
}
