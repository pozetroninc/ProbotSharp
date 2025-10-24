# Operations & Runtime Guide

This guide describes how to run, configure, and observe Probot-Sharp in development and production environments.

## Runtime Modes

| Mode | Project | Description |
| --- | --- | --- |
| Minimal API | `src/ProbotSharp.Bootstrap.Api` | Hosts inbound HTTP endpoints, health checks, and DI wiring. |
| CLI | `src/ProbotSharp.Bootstrap.Cli` | Hosts CLI commands (work in progress). |
| Worker | `src/ProbotSharp.Adapters.Workers` | Queue/subscription processing (replay, async actions). |

### Minimal API

```bash
dotnet run --project src/ProbotSharp.Bootstrap.Api
```

**Endpoints:**
- `GET /` - Returns application metadata (name and assembly version)
- `POST /webhooks` - GitHub webhook receiver with signature validation
- `POST /webhooks/replay/{deliveryId}` - Manual replay of failed webhooks by delivery ID
- `GET /webhooks/dlq/stats` - Dead-letter queue statistics and item listing
- `GET /health` - Comprehensive health check endpoint with JSON response

**Hosted Services:**
- `WebhookReplayWorker` - Continuously processes the replay queue for failed webhooks

### CLI

```bash
dotnet run --project src/ProbotSharp.Bootstrap.Cli -- --help
```

