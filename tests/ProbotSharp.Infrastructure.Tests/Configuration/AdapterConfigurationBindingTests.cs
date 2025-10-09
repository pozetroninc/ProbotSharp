// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using ProbotSharp.Infrastructure.Configuration;

namespace ProbotSharp.Infrastructure.Tests.Configuration;

public sealed class AdapterConfigurationBindingTests
{
    [Fact]
    public void Bind_ShouldMapOptions_AndValidateDefaults()
    {
        var json = """
        {
          "ProbotSharp:Adapters": {
            "Cache": { "Provider": "Redis", "Options": { "ConnectionString": "localhost:6379", "InstanceName": "PS:" } },
            "Idempotency": { "Provider": "Database" },
            "Persistence": { "Provider": "InMemory" },
            "ReplayQueue": { "Provider": "FileSystem", "Options": { "Path": "/tmp/replay" } },
            "Metrics": { "Provider": "OpenTelemetry", "Options": { "MeterName": "PS", "Version": "1.0" } },
            "Tracing": { "Provider": "NoOp" }
          }
        }
        """;

        var config = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            .Build();

        var options = new AdapterConfiguration();
        config.GetSection(AdapterConfiguration.SectionName).Bind(options);
        options.Validate();

        options.Cache.Provider.Should().Be(CacheProvider.Redis);
        options.Cache.GetRedisConnectionString().Should().Be("localhost:6379");
        options.Cache.GetRedisInstanceName().Should().Be("PS:");

        options.ReplayQueue.Provider.Should().Be(ReplayQueueProvider.FileSystem);
        options.ReplayQueue.GetPath().Should().Be("/tmp/replay");

        options.Metrics.Provider.Should().Be(MetricsProvider.OpenTelemetry);
        options.Metrics.GetMeterName().Should().Be("PS");
        options.Metrics.GetVersion().Should().Be("1.0");
    }
}



