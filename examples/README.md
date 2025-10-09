# ProbotSharp Examples

This directory contains working examples demonstrating various features of ProbotSharp.

## Quick Start

### Using Make (Recommended)

The easiest way to build, test, and run examples is using the Makefile:

```bash
cd examples

# Build a specific example
make build-MinimalBot

# Run a specific example (interactive)
make run-MinimalBot

# Run a specific example (detached/background)
make run-detached-MinimalBot

# Test a specific example (build + health check + webhook test)
make test-MinimalBot

# Build all examples
make build-all

# Test all examples
make test-all

# Clean up containers and images
make clean-MinimalBot
make clean  # Clean all
```

### Using Shell Scripts (Cross-Platform)

For systems without `make` installed (e.g., Windows), use the shell scripts:

```bash
# Build a specific example
./docker-build.sh MinimalBot

# Test a specific example
./docker-test.sh MinimalBot

# Build all examples
./docker-build-all.sh
```

### Manual Docker Commands

You can also use Docker directly:

```bash
# Build
docker build -f Dockerfile --build-arg EXAMPLE_NAME=MinimalBot -t minimalbot:latest ..

# Run
docker run -p 8080:5000 \
  -e ProbotSharp__WebhookSecret=development \
  minimalbot:latest

# Access the app
curl http://localhost:8080/health
```

## Available Examples

| Example | Description | Features Demonstrated |
|---------|-------------|-----------------------|
| **MinimalBot** | Simplest possible bot | Basic webhook handling, issue events |
| **HelloWorldBot** | Hello world bot | Event handlers, GitHub API calls |
| **AttachmentsBot** | Slack-style message attachments | Comment attachments, formatting |
| **SlashCommandsBot** | Command parser for issues/PRs | Slash commands, command routing |
| **DryRunBot** | Dry-run mode testing | Dry-run logging, bulk operations |
| **PaginationBot** | Paginated API results | Async pagination, iterators |
| **MetadataBot** | Issue metadata storage | Key-value metadata, persistence |
| **ExtensionsBot** | Extension methods | Context extensions, utilities |
| **GraphQLBot** | GraphQL API usage | GraphQL queries, type-safe APIs |
| **HttpExtensibilityBot** | HTTP client customization | HTTP hooks, extensibility points |

## Docker Architecture

All examples use a **shared Dockerfile** with build arguments to eliminate duplication:

- **Dockerfile**: Multi-stage build (SDK → Runtime) with `ARG EXAMPLE_NAME`
- **.dockerignore**: Optimized build context (excludes docs, tests, etc.)
- **Makefile**: Developer-friendly commands for all operations
- **Shell Scripts**: Cross-platform alternatives for `make`

### Why Shared Dockerfile?

Before:
- 10 identical Dockerfiles (one per example)
- Hard to maintain, easy to get out of sync
- Repetitive configuration