**Available Commands:**
- `run` - Starts the ProbotSharp application (delegates to `IAppLifecyclePort`)
- `receive` - Replays webhook fixtures for local development and testing (see [Local Development](LocalDevelopment.md#replaying-webhooks-locally))
- `setup` - Interactive setup wizard for creating a new GitHub App
- `version` - Displays version information and assembly details
- `help` - Shows help and usage information for all commands

All commands use Spectre.Console for rich terminal UI and colored output.

### Worker Adapter

- Background service `WebhookReplayWorker` consumes `IWebhookReplayQueuePort`.
- File-system queue adapter available for development and single-instance deployments.
- In-memory queue adapter available for testing.
- Production queue providers (SQS, Azure Service Bus, etc.) remain on the backlog.

## Configuration

ProbotSharp uses the standard .NET configuration system with support for appsettings.json files and environment variables. Configuration is validated at startup to fail fast if required settings are missing.

### Configuration Sections

#### GitHub App Credentials (Required)

Configuration for authenticating with GitHub as a GitHub App. These values are available from your GitHub App settings page at `https://github.com/settings/apps/YOUR_APP_NAME`.

| Setting | Description | Example | Required |
| --- | --- | --- | --- |
| `ProbotSharp:GitHub:AppId` | GitHub App ID from app settings page | `123456` | Yes |
| `ProbotSharp:GitHub:WebhookSecret` | Secret for validating webhook signatures (must match GitHub App webhook secret) | `development-secret-change-in-production` | Yes |
| `ProbotSharp:GitHub:PrivateKey` | Private key (PEM format or Base64) or path to .pem file | `/app/private-key.pem` or `LS0tLS1...` | Yes |
| `ProbotSharp:GitHub:ClientId` | OAuth Client ID (optional, for OAuth flows) | `Iv1.abc123` | No |
| `ProbotSharp:GitHub:ClientSecret` | OAuth Client Secret (optional, for OAuth flows) | `abc123def456` | No |

**Note:** The `WebhookSecret` in Development environment defaults to `development-secret-change-in-production`. This MUST be changed in Production to match your GitHub App's webhook secret.

#### Database Configuration (Required)

Configuration for PostgreSQL database connectivity. The application uses Entity Framework Core for data access with automatic retry policies for transient failures.

| Setting | Description | Default | Environment-Specific |
| --- | --- | --- | --- |
| `ConnectionStrings:ProbotSharp` | PostgreSQL connection string | `Host=localhost;Port=5432;Database=probotsharp;Username=probotsharp;Password=dev-password` | Yes - set per environment |
| `ProbotSharp:Database:Provider` | Database provider (currently only PostgreSQL) | `PostgreSQL` | No |
| `ProbotSharp:Database:EnableSensitiveDataLogging` | Log SQL parameters and sensitive data (development only) | `false` | Yes - `false` in prod, `true` in staging |
| `ProbotSharp:Database:EnableDetailedErrors` | Include detailed error messages in EF Core exceptions | `false` | Yes - `false` in prod, `true` in dev/staging |
| `ProbotSharp:Database:CommandTimeout` | Database command timeout in seconds | `30` | No |
| `ProbotSharp:Database:MaxRetryCount` | Max retry attempts for transient database failures | `3` | No |
| `ProbotSharp:Database:MaxRetryDelay` | Max retry delay in seconds | `30` | No |

**Connection String Format:**
```
Host={host};Port={port};Database={database};Username={user};Password={password};Maximum Pool Size={pool_size}
```

**Note:** SQLite support is planned but not yet implemented.

#### Cache Configuration

Configuration for caching GitHub access tokens and other frequently accessed data. For available providers and configuration options, see [Adapter Configuration](AdapterConfiguration.md#cache-adapters).

**Recommended Configuration:**
- **Development:** InMemory provider (no configuration needed)
- **Production:** Redis provider for multi-instance deployments
- **Connection String Formats:**
  - Basic: `redis:6379` or `localhost:6379`
  - With password: `redis:6379,password=your-redis-password`
  - Multiple hosts: `host1:6379,host2:6379,password=secret`

#### Idempotency Configuration

Configuration for preventing duplicate webhook processing. Idempotency keys are based on GitHub's delivery ID and ensure webhooks are processed exactly once. For available providers and configuration options, see [Adapter Configuration](AdapterConfiguration.md#idempotency-adapters).

**Recommended Configuration:**
- **Development:** Database provider
- **Production:** Redis provider for better performance
- **Multi-instance:** Redis or Database (both support concurrent access)

#### Replay Queue Configuration

Configuration for the webhook replay queue, which handles retrying failed webhook processing. The replay queue uses exponential backoff for retries. For available providers and configuration options, see [Adapter Configuration](AdapterConfiguration.md#replay-queue-adapters).

**Recommended Configuration:**
- **Development:** InMemory provider
- **Single-instance production:** FileSystem provider
- **Multi-instance production:** External queue (planned)

**Retry Behavior:**
- Attempt 1: Immediate retry
- Attempt 2: Base delay (1-2 seconds)
- Attempt 3: 2× base delay (2-4 seconds)
- Attempt 4+: Exponential backoff up to max attempts
- After max attempts: Moved to Dead Letter Queue

#### Dead Letter Queue Configuration

Configuration for the Dead Letter Queue (DLQ), which stores webhooks that failed after all retry attempts. Items in the DLQ can be inspected via the `/webhooks/dlq/stats` endpoint and manually replayed. For available providers and configuration options, see [Adapter Configuration](AdapterConfiguration.md#dead-letter-queue-adapters).

**Recommended Configuration:**
- **Development:** InMemory provider
- **Single-instance production:** FileSystem provider
- **Multi-instance production:** Database provider

**DLQ Behavior:**
- Failed webhooks include error reason, timestamp, and all retry attempt details
- Items accessible via `GET /webhooks/dlq/stats` endpoint
- Manual replay available via `POST /webhooks/replay/{deliveryId}`

#### Metrics & Observability

Configuration for metrics and distributed tracing. For available providers and configuration options, see [Adapter Configuration](AdapterConfiguration.md#metrics-adapters) and [Tracing](AdapterConfiguration.md#tracing-adapters).

**Recommended Configuration:**
- **Development:** NoOp provider (no overhead)
- **Production:** OpenTelemetry provider for observability

**OTLP Endpoint Examples:**
- Local Jaeger: `http://localhost:4317`
- Local OTEL Collector: `http://otel-collector:4317`
- Cloud providers: Varies by service (AWS X-Ray, Azure Monitor, Google Cloud Trace)

**Collected Metrics:**
- Webhook processing duration and count
- Replay queue depth and processing time
- Dead-letter queue item count
- Cache hit/miss ratios
- GitHub API call counts and latencies
- Circuit breaker state changes
- HTTP request/response metrics

**Distributed Tracing:**
- Activity source name: `ProbotSharp`
- Traces complete webhook processing pipeline
- Includes GitHub API calls, database operations, cache operations

#### Resilience Policies (Polly)

Configuration for HTTP client resilience policies using Polly library. These policies apply to all GitHub API calls to handle transient failures gracefully.

**HTTP Timeout:**

Timeout policy for HTTP requests to prevent hanging operations.

| Setting | Description | Default |
| --- | --- | --- |
| `ProbotSharp:Resilience:HttpTimeout:TimeoutSeconds` | Maximum time to wait for HTTP response (seconds) | `30` |

**Circuit Breaker:**

Circuit breaker pattern to prevent cascading failures when GitHub API is experiencing issues.

| Setting | Description | Default | Environment-Specific |
| --- | --- | --- | --- |
| `ProbotSharp:Resilience:CircuitBreaker:FailureRatio` | Failure ratio threshold to open circuit (0.0-1.0) | `0.5` (50%) | No |
| `ProbotSharp:Resilience:CircuitBreaker:MinimumThroughput` | Minimum requests before applying failure ratio | `5` | Yes - `5` in dev, `10` in prod |
| `ProbotSharp:Resilience:CircuitBreaker:BreakDurationSeconds` | How long circuit stays open before retry (seconds) | `30` | No |
| `ProbotSharp:Resilience:CircuitBreaker:SamplingDurationSeconds` | Time window for failure rate calculation (seconds) | `60` | No |

**Circuit Breaker States:**
- **Closed:** Normal operation, requests pass through
- **Open:** Too many failures detected, requests immediately fail
- **Half-Open:** After break duration, allows test requests to check if service recovered

**Retry Policy:**

Retry policy with exponential backoff for transient HTTP failures (5xx errors, timeouts).

| Setting | Description | Default |
| --- | --- | --- |
| `ProbotSharp:Resilience:Retry:MaxRetryAttempts` | Maximum retry attempts for failed requests | `3` |
| `ProbotSharp:Resilience:Retry:BaseDelaySeconds` | Base delay between retries (exponential backoff) | `2` |
| `ProbotSharp:Resilience:Retry:MaxDelaySeconds` | Maximum delay cap between retries | `30` |
| `ProbotSharp:Resilience:Retry:UseJitter` | Add random jitter to retry delays (prevents thundering herd) | `true` |

**Retry Behavior Example:**
- Attempt 1: 2 seconds + jitter
- Attempt 2: 4 seconds + jitter
- Attempt 3: 8 seconds + jitter (capped at MaxDelaySeconds)

#### Feature Flags

Runtime feature toggles to enable/disable specific functionality without code changes.

| Setting | Description | Default | Environment-Specific |
| --- | --- | --- | --- |
| `ProbotSharp:Features:EnableWebhookReplay` | Enable WebhookReplayWorker background service | `true` | No - usually `true` |
| `ProbotSharp:Features:EnableMetrics` | Enable metrics collection and export | `false` | Yes - `false` in dev, `true` in prod/staging |
| `ProbotSharp:Features:EnableHealthChecks` | Enable health check endpoints | `true` | No - usually `true` |
| `ProbotSharp:Features:EnableDetailedErrors` | Include detailed error messages and stack traces in API responses | `false` | Yes - `false` in prod, `true` in dev/staging |

**Feature Flag Usage:**
- **EnableWebhookReplay:** Disabling stops the background worker from processing replay queue. Webhooks continue to be enqueued but not processed.
- **EnableMetrics:** Disabling uses no-op metrics adapter (zero overhead). Enable in production for observability.
- **EnableHealthChecks:** Should always be enabled for container orchestration (Kubernetes liveness/readiness probes).
- **EnableDetailedErrors:** Disable in production to prevent exposing sensitive error details to external callers.

#### Logging Configuration (Serilog)

Configuration for Serilog logging. Logs are written to both console and file with structured output.

| Setting | Description | Default | Environment-Specific |
| --- | --- | --- | --- |
| `Logging:LogLevel:Default` | Default log level for all namespaces | `Information` | No |
| `Logging:LogLevel:Microsoft.AspNetCore` | ASP.NET Core framework log level | `Warning` | No |
| `Logging:LogLevel:Microsoft.EntityFrameworkCore` | Entity Framework Core log level | `Warning` | Yes - `Information` in dev for SQL logging |
| `Logging:LogLevel:ProbotSharp` | Application-specific log level | `Debug` | Yes - `Debug` in dev/staging, `Information` in prod |

**Serilog Configuration (hardcoded in Program.cs):**
- **Console Sink:** Structured output with timestamp, level, source context, message, and exception
- **File Sink:** Daily rolling logs with 30-day retention
- **Log File Path:** `logs/probotsharp-{Date}.log` (e.g., `logs/probotsharp-20251004.log`)
- **Output Template:** `[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}`
- **Enrichment:** All logs enriched with `Application=ProbotSharp` property

**Log Levels:**
- **Debug:** Detailed diagnostic information (dev/staging only)
- **Information:** General informational messages (production default)
- **Warning:** Potentially harmful situations (e.g., Redis unavailable, fallback to in-memory)
- **Error:** Error events that might still allow application to continue
- **Fatal:** Very severe errors that cause application termination

**Special Logging:**
- **EF Core SQL Commands:** Set `Microsoft.EntityFrameworkCore.Database.Command` to `Information` to log SQL queries
- **Sensitive Data Logging:** Controlled by `ProbotSharp:Database:EnableSensitiveDataLogging` (not via log level)

### Environment Variable Mapping

Environment variables override appsettings.json values using the standard double-underscore separator format: `ProbotSharp__GitHub__AppId`.

For complete adapter configuration documentation, see [Adapter Configuration](AdapterConfiguration.md).

#### GitHub Configuration
| appsettings.json Path | Environment Variable |
| --- | --- |
| `ProbotSharp:GitHub:AppId` | `ProbotSharp__GitHub__AppId` |
| `ProbotSharp:GitHub:WebhookSecret` | `ProbotSharp__GitHub__WebhookSecret` |
| `ProbotSharp:GitHub:PrivateKey` | `ProbotSharp__GitHub__PrivateKey` |
| `ProbotSharp:GitHub:ClientId` | `ProbotSharp__GitHub__ClientId` |
| `ProbotSharp:GitHub:ClientSecret` | `ProbotSharp__GitHub__ClientSecret` |

#### Database Configuration
| appsettings.json Path | Environment Variable |
| --- | --- |
| `ConnectionStrings:ProbotSharp` | `ConnectionStrings__ProbotSharp` |
| `ProbotSharp:Database:Provider` | `ProbotSharp__Database__Provider` |
| `ProbotSharp:Database:EnableSensitiveDataLogging` | `ProbotSharp__Database__EnableSensitiveDataLogging` |
| `ProbotSharp:Database:EnableDetailedErrors` | `ProbotSharp__Database__EnableDetailedErrors` |
| `ProbotSharp:Database:CommandTimeout` | `ProbotSharp__Database__CommandTimeout` |
| `ProbotSharp:Database:MaxRetryCount` | `ProbotSharp__Database__MaxRetryCount` |
| `ProbotSharp:Database:MaxRetryDelay` | `ProbotSharp__Database__MaxRetryDelay` |

#### Adapter Configuration
| appsettings.json Path | Environment Variable |
| --- | --- |
| `ProbotSharp:Adapters:Cache:Provider` | `ProbotSharp__Adapters__Cache__Provider` |
| `ProbotSharp:Adapters:Cache:Options:*` | `ProbotSharp__Adapters__Cache__Options__*` |
| `ProbotSharp:Adapters:Idempotency:Provider` | `ProbotSharp__Adapters__Idempotency__Provider` |
| `ProbotSharp:Adapters:Idempotency:Options:*` | `ProbotSharp__Adapters__Idempotency__Options__*` |
| `ProbotSharp:Adapters:Persistence:Provider` | `ProbotSharp__Adapters__Persistence__Provider` |
| `ProbotSharp:Adapters:ReplayQueue:Provider` | `ProbotSharp__Adapters__ReplayQueue__Provider` |
| `ProbotSharp:Adapters:DeadLetterQueue:Provider` | `ProbotSharp__Adapters__DeadLetterQueue__Provider` |
| `ProbotSharp:Adapters:Metrics:Provider` | `ProbotSharp__Adapters__Metrics__Provider` |
| `ProbotSharp:Adapters:Tracing:Provider` | `ProbotSharp__Adapters__Tracing__Provider` |

#### Resilience Configuration
| appsettings.json Path | Environment Variable |
| --- | --- |
| `ProbotSharp:Resilience:HttpTimeout:TimeoutSeconds` | `ProbotSharp__Resilience__HttpTimeout__TimeoutSeconds` |
| `ProbotSharp:Resilience:CircuitBreaker:FailureRatio` | `ProbotSharp__Resilience__CircuitBreaker__FailureRatio` |
| `ProbotSharp:Resilience:CircuitBreaker:MinimumThroughput` | `ProbotSharp__Resilience__CircuitBreaker__MinimumThroughput` |
| `ProbotSharp:Resilience:CircuitBreaker:BreakDurationSeconds` | `ProbotSharp__Resilience__CircuitBreaker__BreakDurationSeconds` |
| `ProbotSharp:Resilience:CircuitBreaker:SamplingDurationSeconds` | `ProbotSharp__Resilience__CircuitBreaker__SamplingDurationSeconds` |
| `ProbotSharp:Resilience:Retry:MaxRetryAttempts` | `ProbotSharp__Resilience__Retry__MaxRetryAttempts` |
| `ProbotSharp:Resilience:Retry:BaseDelaySeconds` | `ProbotSharp__Resilience__Retry__BaseDelaySeconds` |
| `ProbotSharp:Resilience:Retry:MaxDelaySeconds` | `ProbotSharp__Resilience__Retry__MaxDelaySeconds` |
| `ProbotSharp:Resilience:Retry:UseJitter` | `ProbotSharp__Resilience__Retry__UseJitter` |

#### Feature Flags
| appsettings.json Path | Environment Variable |
| --- | --- |
| `ProbotSharp:Features:EnableWebhookReplay` | `ProbotSharp__Features__EnableWebhookReplay` |
| `ProbotSharp:Features:EnableMetrics` | `ProbotSharp__Features__EnableMetrics` |
| `ProbotSharp:Features:EnableHealthChecks` | `ProbotSharp__Features__EnableHealthChecks` |
| `ProbotSharp:Features:EnableDetailedErrors` | `ProbotSharp__Features__EnableDetailedErrors` |

#### Logging Configuration
| appsettings.json Path | Environment Variable |
| --- | --- |
| `Logging:LogLevel:Default` | `Logging__LogLevel__Default` |
| `Logging:LogLevel:Microsoft.AspNetCore` | `Logging__LogLevel__Microsoft.AspNetCore` |
| `Logging:LogLevel:Microsoft.EntityFrameworkCore` | `Logging__LogLevel__Microsoft.EntityFrameworkCore` |
| `Logging:LogLevel:ProbotSharp` | `Logging__LogLevel__ProbotSharp` |

### Configuration Validation

ProbotSharp validates required configuration on startup via `ConfigurationValidation.ValidateRequiredConfiguration()`. The following settings are checked:

**Always Required:**
- `ProbotSharp:GitHub:AppId`
- `ProbotSharp:GitHub:WebhookSecret`
- `ProbotSharp:GitHub:PrivateKey`
- `ConnectionStrings:ProbotSharp`

**Conditionally Required:**
- Provider-specific configuration options based on selected adapters (see [Adapter Configuration](AdapterConfiguration.md))

**Validation Bypass:**
Set `ProbotSharp:SkipConfigurationValidation=true` to skip validation (useful for tests).

### Configuration Files by Environment

**Development** (`appsettings.Development.json`):
- Uses `development-secret-change-in-production` as default webhook secret
- Enables Entity Framework command logging
- Debug-level logging for ProbotSharp namespace
- In-memory adapters for simple local development

**Staging** (`appsettings.Staging.json`):
- Debug logging enabled
- Redis adapters for cache and idempotency
- OpenTelemetry metrics and tracing enabled
- Detailed errors enabled for troubleshooting
- Database adapter for dead letter queue

**Production** (`appsettings.Production.json`):
- Information-level logging
- Redis adapters for cache and idempotency
- OpenTelemetry metrics and tracing enabled
- Detailed errors disabled
- Higher circuit breaker threshold (MinimumThroughput: 10)
- Database adapter for dead letter queue
- Increased replay retry attempts (5)

### Configuration Summary by Environment

Quick reference table showing key configuration differences across environments:

| Setting | Development | Staging | Production |
| --- | --- | --- | --- |
| **Log Level** | Debug | Debug | Information |
| **EF Core Logging** | Information (SQL) | Information | Warning |
| **Detailed Errors** | Enabled | Enabled | Disabled |
| **Sensitive Data Logging** | Disabled | Enabled | Disabled |
| **Cache Adapter** | InMemory | Redis | Redis |
| **Idempotency Adapter** | Database | Redis | Redis |
| **Replay Queue Adapter** | InMemory | InMemory/FileSystem | InMemory/FileSystem |
| **Dead Letter Queue Adapter** | InMemory | Database | Database |
| **Metrics Adapter** | NoOp | OpenTelemetry | OpenTelemetry |
| **Tracing Adapter** | NoOp | OpenTelemetry | OpenTelemetry |
| **Replay Max Retries** | 3 | 3 | 5 |
| **Replay Base Delay** | 1000ms | 1000ms | 2000ms |
| **Circuit Breaker Min Throughput** | 5 | 5 | 10 |
| **Webhook Secret** | `development-secret-change-in-production` | (from env) | (from env) |

**Note:** All credential values (AppId, PrivateKey, connection strings) must be provided via environment variables in all environments.

## Observability

### Health Checks

ProbotSharp includes comprehensive health checks accessible at `GET /health`:

**Implemented Health Checks:**
- **Database** (`database`) - Verifies PostgreSQL connectivity and query execution
  - Status: Unhealthy if database is unreachable
  - Returns: Provider name, masked connection string
- **Cache** (`cache`) - Tests Redis or in-memory cache accessibility
  - Status: Degraded if cache is unavailable (application can still function)
  - Returns: Cache type, test result
- **GitHub API** (`github_api`) - Checks GitHub API reachability via `/meta` endpoint
  - Status: Degraded if GitHub API is down (webhooks are queued for replay)
  - Returns: Status code, endpoint, response time

**Health Check Response Format:**
```json
{
  "status": "Healthy",
  "timestamp": "2025-10-04T12:00:00Z",
  "duration": 145.23,
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "Database is accessible",
      "duration": 23.45,
      "data": {
        "database_provider": "Npgsql.EntityFrameworkCore.PostgreSQL",
        "connection_string": "Host=localhost; Port=5432; Database=probotsharp; Username=probotsharp; Password=***"
      }
    }
  ]
}
```

**Health Check Tags:**
- `db`, `sql`, `ready` - Database checks
- `cache`, `ready` - Cache checks
- `external`, `github`, `ready` - GitHub API checks

**Kubernetes/Docker Integration:**
Use `/health` endpoint for both liveness and readiness probes.

### Logging

**Serilog Configuration:**
- Console sink with structured output template: `[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}`
- File sink with daily rolling logs
  - Path: `logs/probotsharp-YYYYMMDD.log`
  - Retention: 30 days
  - Same structured format as console
- All logs include application context property: `Application=ProbotSharp`

**Log Levels by Environment:**
- Development: Debug for ProbotSharp, Warning for ASP.NET Core/EF Core
- Staging: Debug for ProbotSharp, Information for framework components
- Production: Information for ProbotSharp, Warning for framework components

**Key Log Events:**
- Webhook reception and signature validation
- Idempotency key checks (duplicate detection)
- Replay queue enqueue/dequeue operations
- Dead-letter queue entries
- GitHub API calls and rate limit headers
- Database connection issues
- Redis connectivity problems

**Correlation IDs:**
Each request is assigned a correlation ID via middleware for tracing requests across logs.

### Metrics & Tracing

**OpenTelemetry Integration:**
- Enable with `ProbotSharp:Adapters:Metrics:Provider=OpenTelemetry`
- Configure OTLP endpoint: `ProbotSharp:Metrics:OtlpEndpoint`
- Meter name: `ProbotSharp` (configurable)
- Version tracking for metrics schema changes

**Available Metrics:**
- Webhook processing duration and count
- Replay queue depth and processing time
- Dead-letter queue item count
- Cache hit/miss ratios
- GitHub API call counts and latencies
- Circuit breaker state changes

**Distributed Tracing:**
- Activity source name: `ProbotSharp` (configurable via `Tracing:SourceName`)
- Traces webhook processing pipeline
- Includes GitHub API calls
- Database operations
- Cache operations

**Disable Metrics:**
Set `ProbotSharp:Adapters:Metrics:Provider=NoOp` to use no-op adapter (zero overhead).

## Troubleshooting Guide

### Database Connection Failures

**Symptom:** Application fails to start or `/health` returns unhealthy for database check.

**Common Causes:**
1. **Invalid connection string**
   - Check `ConnectionStrings:ProbotSharp` in appsettings or `ConnectionStrings__ProbotSharp` environment variable
   - Verify host, port, database name, username, password
   - For Docker Compose: use service name (`postgres`) not `localhost`

2. **Database not ready**
   - PostgreSQL container may not be fully initialized
   - Solution: Use health checks in docker-compose.yml dependencies
   - Wait for `pg_isready` to succeed before starting API

3. **Network connectivity**
   - Verify PostgreSQL is listening on expected port (default 5432)
   - Check firewall rules and security groups
   - Test connection: `psql -h localhost -p 5432 -U probotsharp -d probotsharp`

4. **Authentication failure**
   - Verify username/password match PostgreSQL configuration
   - Check PostgreSQL `pg_hba.conf` for authentication method
   - Ensure user has necessary permissions

**Resolution:**
```bash
# Test database connectivity
docker exec probotsharp-postgres pg_isready -U probotsharp

# Check PostgreSQL logs
docker logs probotsharp-postgres

# Verify connection string
echo $ConnectionStrings__ProbotSharp
```

### Redis Connection Failures

**Symptom:** Application degrades to in-memory cache or idempotency fails.

**Common Causes:**
1. **Redis not accessible**
   - Verify Redis is running: `docker ps | grep redis`
   - Test connectivity: `redis-cli -h localhost -p 6379 ping`
   - Expected response: `PONG`

2. **Invalid connection string**
   - Check `ProbotSharp:Cache:RedisConnectionString` or `ProbotSharp:Idempotency:RedisConnectionString`
   - Format: `host:port` or `host:port,password=secret`
   - For Docker Compose: use service name (`redis:6379`)

3. **Redis authentication required**
   - If Redis has `requirepass` enabled, include password in connection string
   - Format: `redis:6379,password=your-redis-password`

**Resolution:**
```bash
# Test Redis connectivity
docker exec probotsharp-redis redis-cli ping

# Check Redis logs
docker logs probotsharp-redis

# Verify connection string
echo $PROBOTSHARP_CACHE_REDIS_CONNECTION
```

**Fallback Behavior:**
- Cache: Falls back to in-memory cache (works but not shared across instances)
- Idempotency: Falls back to database-backed idempotency

### GitHub API Authentication Errors

**Symptom:** 401 Unauthorized errors in logs when calling GitHub API.

**Common Causes:**
1. **Missing or invalid App ID**
   - Verify `ProbotSharp:GitHub:AppId` matches your GitHub App settings
   - Must be numeric (e.g., `123456`)

2. **Invalid private key**
   - Check `ProbotSharp:GitHub:PrivateKey` configuration
   - If using file path, ensure file exists and is mounted in container
   - If using Base64, ensure entire key is encoded correctly
   - Private key must be in PEM format starting with `-----BEGIN RSA PRIVATE KEY-----`

3. **Private key file permissions**
   - In Docker: mount as read-only (`:ro`)
   - Ensure application has read permissions
   - Check file exists: `docker exec probotsharp-api ls -la /app/private-key.pem`

**Resolution:**
```bash
# Verify App ID
echo $PROBOTSHARP_GITHUB_APPID

# Check private key file exists (Docker)
docker exec probotsharp-api test -f /app/private-key.pem && echo "exists" || echo "missing"

# Verify private key format
head -1 /path/to/private-key.pem
# Should output: -----BEGIN RSA PRIVATE KEY-----

# Test GitHub App configuration
curl -i https://api.github.com/app \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Webhook Signature Validation Failures

**Symptom:** `POST /webhooks` returns 401 Unauthorized or logs show signature validation failures.

**Common Causes:**
1. **Webhook secret mismatch**
   - Verify `ProbotSharp:GitHub:WebhookSecret` matches GitHub App webhook secret
   - GitHub App Settings > Webhook secret must match exactly
   - Secret is case-sensitive

2. **Using development secret in production**
   - Default `development-secret-change-in-production` should be changed
   - Generate secure random secret for production

3. **Payload encoding issues**
   - ProbotSharp validates using raw request body
   - Middleware must preserve exact payload for signature calculation

**Resolution:**
```bash
# Verify webhook secret is set
echo $PROBOTSHARP_GITHUB_WEBHOOKSECRET

# Test webhook locally with smee.io
docker-compose --profile smee up

# Check recent webhook deliveries in GitHub App settings
# Look for 401 responses and validation errors
```

### GitHub API Rate Limiting

**Symptom:** 403 Forbidden errors with `X-RateLimit-Remaining: 0` in logs.

**Common Causes:**
1. **Too many API calls**
   - GitHub Apps have 5,000 requests/hour per installation
   - Check `X-RateLimit-Remaining` and `X-RateLimit-Reset` headers in logs

2. **Not using installation tokens**
   - Installation tokens have higher rate limits than personal access tokens
   - Verify application is using `InstallationAccessToken` not PAT

**Resolution:**
- Implement caching for frequently accessed data
- Use GraphQL API for batch operations (reduces request count)
- Monitor rate limit headers and implement backoff
- Cache access tokens (default 50 minutes, GitHub expires at 60 minutes)

**Rate Limit Monitoring:**
```bash
# Check current rate limit (requires valid token)
curl -H "Authorization: Bearer $GITHUB_TOKEN" \
  https://api.github.com/rate_limit
```

### Memory and CPU Issues

**Symptom:** Application crashes, OOMKilled in Kubernetes, or high CPU usage.

**Common Causes:**
1. **Insufficient memory allocation**
   - Default .NET GC uses available memory
   - Container without limits may assume more memory than available
   - Set memory limits in docker-compose.yml or Kubernetes

2. **Replay queue backlog**
   - Large number of failed webhooks in replay queue
   - Each retry consumes resources
   - Check `/webhooks/dlq/stats` for dead-letter queue depth

3. **Database connection pool exhaustion**
   - Too many concurrent webhook processes
   - Default max pool size: 100
   - Adjust `ConnectionString` with `Maximum Pool Size=N`

4. **File-system queue growth**
   - Replay queue directory growing unbounded
   - Monitor disk usage on replay queue path
   - Dead-letter queue retention not being enforced

**Resolution:**
```bash
# Monitor container memory usage
docker stats probotsharp-api

# Check replay queue depth
curl http://localhost:8080/webhooks/dlq/stats

# Clean up old dead-letter items manually
find /app/dead-letter-queue -mtime +30 -delete

# Set memory limits (docker-compose.yml)
# deploy:
#   resources:
#     limits:
#       memory: 512M
#     reservations:
#       memory: 256M
```

### Webhook Replay Queue Stuck

**Symptom:** Webhooks not being replayed, queue depth increasing.

**Common Causes:**
1. **WebhookReplayWorker not running**
   - Check hosted service started: look for startup logs
   - Verify `ProbotSharp:Features:EnableWebhookReplay=true`

2. **All retries exhausted**
   - Webhooks moved to dead-letter queue after max retries
   - Check `/webhooks/dlq/stats` endpoint

3. **Persistent failures**
   - Upstream service (database, GitHub API) unavailable
   - Circuit breaker opened, preventing retries
   - Check health endpoint for service status

**Resolution:**
```bash
# Check dead-letter queue
curl http://localhost:8080/webhooks/dlq/stats | jq .

# Manually replay webhook by delivery ID
curl -X POST http://localhost:8080/webhooks/replay/{deliveryId}

# Check worker logs
docker logs probotsharp-api | grep WebhookReplayWorker

# Verify queue configuration
echo $PROBOTSHARP_REPLAY_MAX_RETRIES
echo $PROBOTSHARP_REPLAY_RETRY_DELAY
```

## Scaling Considerations

### Horizontal Scaling

**Stateless Design:**
- ProbotSharp API is designed for horizontal scaling
- No in-process state shared between instances
- All state stored in PostgreSQL or Redis

**Requirements for Multi-Instance Deployment:**
1. **Redis cache adapter** - Required for shared access token cache
   - Memory adapter is per-instance only
   - Access tokens must be shared across instances

2. **Redis or Database idempotency adapter** - Required for duplicate detection
   - Redis provides better performance
   - Database works but has higher latency

3. **Shared PostgreSQL database**
   - All instances connect to same database
   - Use connection pooling (default: 100 connections per instance)
   - Monitor total connections: `SELECT count(*) FROM pg_stat_activity;`

4. **Replay queue considerations**
   - FileSystem adapter NOT suitable for multi-instance (file locking issues)
   - Use InMemory adapter (queued items lost on restart)
   - Future: Use external queue adapter (SQS, Azure Service Bus) for multi-instance

5. **Dead letter queue**
   - Database adapter recommended for multi-instance
   - FileSystem adapter NOT suitable for multi-instance

**Load Balancer Configuration:**
- Use round-robin or least-connections algorithm
- Health check: `GET /health`
- Sticky sessions NOT required
- Webhook signature validation handles replay attacks via idempotency

**Example Kubernetes Deployment:**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: probotsharp-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: probotsharp-api
  template:
    spec:
      containers:
      - name: api
        image: probotsharp-api:latest
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 5
```

### Queue Depth Monitoring

**Metrics to Track:**
- Replay queue depth (items waiting to be processed)
- Dead-letter queue depth (permanently failed items)
- Replay processing rate (items/second)
- Average retry count before success

**Alerting Thresholds:**
- Replay queue depth > 100 items (scale up workers)
- Dead-letter queue growth rate > 10 items/hour (investigate failures)
- Replay processing time > 5 seconds (check GitHub API latency)

**Monitoring Endpoints:**
```bash
# Dead-letter queue stats
curl http://localhost:8080/webhooks/dlq/stats

# Health check includes queue metrics (future enhancement)
curl http://localhost:8080/health
```

### Database Connection Pooling

**Default Settings:**
- Default max pool size: 100 connections
- Min pool size: 0 (connections created on demand)
- Connection lifetime: unlimited
- Connection timeout: 15 seconds

**Production Recommendations:**
1. **Calculate max connections needed:**
   - Formula: `(Instances × MaxPoolSize) < PostgreSQL max_connections`
   - Default PostgreSQL max_connections: 100
   - Example: 3 instances × 30 connections = 90 total (safe)

2. **Connection string tuning:**
   ```
   Host=postgres;Database=probotsharp;Username=user;Password=pass;
   Maximum Pool Size=30;Minimum Pool Size=5;Connection Lifetime=300
   ```

3. **Monitor connection usage:**
   ```sql
   -- Check active connections
   SELECT count(*), state FROM pg_stat_activity
   GROUP BY state;

   -- Check connections by application
   SELECT application_name, count(*)
   FROM pg_stat_activity
   GROUP BY application_name;
   ```

### Redis Caching Strategy

**Cache Layers:**
1. **Access tokens** (50 minute TTL)
   - GitHub tokens valid for 60 minutes
   - Cache invalidation: automatic via TTL
   - Key pattern: `access_token:installation:{id}`

2. **Idempotency keys** (24 hour TTL)
   - Prevents duplicate webhook processing
   - Key pattern: `idempotency:{delivery_id}`
   - Cleanup: automatic via Redis TTL

**Memory Estimation:**
- Access token: ~2KB per installation
- Idempotency key: ~500 bytes per webhook
- Example: 100 installations + 10K webhooks/day = 200KB + 5MB = ~5.2MB

**Redis Configuration:**
- Eviction policy: `allkeys-lru` (least recently used)
- Max memory: 256MB recommended minimum
- Persistence: Optional (can rebuild cache from source)

## Monitoring and Alerting

### Key Metrics to Monitor

**Application Health:**
- `/health` endpoint response time and status
- Uptime percentage
- Request rate (webhooks/minute)
- Response time P50, P95, P99

**Resource Utilization:**
- CPU usage (target: < 70% average)
- Memory usage (target: < 80% of limit)
- Disk usage for logs and queues (target: < 80%)
- Network I/O

**Database Metrics:**
- Connection pool utilization (target: < 80%)
- Query duration P95 (target: < 100ms)
- Active connections count
- Deadlocks and lock timeouts

**Redis Metrics:**
- Memory usage (target: < 80% of max memory)
- Hit rate (target: > 90% for access tokens)
- Connected clients
- Evicted keys count

**GitHub API:**
- Rate limit remaining (alert if < 500)
- API call latency P95
- 4xx/5xx error rate
- Circuit breaker state

**Queue Metrics:**
- Replay queue depth (alert if > 100)
- Dead-letter queue depth (alert if > 50)
- Average processing time
- Retry attempts distribution

**Idempotency Metrics:**
- Idempotency hits (duplicate deliveries blocked)
- Idempotency misses (unique deliveries processed)
- Idempotency errors (lock acquisition failures)
- Duplicate delivery rate (hits / total deliveries)

> **Note:** High duplicate rates may indicate GitHub infrastructure issues, webhook delivery retries (normal for failures), or load balancer misconfiguration. Duplicates are expected when GitHub retries failed webhook deliveries (status codes 500, 503), during network timeouts, or after application restarts during request handling.

### Alert Thresholds

| Metric | Warning | Critical |
| --- | --- | --- |
| Database health | Degraded | Unhealthy |
| Redis health | Degraded | N/A (optional) |
| GitHub API health | Degraded | N/A (external) |
| CPU usage | > 70% | > 90% |
| Memory usage | > 80% | > 95% |
| Disk usage | > 80% | > 90% |
| Replay queue depth | > 50 items | > 100 items |
| Dead-letter queue growth | > 10/hour | > 50/hour |
| GitHub rate limit | < 500 remaining | < 100 remaining |
| Error rate | > 1% | > 5% |

### Log Aggregation Setup

**Recommended Stack:**
1. **ELK Stack** (Elasticsearch, Logstash, Kibana)
   - Collect logs from all instances
   - Index by timestamp and correlation ID
   - Create dashboards for webhook processing pipeline

2. **Cloud Options:**
   - AWS CloudWatch Logs
   - Azure Application Insights
   - Google Cloud Logging
   - Datadog APM

**Structured Logging:**
- All logs are in structured format via Serilog
- JSON output available via Serilog.Sinks.Console (JSON formatter)
- Fields: `Timestamp`, `Level`, `SourceContext`, `Message`, `Exception`, `CorrelationId`

**Log Queries:**
```
# Find all webhook processing for specific delivery ID
CorrelationId:"d1234567-890a-bcde-f012-3456789abcde"

# Find all signature validation failures
Message:"Signature validation failed"

# Find all GitHub API rate limit warnings
Message:"rate limit" AND Level:Warning
```

### Deployment Process

**Docker Compose (Local Development):**
See `docker-compose.yml` in project root.

**Cloud Deployments:**
- **AWS ECS:** See `deploy/aws/README.md`
- **Kubernetes:** See `deploy/k8s/README.md`
- **Azure Web Apps:** See `deploy/azure/README.md`

## Deployment Considerations

### Containers

- Multi-stage Dockerfiles ship for both API (`src/ProbotSharp.Bootstrap.Api/Dockerfile`) and CLI (`src/ProbotSharp.Bootstrap.Cli/Dockerfile`).
- Images expose the default ASP.NET ports (8080/8443). Provide configuration via environment variables or bind-mounted `appsettings.*`.
- Health check endpoint: `GET /health` for container orchestration

### Docker Compose

**Local Development Setup:**
```bash
# Copy environment file
cp .env.example .env

# Edit .env with your GitHub App credentials
# - PROBOTSHARP_GITHUB_APPID
# - PROBOTSHARP_GITHUB_WEBHOOKSECRET
# - PROBOTSHARP_GITHUB_PRIVATEKEY (or mount private-key.pem)

# Start all services
docker-compose up -d

# View logs
docker-compose logs -f probotsharp-api

# Check health
curl http://localhost:8080/health

# Stop all services
docker-compose down
```

**Services Included:**
- PostgreSQL 16 with persistent volume
- Redis 7 with persistent volume
- ProbotSharp API with health checks
- Smee.io webhook proxy (optional, use `--profile smee`)

### CI/CD

- `.github/workflows/dotnet.yml` runs restore, build, tests, and publishes coverage to Codecov (uses `CODECOV_TOKEN`).
- Future work: add image publish jobs and deployment smoke tests once target environment is defined.

## Maintenance Checklist

### Regular Maintenance

**Daily:**
- Monitor health check status (`/health`)
- Check dead-letter queue depth (`/webhooks/dlq/stats`)
- Review error logs for recurring issues
- Verify GitHub API rate limit headroom

**Weekly:**
- Review replay queue patterns (peak times, common failures)
- Analyze dead-letter queue items for trends
- Check database connection pool utilization
- Review Redis memory usage and eviction rates
- Monitor disk usage for logs and queues

**Monthly:**
- Review and rotate logs (automatic with Serilog 30-day retention)
- Update dependencies and security patches
- Review and tune resilience policies (retry counts, timeouts)
- Analyze performance metrics and optimize queries
- Review alert thresholds and adjust if needed

### Documentation Updates

- Document adapter-specific behaviors as production implementations are completed
- Update this Operations guide when configuration options change

### Performance Monitoring

- Monitor coverage metrics
- Track webhook processing latency trends
- Monitor GitHub API call patterns and optimize where possible
- Review database query performance (use EF Core logging)

### Security

- Rotate webhook secrets periodically
- Review and update GitHub App private key if compromised
- Monitor for suspicious webhook patterns
- Keep dependencies updated (especially security-related packages)
- Review access logs for unauthorized access attempts

### Capacity Planning

- Monitor webhook volume trends
- Plan for scaling before hitting 70% CPU/memory utilization
- Review database growth and plan for archival/cleanup
- Monitor Redis memory usage and plan for upgrade if needed
- Review queue depth trends and plan for external queue migration if needed

## Current Implementation Status

**Production-Ready Components:**
- PostgreSQL persistence with Entity Framework Core
- Redis caching for access tokens and idempotency
- File-system replay queue for single-instance deployments
- Comprehensive health checks (database, cache, GitHub API)
- Serilog logging with console and file sinks
- OpenTelemetry metrics and tracing support
- Polly resilience policies (retry, circuit breaker, timeout)
- Idempotency middleware for duplicate webhook prevention
- Dead-letter queue for permanently failed webhooks

**Development/Testing Components:**
- In-memory cache adapter
- In-memory replay queue adapter
- Database-backed idempotency adapter

**Future Enhancements:**
- External queue providers (AWS SQS, Azure Service Bus)
- Additional health check providers
- More advanced log aggregation integration
- Automated dead-letter queue replay with admin UI
- Enhanced metrics dashboards and alerting
- SQLite support for development environments
