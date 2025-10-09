// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Infrastructure.Extensions;
using StackExchange.Redis;
using NSubstitute;

namespace ProbotSharp.Infrastructure.Tests.Extensions;

public sealed class InfrastructureServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInfrastructure_WithDefaults_ShouldRegisterCoreServices()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ProbotSharp:Adapters:Cache:Provider"] = "InMemory",
            ["ProbotSharp:Adapters:Idempotency:Provider"] = "Database",
            ["ProbotSharp:Adapters:Metrics:Provider"] = "NoOp",
            ["ProbotSharp:Adapters:Tracing:Provider"] = "NoOp",
            ["ProbotSharp:Adapters:ReplayQueue:Provider"] = "InMemory",
            ["ProbotSharp:Adapters:DeadLetterQueue:Provider"] = "InMemory",
        }).Build();

        services.AddLogging();
        services.AddInfrastructure(config);

        using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IAccessTokenCachePort>();
        sp.GetRequiredService<IIdempotencyPort>();
        sp.GetRequiredService<IWebhookReplayQueuePort>();
        sp.GetRequiredService<IDeadLetterQueuePort>();
        sp.GetRequiredService<IMetricsPort>();
        sp.GetRequiredService<ITracingPort>();
    }

    [Fact]
    public void AddInfrastructure_WithRedisCache_ShouldRegisterRedis()
    {
        // Skip on CI / environments without Redis to avoid external dependency
        if (Environment.GetEnvironmentVariable("CI") == "true")
        {
            return;
        }

        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ProbotSharp:Adapters:Cache:Provider"] = "Redis",
            ["ProbotSharp:Adapters:Cache:Options:ConnectionString"] = "localhost:6379,abortConnect=false",
            ["ProbotSharp:Adapters:Idempotency:Provider"] = "Database",
            ["ProbotSharp:Adapters:Metrics:Provider"] = "NoOp",
            ["ProbotSharp:Adapters:Tracing:Provider"] = "NoOp",
            ["ProbotSharp:Adapters:ReplayQueue:Provider"] = "InMemory",
            ["ProbotSharp:Adapters:DeadLetterQueue:Provider"] = "InMemory",
        }).Build();

        services.AddLogging();
        services.AddInfrastructure(config);

        using var sp = services.BuildServiceProvider();
        // Resolve without throwing; actual connection may fail in some envs, so we only check registration exists
        sp.GetService<IAccessTokenCachePort>().Should().NotBeNull();
    }
}

public sealed class InfrastructureServiceCollectionExtensionsAdditionalTests
{
    [Fact]
    public void AddInfrastructure_WithIdempotencyRedis_ShouldRegister()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ProbotSharp:Adapters:Cache:Provider"] = "InMemory",
            ["ProbotSharp:Adapters:Idempotency:Provider"] = "Redis",
            ["ProbotSharp:Adapters:Idempotency:Options:ConnectionString"] = "localhost:6379,abortConnect=false",
            ["ProbotSharp:Adapters:Metrics:Provider"] = "NoOp",
            ["ProbotSharp:Adapters:Tracing:Provider"] = "NoOp",
            ["ProbotSharp:Adapters:ReplayQueue:Provider"] = "InMemory",
            ["ProbotSharp:Adapters:DeadLetterQueue:Provider"] = "InMemory",
        }).Build();

        services.AddLogging();
        // Provide a stubbed Redis multiplexer so DI can construct the adapter without real Redis
        services.AddSingleton<IConnectionMultiplexer>(_ => Substitute.For<IConnectionMultiplexer>());
        services.AddInfrastructure(config);

        using var sp = services.BuildServiceProvider();
        sp.GetService<IIdempotencyPort>().Should().NotBeNull();
    }

    [Fact]
    public void AddInfrastructure_WithReplayQueueFileSystem_ShouldRegister()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ProbotSharp:Adapters:Cache:Provider"] = "InMemory",
            ["ProbotSharp:Adapters:Idempotency:Provider"] = "Database",
            ["ProbotSharp:Adapters:Metrics:Provider"] = "NoOp",
            ["ProbotSharp:Adapters:Tracing:Provider"] = "NoOp",
            ["ProbotSharp:Adapters:ReplayQueue:Provider"] = "FileSystem",
            ["ProbotSharp:Adapters:ReplayQueue:Options:Path"] = Path.Combine(AppContext.BaseDirectory, "replay-queue-test"),
            ["ProbotSharp:Adapters:DeadLetterQueue:Provider"] = "InMemory",
        }).Build();

        services.AddLogging();
        services.AddInfrastructure(config);

        using var sp = services.BuildServiceProvider();
        sp.GetService<IWebhookReplayQueuePort>().Should().NotBeNull();
    }

    [Fact]
    public void AddInfrastructure_WithDeadLetterQueueFileSystem_ShouldRegister()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ProbotSharp:Adapters:Cache:Provider"] = "InMemory",
            ["ProbotSharp:Adapters:Idempotency:Provider"] = "Database",
            ["ProbotSharp:Adapters:Metrics:Provider"] = "NoOp",
            ["ProbotSharp:Adapters:Tracing:Provider"] = "NoOp",
            ["ProbotSharp:Adapters:ReplayQueue:Provider"] = "InMemory",
            ["ProbotSharp:Adapters:DeadLetterQueue:Provider"] = "FileSystem",
            ["ProbotSharp:Adapters:DeadLetterQueue:Options:Path"] = Path.Combine(AppContext.BaseDirectory, "dead-letter-queue-test"),
        }).Build();

        services.AddLogging();
        services.AddInfrastructure(config);

        using var sp = services.BuildServiceProvider();
        sp.GetService<IDeadLetterQueuePort>().Should().NotBeNull();
    }

    [Theory]
    [InlineData("Cache", "UnknownCache")]
    [InlineData("Idempotency", "UnknownIdem")]
    [InlineData("Metrics", "UnknownMetrics")]
    [InlineData("Tracing", "UnknownTracing")]
    [InlineData("ReplayQueue", "UnknownReplay")]
    [InlineData("DeadLetterQueue", "UnknownDlq")]
    public void AddInfrastructure_WithUnknownProvider_ShouldThrow(string section, string provider)
    {
        var services = new ServiceCollection();
        var dict = new Dictionary<string, string?>
        {
            ["ProbotSharp:Adapters:Cache:Provider"] = "Memory",
            ["ProbotSharp:Adapters:Idempotency:Provider"] = "Database",
            ["ProbotSharp:Adapters:Metrics:Provider"] = "NoOp",
            ["ProbotSharp:Adapters:Tracing:Provider"] = "NoOp",
            ["ProbotSharp:Adapters:ReplayQueue:Provider"] = "InMemory",
            ["ProbotSharp:Adapters:DeadLetterQueue:Provider"] = "InMemory",
        };
        dict[$"ProbotSharp:Adapters:{section}:Provider"] = provider;
        var config = new ConfigurationBuilder().AddInMemoryCollection(dict!).Build();

        services.AddLogging();
        Action act = () => services.AddInfrastructure(config);
        act.Should().Throw<InvalidOperationException>();
    }
}



