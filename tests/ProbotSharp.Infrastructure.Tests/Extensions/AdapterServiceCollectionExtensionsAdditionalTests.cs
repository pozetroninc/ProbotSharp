// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.IO;
using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Infrastructure.Extensions;

namespace ProbotSharp.Infrastructure.Tests.Extensions;

public sealed class AdapterServiceCollectionExtensionsAdditionalTests
{
    private static IConfiguration BuildConfig(string json)
        => new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            .Build();

    [Fact]
    public void AddInfrastructureAdapters_CacheDistributed_ShouldThrowNotImplemented()
    {
        var json = """
        {
          "ProbotSharp:Adapters": {
            "Cache": { "Provider": "Distributed", "Options": { "ConnectionString": "localhost:6379" } },
            "Idempotency": { "Provider": "Database" },
            "Persistence": { "Provider": "InMemory" },
            "ReplayQueue": { "Provider": "InMemory" },
            "Metrics": { "Provider": "NoOp" },
            "Tracing": { "Provider": "NoOp" }
          }
        }
        """;

        var services = new ServiceCollection();
        var config = BuildConfig(json);

        Action act = () => services.AddInfrastructureAdapters(config);
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void AddInfrastructureAdapters_IdempotencyInMemory_ShouldRegister()
    {
        var json = """
        {
          "ProbotSharp:Adapters": {
            "Cache": { "Provider": "Memory" },
            "Idempotency": { "Provider": "Memory" },
            "Persistence": { "Provider": "InMemory" },
            "ReplayQueue": { "Provider": "InMemory" },
            "Metrics": { "Provider": "NoOp" },
            "Tracing": { "Provider": "NoOp" }
          }
        }
        """;

        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfig(json);

        services.AddInfrastructureAdapters(config);

        using var sp = services.BuildServiceProvider();
        var port = sp.GetRequiredService<IIdempotencyPort>();
        port.Should().NotBeNull();
        port.GetType().Name.Should().Be("InMemoryIdempotencyAdapter");
    }

    [Fact]
    public void AddInfrastructureAdapters_ReplayQueueAzure_ShouldThrowNotImplemented()
    {
        var json = """
        {
          "ProbotSharp:Adapters": {
            "Cache": { "Provider": "Memory" },
            "Idempotency": { "Provider": "Database" },
            "Persistence": { "Provider": "InMemory" },
            "ReplayQueue": { "Provider": "AzureQueue", "Options": { "ConnectionString": "UseDevelopmentStorage=true" } },
            "Metrics": { "Provider": "NoOp" },
            "Tracing": { "Provider": "NoOp" }
          }
        }
        """;

        var services = new ServiceCollection();
        var config = BuildConfig(json);

        Action act = () => services.AddInfrastructureAdapters(config);
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void AddInfrastructureAdapters_MetricsPrometheus_ShouldThrowNotImplemented()
    {
        var json = """
        {
          "ProbotSharp:Adapters": {
            "Cache": { "Provider": "Memory" },
            "Idempotency": { "Provider": "Database" },
            "Persistence": { "Provider": "InMemory" },
            "ReplayQueue": { "Provider": "InMemory" },
            "Metrics": { "Provider": "Prometheus" },
            "Tracing": { "Provider": "NoOp" }
          }
        }
        """;

        var services = new ServiceCollection();
        var config = BuildConfig(json);

        Action act = () => services.AddInfrastructureAdapters(config);
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void AddInfrastructureAdapters_TracingOpenTelemetry_ShouldThrowNotImplemented()
    {
        var json = """
        {
          "ProbotSharp:Adapters": {
            "Cache": { "Provider": "Memory" },
            "Idempotency": { "Provider": "Database" },
            "Persistence": { "Provider": "InMemory" },
            "ReplayQueue": { "Provider": "InMemory" },
            "Metrics": { "Provider": "NoOp" },
            "Tracing": { "Provider": "OpenTelemetry" }
          }
        }
        """;

        var services = new ServiceCollection();
        var config = BuildConfig(json);

        Action act = () => services.AddInfrastructureAdapters(config);
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void AddInfrastructureAdapters_ReplayQueueFileSystem_WithoutPath_ShouldThrow()
    {
        var json = """
        {
          "ProbotSharp:Adapters": {
            "Cache": { "Provider": "Memory" },
            "Idempotency": { "Provider": "Database" },
            "Persistence": { "Provider": "InMemory" },
            "ReplayQueue": { "Provider": "FileSystem" },
            "Metrics": { "Provider": "NoOp" },
            "Tracing": { "Provider": "NoOp" }
          }
        }
        """;

        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfig(json);

        Action act = () => services.AddInfrastructureAdapters(config);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddInfrastructureAdapters_ReplayQueueFileSystem_ShouldHonorCustomPath()
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"ps-replay-{Guid.NewGuid():N}");
        var json = "{\n  \"ProbotSharp:Adapters\": {\n    \"Cache\": { \"Provider\": \"Memory\" },\n    \"Idempotency\": { \"Provider\": \"Database\" },\n    \"Persistence\": { \"Provider\": \"InMemory\" },\n    \"ReplayQueue\": { \"Provider\": \"FileSystem\", \"Options\": { \"Path\": \"" + tmp.Replace("\\", "\\\\") + "\" } },\n    \"Metrics\": { \"Provider\": \"NoOp\" },\n    \"Tracing\": { \"Provider\": \"NoOp\" }\n  }\n}\n";

        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfig(json);
        services.AddInfrastructureAdapters(config);

        using var sp = services.BuildServiceProvider();
        var port = sp.GetRequiredService<IWebhookReplayQueuePort>();
        port.Should().NotBeNull();

        // Ensure type is filesystem adapter (path used inside)
        port.GetType().Name.Should().Be("FileSystemWebhookReplayQueueAdapter");
    }
}
