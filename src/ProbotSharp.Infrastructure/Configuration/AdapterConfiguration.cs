// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace ProbotSharp.Infrastructure.Configuration;

/// <summary>
/// Root configuration for all infrastructure adapters.
/// Follows the Gateway Aggregation pattern - single point of adapter configuration.
/// </summary>
public sealed class AdapterConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "ProbotSharp:Adapters";

    /// <summary>
    /// Cache adapter configuration.
    /// </summary>
    [Required]
    public CacheAdapterOptions Cache { get; set; } = new();

    /// <summary>
    /// Idempotency adapter configuration.
    /// </summary>
    [Required]
    public IdempotencyAdapterOptions Idempotency { get; set; } = new();

    /// <summary>
    /// Persistence adapter configuration.
    /// </summary>
    [Required]
    public PersistenceAdapterOptions Persistence { get; set; } = new();

    /// <summary>
    /// Replay queue adapter configuration.
    /// </summary>
    [Required]
    public ReplayQueueAdapterOptions ReplayQueue { get; set; } = new();

    /// <summary>
    /// Metrics adapter configuration.
    /// </summary>
    [Required]
    public MetricsAdapterOptions Metrics { get; set; } = new();

    /// <summary>
    /// Tracing adapter configuration.
    /// </summary>
    [Required]
    public TracingAdapterOptions Tracing { get; set; } = new();

    /// <summary>
    /// Validates the entire adapter configuration.
    /// Fails fast at startup if configuration is invalid.
    /// </summary>
    public void Validate()
    {
        Cache.Validate();
        Idempotency.Validate();
        Persistence.Validate();
        ReplayQueue.Validate();
        Metrics.Validate();
        Tracing.Validate();
    }
}

/// <summary>
/// Cache adapter configuration options.
/// </summary>
public sealed class CacheAdapterOptions
{
    /// <summary>
    /// Selected cache provider.
    /// </summary>
    [Required]
    public CacheProvider Provider { get; set; } = CacheProvider.Memory;

    /// <summary>
    /// Provider-specific options (e.g., Redis connection string).
    /// </summary>
    public Dictionary<string, string> Options { get; set; } = new();

    /// <summary>
    /// Gets the Redis connection string from options.
    /// </summary>
    public string? GetRedisConnectionString() =>
        Options.TryGetValue("ConnectionString", out var value) ? value : null;

    /// <summary>
    /// Gets the Redis instance name from options.
    /// </summary>
    public string GetRedisInstanceName() =>
        Options.TryGetValue("InstanceName", out var value) ? value : "ProbotSharp:";

    /// <summary>
    /// Validates cache configuration.
    /// </summary>
    public void Validate()
    {
        if (Provider == CacheProvider.Redis || Provider == CacheProvider.Distributed)
        {
            var connectionString = GetRedisConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Cache provider '{Provider}' requires 'ConnectionString' in Options. " +
                    "Add: \"Options\": { \"ConnectionString\": \"localhost:6379\" }");
            }
        }
    }
}

/// <summary>
/// Idempotency adapter configuration options.
/// </summary>
public sealed class IdempotencyAdapterOptions
{
    /// <summary>
    /// Selected idempotency provider.
    /// </summary>
    [Required]
    public IdempotencyProvider Provider { get; set; } = IdempotencyProvider.Database;

    /// <summary>
    /// Provider-specific options.
    /// </summary>
    public Dictionary<string, string> Options { get; set; } = new();

    /// <summary>
    /// Gets the Redis connection string from options.
    /// </summary>
    public string? GetRedisConnectionString() =>
        Options.TryGetValue("ConnectionString", out var value) ? value : null;

    /// <summary>
    /// Gets the expiration hours for idempotency keys.
    /// </summary>
    public int GetExpirationHours() =>
        Options.TryGetValue("ExpirationHours", out var value) && int.TryParse(value, out var hours) ? hours : 24;

    /// <summary>
    /// Validates idempotency configuration.
    /// </summary>
    public void Validate()
    {
        if (Provider == IdempotencyProvider.Redis)
        {
            var connectionString = GetRedisConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Idempotency provider '{Provider}' requires 'ConnectionString' in Options.");
            }
        }
    }
}

