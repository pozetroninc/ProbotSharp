# Adapter Configuration Guide

This guide explains how to configure infrastructure adapters in ProbotSharp using the **Adapter Provider Pattern**.

## Philosophy

Following **Hexagonal Architecture** and **Cloud Design Patterns**, adapter configuration in ProbotSharp is:

- **Explicit**: Always clear which adapter is active
- **Validated**: Fail fast at startup, not runtime
- **Extensible**: Add new adapters without changing existing code
- **Type-Safe**: Strong typing with enums prevents typos
- **Environment-Aware**: Easy per-environment overrides

## Configuration Structure

```json
{
  "ProbotSharp": {
    "Adapters": {
      "Cache": {
        "Provider": "InMemory",  // Which adapter to use
        "Options": {           // Provider-specific configuration
          "ExpirationMinutes": "60"
        }
      }
    }
  }
}
```

## Available Adapters

### Cache Adapters

| Provider | Use Case | Dependencies | Data Loss on Restart |
|----------|----------|--------------|---------------------|
| **InMemory** | Development, single instance | None | Yes |
| **Redis** | Production, multi-instance | Redis | No |
| Distributed | Enterprise, flexible backend | IDistributedCache | No |

**Example: In-Memory Cache**
```json
{
  "Cache": {
    "Provider": "InMemory",
    "Options": {
      "ExpirationMinutes": "60"
    }
  }
}
```

**Example: Redis Cache**
```json
{
  "Cache": {
    "Provider": "Redis",
    "Options": {
      "ConnectionString": "localhost:6379",
      "InstanceName": "ProbotSharp:",
      "ExpirationMinutes": "60"
    }
  }
}
```

### Idempotency Adapters

> **Probot Comparison:** Unlike Probot (Node.js), which requires manual deduplication tracking, ProbotSharp provides automatic webhook deduplication via the idempotency adapter. This can be disabled by removing `app.UseIdempotency()` from your Program.cs if you need Probot-compatible behavior.

| Provider | Use Case | Dependencies | Data Loss on Restart |
|----------|----------|--------------|---------------------|
| **InMemory** | Development, testing, single instance | None | Yes |
| **Database** | Default, multi-instance | Database | No |
| **Redis** | Production, fastest | Redis | No |

⚠️ **Warning**: InMemory provider loses all idempotency state on application restart. Only use for:
- Development and testing environments
- Single-instance deployments with no restart requirements
- Applications that can tolerate duplicate webhook processing after restart

For production use with multiple instances or restart resilience, use Redis or Database adapters.

**Example: Memory Idempotency (Development)**
```json
{
  "Idempotency": {
    "Provider": "InMemory",
    "Options": {
      "ExpirationHours": "24"
    }
  }
}
```

**Example: Database Idempotency (Production)**
```json
{
  "Idempotency": {
    "Provider": "Database",
    "Options": {
      "ExpirationHours": "24"
    }
  }
}
```

**Example: Redis Idempotency (Production)**
```json
{
  "Idempotency": {
    "Provider": "Redis",
    "Options": {
      "ConnectionString": "localhost:6379",
      "ExpirationHours": "24"
    }
  }
}
```

### Persistence Adapters

| Provider | Use Case | Dependencies | Scalability |
|----------|----------|--------------|-------------|
| **InMemory** | Development, testing | None | Single instance |
| **Sqlite** | Low traffic, embedded | SQLite file | Single instance |
| **PostgreSQL** | Production | PostgreSQL | Multi-instance |

**Example: InMemory Persistence**
```json
{
  "Persistence": {
    "Provider": "InMemory",
    "Options": {}
  }
}
```

**Example: PostgreSQL Persistence**
```json
{
  "Persistence": {
    "Provider": "PostgreSQL",
    "Options": {
      "ConnectionString": "Host=localhost;Database=probotsharp;Username=postgres;Password=postgres"
    }
  }
}
```

### Replay Queue Adapters

| Provider | Use Case | Dependencies | Data Loss on Restart |
|----------|----------|--------------|---------------------|
| **InMemory** | Development | None | Yes |
| **FileSystem** | Single instance, persistent | Local disk | No |
| AzureQueue | Cloud, distributed | Azure Queue Storage | No |

**Example: InMemory Queue**
```json
{
  "ReplayQueue": {
    "Provider": "InMemory",
    "Options": {
      "MaxRetryAttempts": "3",
      "RetryBaseDelayMs": "1000"
    }
  }
}
```

**Example: FileSystem Queue**
```json
{
  "ReplayQueue": {
    "Provider": "FileSystem",
    "Options": {
      "Path": "/var/lib/probotsharp/replay-queue",
      "MaxRetryAttempts": "5"
    }
  }
}
```

### Dead Letter Queue Adapters

| Provider | Use Case | Dependencies | Data Loss on Restart | Scalability |
|----------|----------|--------------|---------------------|-------------|
| **InMemory** | Development, testing | None | Yes | Single instance |
| **Database** | Production, multi-instance | Database | No | Multi-instance |
| **FileSystem** | Production, single instance | Local disk | No | Single instance |

**Example: InMemory Dead Letter Queue**
```json
{
  "DeadLetterQueue": {
    "Provider": "InMemory",
    "Options": {}
  }
}
```

**Example: Database Dead Letter Queue**
```json
{
  "DeadLetterQueue": {
    "Provider": "Database",
    "Options": {}
  }
}
```

