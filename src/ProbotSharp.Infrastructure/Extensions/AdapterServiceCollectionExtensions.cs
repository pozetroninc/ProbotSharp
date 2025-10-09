// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Infrastructure.Adapters.Caching;
using ProbotSharp.Infrastructure.Adapters.Idempotency;
using ProbotSharp.Infrastructure.Adapters.Observability;
using ProbotSharp.Infrastructure.Adapters.Workers;
using ProbotSharp.Infrastructure.Configuration;

namespace ProbotSharp.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering infrastructure adapters using the Adapter Provider pattern.
/// Follows cloud patterns: Strategy, Registry, Gateway Aggregation.
/// </summary>
public static class AdapterServiceCollectionExtensions
{
    /// <summary>
    /// Registers infrastructure adapters based on configuration.
    /// Validates configuration at startup (fail-fast principle).
    /// </summary>
    public static IServiceCollection AddInfrastructureAdapters(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Bind and validate adapter configuration
        var adapterConfig = new AdapterConfiguration();
        configuration.GetSection(AdapterConfiguration.SectionName).Bind(adapterConfig);

        // Validate configuration (fail fast at startup)
        adapterConfig.Validate();

        // Register configuration as singleton for DI
        services.AddSingleton(Options.Create(adapterConfig));

        // Register adapters using factory pattern
        services.AddCacheAdapter(adapterConfig.Cache);
        services.AddIdempotencyAdapter(adapterConfig.Idempotency);
        services.AddReplayQueueAdapter(adapterConfig.ReplayQueue);
        services.AddMetricsAdapter(adapterConfig.Metrics);
        services.AddTracingAdapter(adapterConfig.Tracing);

        return services;
    }

    /// <summary>
    /// Registers cache adapter based on provider selection.
    /// </summary>
    private static void AddCacheAdapter(
        this IServiceCollection services,
        CacheAdapterOptions options)
    {
        switch (options.Provider)
        {
            case CacheProvider.Memory:
                services.AddMemoryCache();
                services.AddSingleton<IAccessTokenCachePort, InMemoryAccessTokenCacheAdapter>();
                break;

            case CacheProvider.Redis:
                var connectionString = options.GetRedisConnectionString()!;
                services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
                {
                    return StackExchange.Redis.ConnectionMultiplexer.Connect(connectionString);
                });
                services.AddSingleton<IAccessTokenCachePort, RedisAccessTokenCacheAdapter>();
                break;

            case CacheProvider.Distributed:
                // Future: Add IDistributedCache-based adapter
                throw new NotImplementedException(
                    $"Cache provider '{options.Provider}' is not yet implemented. " +
                    "Use 'Memory' or 'Redis'.");

            default:
                throw new InvalidOperationException(
                    $"Unknown cache provider: {options.Provider}");
        }
    }

    /// <summary>
    /// Registers idempotency adapter based on provider selection.
    /// </summary>
    private static void AddIdempotencyAdapter(
        this IServiceCollection services,
        IdempotencyAdapterOptions options)
    {
        switch (options.Provider)
        {
            case IdempotencyProvider.Memory:
                services.AddMemoryCache();
                services.AddSingleton<IIdempotencyPort, InMemoryIdempotencyAdapter>();
                break;

            case IdempotencyProvider.Database:
                services.AddScoped<IIdempotencyPort, DbIdempotencyAdapter>();
                break;

            case IdempotencyProvider.Redis:
                var connectionString = options.GetRedisConnectionString()!;
                services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
                {
                    var existing = sp.GetService<StackExchange.Redis.IConnectionMultiplexer>();
                    if (existing != null)
                    {
                        return existing; // Reuse cache connection if already registered
                    }
                    return StackExchange.Redis.ConnectionMultiplexer.Connect(connectionString);
                });
                services.AddSingleton<IIdempotencyPort, RedisIdempotencyAdapter>();
                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown idempotency provider: {options.Provider}");
        }
    }

    /// <summary>
    /// Registers replay queue adapter based on provider selection.
    /// </summary>
    private static void AddReplayQueueAdapter(
        this IServiceCollection services,
        ReplayQueueAdapterOptions options)
    {
        switch (options.Provider)
        {
            case ReplayQueueProvider.InMemory:
                services.AddSingleton<IWebhookReplayQueuePort, InMemoryWebhookReplayQueueAdapter>();
                break;

            case ReplayQueueProvider.FileSystem:
                var path = options.GetPath() ?? Path.Combine(AppContext.BaseDirectory, "replay-queue");
                services.AddSingleton<IWebhookReplayQueuePort>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<FileSystemWebhookReplayQueueAdapter>>();
                    return new FileSystemWebhookReplayQueueAdapter(path, logger);
                });
                break;

            case ReplayQueueProvider.AzureQueue:
                // Future: Add Azure Queue Storage adapter
                throw new NotImplementedException(
                    $"ReplayQueue provider '{options.Provider}' is not yet implemented. " +
                    "Use 'InMemory' or 'FileSystem'.");

            default:
                throw new InvalidOperationException(
                    $"Unknown replay queue provider: {options.Provider}");
        }
    }

    /// <summary>
    /// Registers metrics adapter based on provider selection.
    /// </summary>
    private static void AddMetricsAdapter(
        this IServiceCollection services,
        MetricsAdapterOptions options)
    {
        switch (options.Provider)
        {
            case MetricsProvider.NoOp:
                services.AddSingleton<IMetricsPort, NoOpMetricsAdapter>();
                break;

            case MetricsProvider.OpenTelemetry:
                var meterName = options.GetMeterName();
                var version = options.GetVersion();
                services.AddSingleton<IMetricsPort>(sp =>
                    new OpenTelemetryMetricsAdapter(meterName, version));
                break;

            case MetricsProvider.Prometheus:
                // Future: Add Prometheus metrics adapter
                throw new NotImplementedException(
                    $"Metrics provider '{options.Provider}' is not yet implemented. " +
                    "Use 'NoOp' or 'OpenTelemetry'.");

            default:
                throw new InvalidOperationException(
                    $"Unknown metrics provider: {options.Provider}");
        }
    }

    /// <summary>
    /// Registers tracing adapter based on provider selection.
    /// </summary>
    private static void AddTracingAdapter(
        this IServiceCollection services,
        TracingAdapterOptions options)
    {
        switch (options.Provider)
        {
            case TracingProvider.NoOp:
                services.AddSingleton<ITracingPort, NoOpTracingAdapter>();
                break;

            case TracingProvider.ActivitySource:
                var sourceName = options.GetSourceName();
                var version = options.GetVersion();
                services.AddSingleton<ITracingPort>(sp =>
                    new ActivitySourceTracingAdapter(sourceName, version));
                break;

            case TracingProvider.OpenTelemetry:
                // Future: Add OpenTelemetry tracing adapter
                throw new NotImplementedException(
                    $"Tracing provider '{options.Provider}' is not yet implemented. " +
                    "Use 'NoOp' or 'ActivitySource'.");

            default:
                throw new InvalidOperationException(
                    $"Unknown tracing provider: {options.Provider}");
        }
    }
}
