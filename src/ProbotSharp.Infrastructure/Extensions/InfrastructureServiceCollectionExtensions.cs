// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Infrastructure.Adapters.Caching;
using ProbotSharp.Infrastructure.Adapters.Configuration;
using ProbotSharp.Infrastructure.Adapters.GitHub;
using ProbotSharp.Infrastructure.Adapters.Idempotency;
using ProbotSharp.Infrastructure.Adapters.Logging;
using ProbotSharp.Infrastructure.Adapters.Observability;
using ProbotSharp.Infrastructure.Adapters.Persistence;
using ProbotSharp.Infrastructure.Adapters.System;
using ProbotSharp.Infrastructure.Adapters.Workers;
using ProbotSharp.Application.Abstractions;
using ProbotSharp.Infrastructure.Context;

namespace ProbotSharp.Infrastructure.Extensions;

/// <summary>
/// Dependency injection extensions for registering infrastructure layer adapters and services.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers all infrastructure adapters including HTTP clients, caching, configuration, and persistence.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHttpClient("GitHubRest", client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ProbotSharp/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        });

        services.AddHttpClient("GitHubOAuth", client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ProbotSharp/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        });

        services.AddHttpClient("GitHubGraphQL", client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ProbotSharp/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.AddMemoryCache();

        // Register cache adapter based on provider configuration
        var cacheProvider = configuration["ProbotSharp:Adapters:Cache:Provider"] ?? "InMemory";
        if (cacheProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
        {
            var redisConnectionString = configuration["ProbotSharp:Adapters:Cache:Options:ConnectionString"];
            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                throw new InvalidOperationException(
                    "Redis cache provider is selected but 'ProbotSharp:Adapters:Cache:Options:ConnectionString' is not configured.");
            }

            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
            {
                return StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString);
            });
            services.AddSingleton<IAccessTokenCachePort, RedisAccessTokenCacheAdapter>();
        }
        else if (cacheProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAccessTokenCachePort, InMemoryAccessTokenCacheAdapter>();
        }
        else
        {
            throw new InvalidOperationException($"Unknown cache provider: {cacheProvider}. Valid options are: InMemory, Redis");
        }

        // Register idempotency adapter based on provider configuration
        var idempotencyProvider = configuration["ProbotSharp:Adapters:Idempotency:Provider"] ?? "Database";
        if (idempotencyProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IIdempotencyPort, InMemoryIdempotencyAdapter>();
        }
        else if (idempotencyProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
        {
            var redisConnectionString = configuration["ProbotSharp:Adapters:Idempotency:Options:ConnectionString"];
            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                throw new InvalidOperationException(
                    "Redis idempotency provider is selected but 'ProbotSharp:Adapters:Idempotency:Options:ConnectionString' is not configured.");
            }

            services.AddSingleton<IIdempotencyPort, RedisIdempotencyAdapter>();
        }
        else if (idempotencyProvider.Equals("Database", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IIdempotencyPort, DbIdempotencyAdapter>();
        }
        else
        {
            throw new InvalidOperationException($"Unknown idempotency provider: {idempotencyProvider}. Valid options are: InMemory, Database, Redis");
        }

        services.AddSingleton<IClockPort, SystemClock>();
        services.AddSingleton<IAppConfigurationPort, ConfigurationAppConfigurationAdapter>();
        services.AddSingleton<ILoggingPort>(sp =>
        {
            var serilogLogger = Serilog.Log.Logger;
            return new SerilogLoggingAdapter(serilogLogger);
        });

        // Register metrics adapter based on provider configuration
        var metricsProvider = configuration["ProbotSharp:Adapters:Metrics:Provider"] ?? "NoOp";
        if (metricsProvider.Equals("OpenTelemetry", StringComparison.OrdinalIgnoreCase))
        {
            var meterName = configuration["ProbotSharp:Adapters:Metrics:Options:MeterName"] ?? "ProbotSharp";
            var version = configuration["ProbotSharp:Adapters:Metrics:Options:Version"];
            services.AddSingleton<IMetricsPort>(sp => new OpenTelemetryMetricsAdapter(meterName, version));
        }
        else if (metricsProvider.Equals("NoOp", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IMetricsPort, NoOpMetricsAdapter>();
        }
        else
        {
            throw new InvalidOperationException($"Unknown metrics provider: {metricsProvider}. Valid options are: NoOp, OpenTelemetry");
        }

        // Register tracing adapter based on provider configuration
        var tracingProvider = configuration["ProbotSharp:Adapters:Tracing:Provider"] ?? "NoOp";
        if (tracingProvider.Equals("ActivitySource", StringComparison.OrdinalIgnoreCase) ||
            tracingProvider.Equals("OpenTelemetry", StringComparison.OrdinalIgnoreCase))
        {
            var sourceName = configuration["ProbotSharp:Adapters:Tracing:Options:SourceName"] ?? "ProbotSharp";
            var version = configuration["ProbotSharp:Adapters:Tracing:Options:Version"];
            services.AddSingleton<ITracingPort>(sp => new ActivitySourceTracingAdapter(sourceName, version));
        }
        else if (tracingProvider.Equals("NoOp", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ITracingPort, NoOpTracingAdapter>();
        }
        else
        {
            throw new InvalidOperationException($"Unknown tracing provider: {tracingProvider}. Valid options are: NoOp, ActivitySource, OpenTelemetry");
        }

        services.AddSingleton<IManifestPersistencePort, FileManifestPersistenceAdapter>();
        services.AddSingleton<IEnvironmentConfigurationPort, EnvironmentConfigurationAdapter>();
        services.AddSingleton<IGitHubAppManifestPort, StubGitHubAppManifestAdapter>();
        services.AddSingleton<IWebhookChannelPort, LocalWebhookChannelAdapter>();
        services.AddTransient<IGitHubOAuthPort, GitHubOAuthClient>();
        services.AddTransient<IGitHubRestClientPort, GitHubRestHttpAdapter>();
        services.AddTransient<IGitHubGraphQlClientPort, GitHubGraphQlClientAdapter>();
        services.AddTransient<IRepositoryContentPort, GitHubRepositoryContentAdapter>();

        // Register replay queue adapter based on provider
        var replayQueueProvider = configuration["ProbotSharp:Adapters:ReplayQueue:Provider"] ?? "InMemory";
        if (replayQueueProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IWebhookReplayQueuePort, InMemoryWebhookReplayQueueAdapter>();
        }
        else if (replayQueueProvider.Equals("FileSystem", StringComparison.OrdinalIgnoreCase))
        {
            var queuePath = configuration["ProbotSharp:Adapters:ReplayQueue:Options:Path"]
                ?? Path.Combine(AppContext.BaseDirectory, "replay-queue");
            services.AddSingleton<IWebhookReplayQueuePort>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<FileSystemWebhookReplayQueueAdapter>>();
                return new FileSystemWebhookReplayQueueAdapter(queuePath, logger);
            });
        }
        else
        {
            throw new InvalidOperationException(
                $"Unknown ReplayQueue provider '{replayQueueProvider}'. Valid providers: InMemory, FileSystem");
        }

        // Register dead-letter queue adapter based on provider
        var dlqProvider = configuration["ProbotSharp:Adapters:DeadLetterQueue:Provider"] ?? "InMemory";
        if (dlqProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IDeadLetterQueuePort, InMemoryDeadLetterQueueAdapter>();
        }
        else if (dlqProvider.Equals("Database", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IDeadLetterQueuePort, DatabaseDeadLetterQueueAdapter>();
        }
        else if (dlqProvider.Equals("FileSystem", StringComparison.OrdinalIgnoreCase))
        {
            var deadLetterPath = configuration["ProbotSharp:Adapters:DeadLetterQueue:Options:Path"]
                ?? Path.Combine(AppContext.BaseDirectory, "dead-letter-queue");
            services.AddSingleton<IDeadLetterQueuePort>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<FileSystemDeadLetterQueueAdapter>>();
                return new FileSystemDeadLetterQueueAdapter(deadLetterPath, logger);
            });
        }
        else
        {
            throw new InvalidOperationException(
                $"Unknown DeadLetterQueue provider '{dlqProvider}'. Valid providers: InMemory, Database, FileSystem");
        }

        // Register ProbotSharpContext factory for event routing
        services.AddScoped<IProbotSharpContextFactory, ProbotSharpContextFactory>();

        services.AddPersistenceAdapters(configuration);
        return services;
    }
}