/// <summary>
/// Persistence adapter configuration options.
/// </summary>
public sealed class PersistenceAdapterOptions
{
    /// <summary>
    /// Selected persistence provider.
    /// </summary>
    [Required]
    public PersistenceProvider Provider { get; set; } = PersistenceProvider.InMemory;

    /// <summary>
    /// Provider-specific options.
    /// </summary>
    public Dictionary<string, string> Options { get; set; } = new();

    /// <summary>
    /// Gets the database connection string from options.
    /// </summary>
    public string? GetConnectionString() =>
        Options.TryGetValue("ConnectionString", out var value) ? value : null;

    /// <summary>
    /// Validates persistence configuration.
    /// </summary>
    public void Validate()
    {
        if (Provider == PersistenceProvider.PostgreSQL)
        {
            var connectionString = GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Persistence provider '{Provider}' requires 'ConnectionString' in Options.");
            }
        }
    }
}

/// <summary>
/// Replay queue adapter configuration options.
/// </summary>
public sealed class ReplayQueueAdapterOptions
{
    /// <summary>
    /// Selected replay queue provider.
    /// </summary>
    [Required]
    public ReplayQueueProvider Provider { get; set; } = ReplayQueueProvider.InMemory;

    /// <summary>
    /// Provider-specific options.
    /// </summary>
    public Dictionary<string, string> Options { get; set; } = new();

    /// <summary>
    /// Gets the file system path from options.
    /// </summary>
    public string? GetPath() =>
        Options.TryGetValue("Path", out var value) ? value : null;

    /// <summary>
    /// Gets the Azure Queue connection string from options.
    /// </summary>
    public string? GetAzureQueueConnectionString() =>
        Options.TryGetValue("ConnectionString", out var value) ? value : null;

    /// <summary>
    /// Validates replay queue configuration.
    /// </summary>
    public void Validate()
    {
        if (Provider == ReplayQueueProvider.FileSystem)
        {
            var path = GetPath();
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException(
                    $"ReplayQueue provider '{Provider}' requires 'Path' in Options.");
            }
        }
        else if (Provider == ReplayQueueProvider.AzureQueue)
        {
            var connectionString = GetAzureQueueConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"ReplayQueue provider '{Provider}' requires 'ConnectionString' in Options.");
            }
        }
    }
}

/// <summary>
/// Metrics adapter configuration options.
/// </summary>
public sealed class MetricsAdapterOptions
{
    /// <summary>
    /// Selected metrics provider.
    /// </summary>
    [Required]
    public MetricsProvider Provider { get; set; } = MetricsProvider.NoOp;

    /// <summary>
    /// Provider-specific options.
    /// </summary>
    public Dictionary<string, string> Options { get; set; } = new();

    /// <summary>
    /// Gets the meter name from options.
    /// </summary>
    public string GetMeterName() =>
        Options.TryGetValue("MeterName", out var value) ? value : "ProbotSharp";

    /// <summary>
    /// Gets the version from options.
    /// </summary>
    public string? GetVersion() =>
        Options.TryGetValue("Version", out var value) ? value : null;

    /// <summary>
    /// Validates metrics configuration.
    /// </summary>
    public void Validate()
    {
        // No required validation for metrics
    }
}

/// <summary>
/// Tracing adapter configuration options.
/// </summary>
public sealed class TracingAdapterOptions
{
    /// <summary>
    /// Selected tracing provider.
    /// </summary>
    [Required]
    public TracingProvider Provider { get; set; } = TracingProvider.NoOp;

    /// <summary>
    /// Provider-specific options.
    /// </summary>
    public Dictionary<string, string> Options { get; set; } = new();

    /// <summary>
    /// Gets the source name from options.
    /// </summary>
    public string GetSourceName() =>
        Options.TryGetValue("SourceName", out var value) ? value : "ProbotSharp";

    /// <summary>
    /// Gets the version from options.
    /// </summary>
    public string? GetVersion() =>
        Options.TryGetValue("Version", out var value) ? value : null;

    /// <summary>
    /// Validates tracing configuration.
    /// </summary>
    public void Validate()
    {
        // No required validation for tracing
    }
}
