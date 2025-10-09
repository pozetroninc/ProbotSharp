# Minimal Deployment Guide

This guide shows how to run ProbotSharp without PostgreSQL, Redis, or other external dependencies. Perfect for learning, simple bots, and low-traffic deployments.

## Philosophy: Start Simple, Scale Later

ProbotSharp embraces the Node.js Probot philosophy: **don't use a database unless you need it**.

Many GitHub Apps are simple automation scripts that:
- React to webhooks in real-time
- Don't need to persist state
- Handle low to moderate traffic
- Run on a single instance

For these scenarios, running a full database and cache infrastructure is overkill. Start with in-memory implementations and scale up when you need it.

## Table of Contents

- [When to Use Minimal Deployment](#when-to-use-minimal-deployment)
- [When to Use Full Infrastructure](#when-to-use-full-infrastructure)
- [Deployment Comparison Matrix](#deployment-comparison-matrix)
- [Minimal Configuration](#minimal-configuration)
- [Quick Start Examples](#quick-start-examples)
- [Scaling Up](#scaling-up)
- [Limitations and Trade-offs](#limitations-and-trade-offs)

## When to Use Minimal Deployment

Choose minimal deployment when:

- **Learning ProbotSharp** - Get started in 5 minutes without infrastructure setup
- **Simple automation** - Bots that react to webhooks without complex state
- **Low traffic** - Fewer than 100 webhooks per hour
- **Single instance** - Running on one server/container
- **Development/Testing** - Local development and CI/CD test environments
- **Prototyping** - Validating ideas before production deployment
- **Personal projects** - Small-scale automation for your own repositories

### Perfect Use Cases

- **Auto-labeler** - Add labels based on PR content
- **Welcome bot** - Comment on first-time contributors
- **Status checker** - Update commit status based on file changes
- **Issue triage** - Auto-assign issues based on labels
- **Documentation bot** - Update docs on release
- **Notification bot** - Post to Slack/Discord on events

## When to Use Full Infrastructure

Upgrade to PostgreSQL/Redis when:

- **High traffic** - More than 100 webhooks per hour
- **Multi-instance** - Running 2+ instances for high availability
- **State persistence** - Need to survive restarts without data loss
- **Webhook replay** - Need durable queue for failed webhook retries
- **Analytics** - Querying webhook history and metrics
- **Compliance** - Audit trail requirements
- **Production deployment** - Mission-critical automation

## Deployment Comparison Matrix

| Feature | Minimal | Standard | Enterprise |
|---------|---------|----------|------------|
| **Setup Time** | 5 minutes | 30 minutes | 2 hours |
| **Dependencies** | None | PostgreSQL | PostgreSQL, Redis, Queue |
| **Infrastructure Cost** | $0 (app hosting only) | ~$20/mo | ~$85-90/mo |
| **Persistence** | No | Yes | Yes |
| **Webhook History** | No | Yes | Yes |
| **Replay Queue** | In-memory (lost on restart) | File-based | Durable (PostgreSQL/Redis) |
| **Idempotency** | In-memory (Memory) | Database | Redis (fast) |
| **Access Token Cache** | In-memory (Memory) | In-memory | Redis (shared) |
| **Scale** | 1 instance | 1-3 instances | Many instances |
| **Max Throughput** | ~100 webhooks/hour | ~500 webhooks/hour | 1000+ webhooks/hour |
| **High Availability** | No | Limited | Yes |
| **Suitable For** | Learning, prototypes, simple bots | Production single-instance | Mission-critical, high-traffic |

## Minimal Configuration

### Overview

Minimal deployment uses:
- **MemoryCache** for access token caching (ASP.NET Core built-in)
- **MemoryCache** for idempotency via InMemoryIdempotencyAdapter (no database required)
- **InMemoryWebhookReplayQueueAdapter** for webhook retries
- **No persistence** - webhooks not stored in database
- **File-based app config** - no database for app credentials

### Configuration File: appsettings.json

Create `appsettings.json` with minimal configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "ProbotSharp": "Debug"
    }
  },
  "AllowedHosts": "*",

  "ProbotSharp": {
    "GitHub": {
      "AppId": "YOUR_GITHUB_APP_ID",
      "WebhookSecret": "YOUR_WEBHOOK_SECRET",
      "PrivateKey": "private-key.pem"
    },

    "Adapters": {
      "Cache": {
        "Provider": "InMemory",
        "Options": {}
      },
      "Idempotency": {
        "Provider": "InMemory",
        "Options": {
          "ExpirationHours": "24"
        }
      },
      "Persistence": {
        "Provider": "InMemory",
        "Options": {}
      },
      "ReplayQueue": {
        "Provider": "InMemory",
        "Options": {
          "MaxRetryAttempts": "3",
          "RetryBaseDelayMs": "1000",
          "PollIntervalMs": "1000"
        }
      },
      "DeadLetterQueue": {
        "Provider": "InMemory",
        "Options": {}
      },
      "Metrics": {
        "Provider": "NoOp",
        "Options": {}
      },
      "Tracing": {
        "Provider": "NoOp",
        "Options": {}
      }
    }
  }
}
```

### Configuration File: appsettings.Development.json

For local development with even more minimal setup:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "ProbotSharp": "Trace"
    }
  },

  "ProbotSharp": {
    "GitHub": {
      "AppId": "123456",
      "WebhookSecret": "development",
      "PrivateKey": "dev-private-key.pem"
    },

    "Adapters": {
      "Cache": { "Provider": "InMemory" },
      "Idempotency": {
        "Provider": "Database",
        "Options": { "ExpirationHours": "1" }
      },
      "Persistence": { "Provider": "InMemory" },
      "ReplayQueue": {
        "Provider": "InMemory",
        "Options": {
          "MaxRetryAttempts": "2",
          "RetryBaseDelayMs": "500"
        }
      },
      "DeadLetterQueue": { "Provider": "InMemory" },
      "Metrics": { "Provider": "NoOp" },
      "Tracing": { "Provider": "NoOp" }
    }
  }
}
```

### Configuration with SQLite (Optional Persistence)

If you want minimal persistence without PostgreSQL, use SQLite:

```json
{
  "ConnectionStrings": {
    "ProbotSharp": "Data Source=probotsharp.db"
  },
  "ProbotSharp": {
    "GitHub": {
      "AppId": "YOUR_GITHUB_APP_ID",
      "WebhookSecret": "YOUR_WEBHOOK_SECRET",
      "PrivateKey": "private-key.pem"
    },

    "Adapters": {
      "Cache": { "Provider": "InMemory" },
      "Idempotency": {
        "Provider": "Database",
        "Options": { "ExpirationHours": "24" }
      },
      "Persistence": {
        "Provider": "SQLite",
        "Options": {}
      },
      "ReplayQueue": {
        "Provider": "FileSystem",
        "Options": {
          "Path": "./replay-queue",
          "MaxRetryAttempts": "3"
        }
      },
      "DeadLetterQueue": {
        "Provider": "FileSystem",
        "Options": { "Path": "./dead-letter-queue" }
      },
      "Metrics": { "Provider": "NoOp" },
      "Tracing": { "Provider": "NoOp" }
    }
  }
}
```

### Environment Variables

You can also configure via environment variables (useful for Docker/containers):

```bash
# GitHub App Configuration
export ProbotSharp__GitHub__AppId=123456
export ProbotSharp__GitHub__WebhookSecret=your_webhook_secret
export ProbotSharp__GitHub__PrivateKey=/app/private-key.pem

# Configure In-Memory Adapters
export ProbotSharp__Adapters__Cache__Provider=Memory
export ProbotSharp__Adapters__Idempotency__Provider=Memory
export ProbotSharp__Adapters__Persistence__Provider=InMemory
export ProbotSharp__Adapters__ReplayQueue__Provider=InMemory
export ProbotSharp__Adapters__DeadLetterQueue__Provider=InMemory
export ProbotSharp__Adapters__Metrics__Provider=NoOp
export ProbotSharp__Adapters__Tracing__Provider=NoOp
```

## Quick Start Examples

### Example 1: Minimal Development Setup

```bash
# Create new bot from template
dotnet new probotsharp-app -n MyBot -o mybot
cd mybot

# Create minimal appsettings.json
cat > appsettings.json << 'EOF'
{
  "ProbotSharp": {
    "GitHub": {
      "AppId": "YOUR_APP_ID",
      "WebhookSecret": "YOUR_SECRET",
      "PrivateKey": "private-key.pem"
    },
    "Adapters": {
      "Cache": { "Provider": "InMemory" },
      "Idempotency": { "Provider": "InMemory" },
      "Persistence": { "Provider": "InMemory" },
      "ReplayQueue": { "Provider": "InMemory" },
      "DeadLetterQueue": { "Provider": "InMemory" },
      "Metrics": { "Provider": "NoOp" },
      "Tracing": { "Provider": "NoOp" }
    }
  }
}
EOF

# Download your GitHub App private key to private-key.pem
# Get credentials from: https://github.com/settings/apps/your-app

# Run without any infrastructure
dotnet run
```

App runs on http://localhost:5000 with zero external dependencies!

### Example 2: Docker Deployment (No Database)

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
COPY private-key.pem .
COPY appsettings.json .

ENV ASPNETCORE_URLS=http://+:8080
ENV ProbotSharp__Adapters__Cache__Provider=Memory
ENV ProbotSharp__Adapters__Idempotency__Provider=Memory
ENV ProbotSharp__Adapters__Persistence__Provider=InMemory
ENV ProbotSharp__Adapters__ReplayQueue__Provider=InMemory
ENV ProbotSharp__Adapters__DeadLetterQueue__Provider=InMemory
ENV ProbotSharp__Adapters__Metrics__Provider=NoOp
ENV ProbotSharp__Adapters__Tracing__Provider=NoOp

EXPOSE 8080
ENTRYPOINT ["dotnet", "YourBot.dll"]
```

```bash
# Build and run
docker build -t mybot .
docker run -p 8080:8080 \
  -e ProbotSharp__GitHub__AppId=123456 \
  -e ProbotSharp__GitHub__WebhookSecret=secret \
  mybot
```

### Example 3: Azure Web App (Minimal PaaS)

```bash
# Create resource group
az group create --name mybot-rg --location eastus

# Create App Service plan (Free tier)
az appservice plan create \
  --name mybot-plan \
  --resource-group mybot-rg \
  --sku F1 \
  --is-linux

# Create web app
az webapp create \
  --resource-group mybot-rg \
  --plan mybot-plan \
  --name mybot \
  --runtime "DOTNET|8.0"

# Configure app settings
az webapp config appsettings set \
  --resource-group mybot-rg \
  --name mybot \
  --settings \
    ProbotSharp__GitHub__AppId=123456 \
    ProbotSharp__GitHub__WebhookSecret=secret \
    ProbotSharp__Adapters__Cache__Provider=Memory \
    ProbotSharp__Adapters__Idempotency__Provider=Memory \
    ProbotSharp__Adapters__Persistence__Provider=InMemory \
    ProbotSharp__Adapters__ReplayQueue__Provider=InMemory \
    ProbotSharp__Adapters__DeadLetterQueue__Provider=InMemory

# Deploy (from local Git or GitHub Actions)
az webapp deployment source config-local-git \
  --name mybot \
  --resource-group mybot-rg
```

Total cost: **$0/month** (Free tier) or **~$13/month** (Basic B1)

### Example 4: Railway/Render Deployment

Both platforms support minimal deployments with zero infrastructure:

**Railway**:
```bash
# Install Railway CLI
npm install -g @railway/cli

# Login and init
railway login
railway init

# Add environment variables in Railway dashboard
# Deploy
railway up
```

**Render**:
```yaml
# render.yaml
services:
  - type: web
    name: mybot
    env: docker
    envVars:
      - key: ProbotSharp__GitHub__AppId
        value: 123456
      - key: ProbotSharp__GitHub__WebhookSecret
        sync: false
      - key: ProbotSharp__Adapters__Cache__Provider
        value: Memory
      - key: ProbotSharp__Adapters__Idempotency__Provider
        value: Memory
      - key: ProbotSharp__Adapters__Persistence__Provider
        value: InMemory
      - key: ProbotSharp__Adapters__ReplayQueue__Provider
        value: InMemory
      - key: ProbotSharp__Adapters__DeadLetterQueue__Provider
        value: InMemory
```

## Scaling Up

When you're ready to scale, upgrade incrementally:

### Step 1: Add File-Based Replay Queue

Survive restarts without losing retry queue:

```json
{
  "ProbotSharp": {
    "Adapters": {
      "ReplayQueue": {
        "Provider": "FileSystem",
        "Options": {
          "Path": "/app/data/replay-queue"
        }
      }
    }
  }
}
```

### Step 2: Add SQLite Database

Enable webhook history and persistence:

```json
{
  "ConnectionStrings": {
    "ProbotSharp": "Data Source=/app/data/probotsharp.db"
  },
  "ProbotSharp": {
    "Adapters": {
      "Persistence": {
        "Provider": "SQLite",
        "Options": {}
      }
    }
  }
}
```

### Step 3: Upgrade to PostgreSQL

For production durability and performance:

```json
{
  "ConnectionStrings": {
    "ProbotSharp": "Host=postgres;Database=probotsharp;Username=user;Password=pass"
  },
  "ProbotSharp": {
    "Adapters": {
      "Persistence": {
        "Provider": "PostgreSQL",
        "Options": {}
      }
    }
  }
}
```

### Step 4: Add Redis Cache

For multi-instance deployments:

```json
{
  "ProbotSharp": {
    "Adapters": {
      "Cache": {
        "Provider": "Redis",
        "Options": {
          "ConnectionString": "redis:6379"
        }
      },
      "Idempotency": {
        "Provider": "Redis",
        "Options": {
          "ConnectionString": "redis:6379"
        }
      }
    }
  }
}
```

### Migration Path Summary

```
1. Minimal (In-Memory)
   ↓ Add file queue
2. File-Based Queue
   ↓ Add SQLite
3. SQLite Database
   ↓ Upgrade to PostgreSQL
4. PostgreSQL
   ↓ Add Redis
5. Full Infrastructure (PostgreSQL + Redis)
```

Each step is backward compatible - no code changes required!

## Limitations and Trade-offs

### Minimal Deployment Limitations

**No Persistence**
- Webhook history is not stored
- Cannot query past events
- No audit trail

**Lost on Restart**
- In-memory cache cleared
- Replay queue lost (unless file-based)
- Idempotency keys reset

**Single Instance Only**
- Cannot scale horizontally
- No load balancing
- Single point of failure

**Limited Throughput**
- MemoryCache has size limits
- No distributed caching
- ~100 webhooks/hour maximum

**No Analytics**
- Cannot query webhook metrics
- No historical reporting
- Limited observability

### When Limitations Don't Matter

Many successful GitHub Apps run perfectly fine with these limitations:

- **Auto-labelers** - Stateless, react to events only
- **Welcome bots** - Comment once, no state needed
- **Status checkers** - Compute status from PR data
- **Notification bots** - Forward to external services
- **Simple automation** - Trigger actions without state

### Risk Mitigation

To minimize risks of minimal deployment:

1. **Enable file-based queue** for replay durability
2. **Monitor logs** for errors and failures
3. **GitHub webhook redelivery** - manually retry failed webhooks
4. **Keep it simple** - complex state management requires database
5. **Plan upgrade path** - know when to scale up

## In-Memory Implementations Reference

ProbotSharp provides production-ready in-memory adapters:

### InMemoryAccessTokenCacheAdapter

- **Location**: `ProbotSharp.Infrastructure.Adapters.Caching.InMemoryAccessTokenCacheAdapter`
- **Purpose**: Caches GitHub installation access tokens (expires in 1 hour)
- **Storage**: ASP.NET Core `IMemoryCache` (LRU eviction)
- **Expiration**: Automatic, based on token expiry (minus 30 second buffer)
- **Thread-safe**: Yes
- **Suitable for**: Single-instance deployments

### InMemoryWebhookReplayQueueAdapter

- **Location**: `ProbotSharp.Infrastructure.Adapters.Workers.InMemoryWebhookReplayQueueAdapter`
- **Purpose**: Queue for retrying failed webhook processing
- **Storage**: `ConcurrentQueue<T>` (thread-safe, in-memory)
- **Durability**: Lost on restart
- **Suitable for**: Development, low-traffic, stateless bots

### DbIdempotencyAdapter (with in-memory EF Core)

- **Location**: `ProbotSharp.Infrastructure.Adapters.Idempotency.DbIdempotencyAdapter`
- **Purpose**: Prevents duplicate webhook processing
- **Storage**: Database (can use SQLite in-memory)
- **Default TTL**: 24 hours
- **Cleanup**: Background job every 60 minutes (database mode only)
- **Suitable for**: Single-instance deployments

### No Persistence Adapter

When using `Persistence.Provider = "InMemory"`:
- **IWebhookStoragePort**: In-memory implementation (webhooks not persisted across restarts)
- **IManifestStoragePort**: File-based adapter (stores app manifest in JSON)
- **App configuration**: File-based (appsettings.json)

## Summary

Minimal deployment removes the infrastructure barrier:

- **Zero setup time** - No databases, no Docker, just run
- **Zero infrastructure cost** - App hosting only
- **Perfect for learning** - Focus on bot logic, not operations
- **Production-ready** - For simple, low-traffic bots
- **Easy scaling** - Upgrade incrementally when needed

Start simple. Scale when you need it. That's the ProbotSharp way.

## Next Steps

- **Create your first minimal bot**: See [examples/MinimalBot/](../examples/MinimalBot/)
- **Local development**: [docs/LocalDevelopment.md](LocalDevelopment.md)
- **Scaling up**: [docs/Deployment.md](Deployment.md)
- **Production checklist**: [docs/BestPractices-Checklist.md](BestPractices-Checklist.md)
