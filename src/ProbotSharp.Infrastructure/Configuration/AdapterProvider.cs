// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Infrastructure.Configuration;

/// <summary>
/// Defines available cache adapter providers.
/// </summary>
public enum CacheProvider
{
    /// <summary>
    /// In-memory cache using ASP.NET Core MemoryCache (default).
    /// Suitable for single-instance deployments.
    /// </summary>
    Memory,

    /// <summary>
    /// Distributed cache using Redis.
    /// Required for multi-instance/scaled deployments.
    /// </summary>
    Redis,

    /// <summary>
    /// Distributed cache using IDistributedCache abstraction.
    /// Allows plugging in SQL Server, NCache, etc.
    /// </summary>
    Distributed,
}

/// <summary>
/// Defines available idempotency adapter providers.
/// </summary>
public enum IdempotencyProvider
{
    /// <summary>
    /// In-memory idempotency tracking.
    /// Lost on restart - only for development/testing.
    /// </summary>
    Memory,

    /// <summary>
    /// Database-backed idempotency using Entity Framework (default).
    /// Persists across restarts, works with configured database provider.
    /// </summary>
    Database,

    /// <summary>
    /// Redis-backed idempotency with automatic TTL.
    /// Fastest option for distributed deployments.
    /// </summary>
    Redis,
}

/// <summary>
/// Defines available persistence adapter providers.
/// </summary>
public enum PersistenceProvider
{
    /// <summary>
    /// In-memory database using EF Core InMemory provider.
    /// Lost on restart - only for development/testing.
    /// </summary>
    InMemory,

    /// <summary>
    /// SQLite database - file-based persistence.
    /// Good for single-instance, low-traffic scenarios.
    /// </summary>
    Sqlite,

    /// <summary>
    /// PostgreSQL database - production-grade persistence (default).
    /// Recommended for production deployments.
    /// </summary>
    PostgreSQL,
}

/// <summary>
/// Defines available replay queue adapter providers.
/// </summary>
public enum ReplayQueueProvider
{
    /// <summary>
    /// In-memory queue using ConcurrentQueue (default).
    /// Lost on restart - only for development/testing.
    /// </summary>
    InMemory,

    /// <summary>
    /// File system-based queue using local disk.
    /// Persists across restarts, suitable for single-instance.
    /// </summary>
    FileSystem,

    /// <summary>
    /// Azure Queue Storage - cloud-native queue.
    /// Fully managed, supports distributed processing.
    /// </summary>
    AzureQueue,
}

/// <summary>
/// Defines available metrics adapter providers.
/// </summary>
public enum MetricsProvider
{
    /// <summary>
    /// No-op metrics adapter (default for development).
    /// Metrics are discarded - zero overhead.
    /// </summary>
    NoOp,

    /// <summary>
    /// OpenTelemetry metrics adapter.
    /// Industry standard, works with multiple backends.
    /// </summary>
    OpenTelemetry,

    /// <summary>
    /// Prometheus metrics adapter.
    /// Direct Prometheus exposition format.
    /// </summary>
    Prometheus,
}

/// <summary>
/// Defines available tracing adapter providers.
/// </summary>
public enum TracingProvider
{
    /// <summary>
    /// No-op tracing adapter.
    /// Tracing is disabled - zero overhead.
    /// </summary>
    NoOp,

    /// <summary>
    /// Activity Source-based tracing (default).
    /// Integrates with .NET distributed tracing.
    /// </summary>
    ActivitySource,

    /// <summary>
    /// OpenTelemetry tracing adapter.
    /// Full distributed tracing with span export.
    /// </summary>
    OpenTelemetry,
}
