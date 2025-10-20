// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using ProbotSharp.Infrastructure.Configuration;
using ProbotSharp.Infrastructure.Extensions;

namespace ProbotSharp.Infrastructure.Tests.Extensions;

public sealed class AdapterServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInfrastructureAdapters_BindsAndValidatesConfiguration()
    {
        var json = """
        {
          "ProbotSharp:Adapters": {
            "Cache": { "Provider": "Memory" },
            "Idempotency": { "Provider": "Database" },
            "Persistence": { "Provider": "InMemory" },
            "ReplayQueue": { "Provider": "InMemory" },
            "Metrics": { "Provider": "NoOp" },
            "Tracing": { "Provider": "NoOp" }
          }
        }
        """;

        var config = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructureAdapters(config);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<AdapterConfiguration>>();
        options.Value.Cache.Provider.Should().Be(CacheProvider.Memory);
        options.Value.Idempotency.Provider.Should().Be(IdempotencyProvider.Database);
        options.Value.Persistence.Provider.Should().Be(PersistenceProvider.InMemory);
    }
}