After:
- 1 shared Dockerfile with build args
- Single source of truth
- DRY (Don't Repeat Yourself)

## Configuration

All examples use the **new explicit adapter provider configuration format**:

```json
{
  "ProbotSharp": {
    "AppId": "YOUR_GITHUB_APP_ID",
    "WebhookSecret": "development",
    "PrivateKeyPath": "private-key.pem",

    "Adapters": {
      "Cache": {
        "Provider": "InMemory",
        "Options": { "ExpirationMinutes": "60" }
      },
      "Idempotency": {
        "Provider": "InMemory",
        "Options": { "ExpirationHours": "24" }
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

### In-Memory Mode (Default)

All examples default to **zero-infrastructure in-memory mode**:
- No database required
- No Redis required
- No external dependencies
- Perfect for development and testing

### Production Mode

For production, override adapter providers:

```bash
docker run -p 8080:5000 \
  -e ProbotSharp__AppId=YOUR_APP_ID \
  -e ProbotSharp__WebhookSecret=YOUR_SECRET \
  -e ProbotSharp__Adapters__Cache__Provider=Redis \
  -e ProbotSharp__Adapters__Cache__Options__ConnectionString=redis:6379 \
  -e ProbotSharp__Adapters__Persistence__Provider=PostgreSQL \
  -e ProbotSharp__Adapters__Persistence__Options__ConnectionString="Host=db;..." \
  minimalbot:latest
```

## Testing

Each example includes:
- **Health Check**: `GET /health` (HTTP 200 expected)
- **Webhook Endpoint**: `POST /webhooks` (HTTP 202 expected)
- **HMAC Signature Validation**: Verifies webhook authenticity

### Test Procedure

The `make test-<Example>` command performs:
1. **Build**: Docker multi-stage build
2. **Run**: Start container in background
3. **Health Check**: `curl http://localhost:8080/health`
4. **Webhook Test**: Send signed test webhook
5. **Cleanup**: Stop and remove container

### Test Results

Last tested: **2025-10-06**

| Example | Build | Health | Webhook | Status |
|---------|-------|--------|---------|--------|
| MinimalBot | ✅ | ✅ 200 | ✅ 202 | ✅ **PASS** |
| HelloWorldBot | ✅ | ✅ 200 | ✅ 202 | ✅ **PASS** |
| AttachmentsBot | ✅ | ✅ 200 | ✅ 202 | ✅ **PASS** |
| SlashCommandsBot | ✅ | ✅ 200 | ✅ 202 | ✅ **PASS** |
| DryRunBot | ✅ | ✅ 200 | ✅ 202 | ✅ **PASS** |
| PaginationBot | ✅ | ✅ 200 | ✅ 202 | ✅ **PASS** |
| MetadataBot | ✅ | ✅ 200 | ✅ 202 | ✅ **PASS** |
| ExtensionsBot | ✅ | ✅ 200 | ✅ 202 | ✅ **PASS** |
| GraphQLBot | ✅ | ✅ 200 | ✅ 202 | ✅ **PASS** |
| HttpExtensibilityBot | ✅ | ✅ 200 | ✅ 202 | ✅ **PASS** |

**Success Rate**: 10/10 (100%) 🎉

## Common Files

Each example includes:

| File | Purpose |
|------|---------|
| `Program.cs` | Entry point with WebApplication builder, Serilog logging |
| `appsettings.json` | Configuration with explicit adapter providers |
| `{ExampleName}.csproj` | Web SDK project with Infrastructure + Adapters.Http references |
| `MinimalApp.cs` (or similar) | Event handlers and bot logic |

## Environment Variables

Configure examples at runtime using environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `ProbotSharp__AppId` | GitHub App ID | (Required for real use) |
| `ProbotSharp__WebhookSecret` | Webhook HMAC secret | `development` |
| `ProbotSharp__PrivateKeyPath` | Path to private key PEM | `private-key.pem` |
| `ASPNETCORE_URLS` | HTTP listener URLs | `http://+:5000` |
| `DOTNET_ENVIRONMENT` | Runtime environment | `Production` |

## Troubleshooting

### Build Fails

```bash
# Clear Docker build cache
docker builder prune -a

# Rebuild without cache
docker build --no-cache -f Dockerfile --build-arg EXAMPLE_NAME=MinimalBot -t minimalbot:latest ..
```

### Container Won't Start

```bash
# Check container logs
docker logs <container-name>

# Run in interactive mode
docker run -it -p 8080:5000 minimalbot:latest
```

### Port Already in Use

```bash
# Find process using port 8080
lsof -i :8080  # macOS/Linux
netstat -ano | findstr :8080  # Windows

# Use different port
docker run -p 9090:5000 minimalbot:latest
```

## Next Steps

1. **Explore Examples**: Start with MinimalBot and work your way up
2. **Modify Configuration**: Try different adapter providers
3. **Create Your Own**: Use `dotnet new probotsharp-app` to scaffold a new bot
4. **Deploy**: See `docs/Deployment.md` for production deployment guides

## Resources

- [ProbotSharp Documentation](../docs/)
- [Template for New Bots](../templates/probotsharp-app/)
- [Deployment Guide](../docs/Deployment.md)

---

**Questions or issues?** Open an issue on GitHub!