**Example: FileSystem Dead Letter Queue**
```json
{
  "DeadLetterQueue": {
    "Provider": "FileSystem",
    "Options": {
      "Path": "/var/lib/probotsharp/dead-letter-queue",
      "RetentionDays": "30"
    }
  }
}
```

### Metrics Adapters

| Provider | Use Case | Dependencies |
|----------|----------|--------------|
| **NoOp** | Development, disable metrics | None |
| **OpenTelemetry** | Production, vendor-neutral | OpenTelemetry |
| Prometheus | Direct Prometheus export | Prometheus client |

**Example: NoOp Metrics**
```json
{
  "Metrics": {
    "Provider": "NoOp",
    "Options": {}
  }
}
```

**Example: OpenTelemetry Metrics**
```json
{
  "Metrics": {
    "Provider": "OpenTelemetry",
    "Options": {
      "MeterName": "ProbotSharp",
      "Version": "1.0.0"
    }
  }
}
```

### Tracing Adapters

| Provider | Use Case | Dependencies |
|----------|----------|--------------|
| **NoOp** | Development, disable tracing | None |
| **ActivitySource** | Default, .NET distributed tracing | None |
| OpenTelemetry | Production, span export | OpenTelemetry |

**Example: ActivitySource Tracing**
```json
{
  "Tracing": {
    "Provider": "ActivitySource",
    "Options": {
      "SourceName": "ProbotSharp",
      "Version": "1.0.0"
    }
  }
}
```

## Pre-Built Configurations

### Minimal Configuration (Zero Infrastructure)

Copy from `appsettings.Adapters.InMemory.json`:

```json
{
  "ProbotSharp": {
    "Adapters": {
      "Cache": { "Provider": "InMemory", "Options": {} },
      "Idempotency": { "Provider": "InMemory", "Options": {} },
      "Persistence": { "Provider": "InMemory", "Options": {} },
      "ReplayQueue": { "Provider": "InMemory", "Options": {} },
      "Metrics": { "Provider": "NoOp", "Options": {} },
      "Tracing": { "Provider": "NoOp", "Options": {} }
    }
  }
}
```

**Use For**: Development, testing, simple bots (<100 webhooks/hour)

### Production Configuration (Redis + PostgreSQL)

Copy from `appsettings.Adapters.Production.json`:

```json
{
  "ProbotSharp": {
    "Adapters": {
      "Cache": {
        "Provider": "Redis",
        "Options": {
          "ConnectionString": "your-redis-host:6379"
        }
      },
      "Idempotency": {
        "Provider": "Redis",
        "Options": {
          "ConnectionString": "your-redis-host:6379"
        }
      },
      "Persistence": {
        "Provider": "PostgreSQL",
        "Options": {
          "ConnectionString": "Host=your-db-host;Database=probotsharp;..."
        }
      },
      "ReplayQueue": {
        "Provider": "FileSystem",
        "Options": {
          "Path": "/var/lib/probotsharp/replay-queue"
        }
      },
      "Metrics": { "Provider": "OpenTelemetry", "Options": {} },
      "Tracing": { "Provider": "ActivitySource", "Options": {} }
    }
  }
}
```

**Use For**: Production, multi-instance, high availability

## Environment-Specific Configuration

Use ASP.NET Core's configuration layering:

**appsettings.json** (base configuration):
```json
{
  "ProbotSharp": {
    "Adapters": {
      "Cache": { "Provider": "InMemory", "Options": {} }
    }
  }
}
```

**appsettings.Production.json** (production overrides):
```json
{
  "ProbotSharp": {
    "Adapters": {
      "Cache": {
        "Provider": "Redis",
        "Options": {
          "ConnectionString": "${REDIS_CONNECTION_STRING}"
        }
      }
    }
  }
}
```

## Environment Variables

Override configuration using environment variables:

```bash
export ProbotSharp__Adapters__Cache__Provider=Redis
export ProbotSharp__Adapters__Cache__Options__ConnectionString=localhost:6379
export ProbotSharp__Adapters__Persistence__Provider=PostgreSQL
export ProbotSharp__Adapters__Persistence__Options__ConnectionString="Host=localhost;..."
```

## Validation

Configuration is **validated at startup** (fail-fast principle):

```
❌ ERROR: Cache provider 'Redis' requires 'ConnectionString' in Options.
   Add: "Options": { "ConnectionString": "localhost:6379" }

Application will NOT start with invalid configuration.
```

## Cloud Design Patterns Applied

This configuration approach implements several cloud design patterns:

- **Strategy Pattern**: Adapter provider name selects implementation strategy
- **Registry Pattern**: Available adapters are registered and discovered
- **Gateway Aggregation**: Single configuration section for all adapters
- **Strangler Fig**: Easy migration between adapter implementations
- **Bulkhead Isolation**: Each adapter configured independently

## Benefits

✅ **Explicit**: Always clear which adapter is active (no guessing)
✅ **Validated**: Invalid configuration fails at startup (fail-fast)
✅ **Extensible**: Add new adapters without modifying existing code (Open/Closed)
✅ **Type-Safe**: Enums prevent typos and invalid values
✅ **Self-Documenting**: Configuration structure explains itself
✅ **Environment-Aware**: Easy per-environment overrides
✅ **DDD-Aligned**: Configuration is a value object with validation

## See Also

- [Architecture Documentation](Architecture.md)
- [Deployment Guide](Deployment.md)
- [Local Development](LocalDevelopment.md)
- [Minimal Deployment](MinimalDeployment.md)
