// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Linq;

using NSubstitute;

using Octokit;

using ProbotSharp.Application.Configuration;
using ProbotSharp.Application.Extensions;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Abstractions;
using ProbotSharp.Domain.Context;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Tests.Configuration;

public class RepositoryConfigurationContextConfiguratorTests
{
    [Fact]
    public void Constructor_WithValidService_ShouldNotThrow()
    {
        // Arrange
        var contentPort = Substitute.For<IRepositoryContentPort>();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = Substitute.For<ILogger<RepositoryConfigurationService>>();
        var configService = new RepositoryConfigurationService(contentPort, cache, logger);

        // Act
        var act = () => new RepositoryConfigurationContextConfigurator(configService);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Configure_ShouldSetConfigurationServiceOnContext()
    {
        // Arrange
        var contentPort = Substitute.For<IRepositoryContentPort>();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = Substitute.For<ILogger<RepositoryConfigurationService>>();
        var configService = new RepositoryConfigurationService(contentPort, cache, logger);
        var configurator = new RepositoryConfigurationContextConfigurator(configService);

        var context = CreateContext();

        // Act
        configurator.Configure(context);

        // Assert - verify service was set by trying to use it
        var result = context.GetConfigAsync<TestConfig>();
        result.Should().NotBeNull();
    }

    private static ProbotSharpContext CreateContext()
    {
        var payload = JObject.Parse(@"{
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

    private class TestConfig
    {
        public string Value { get; set; } = string.Empty;
    }
}
