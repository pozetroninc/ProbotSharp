# Probot-Sharp

[![Build Status](https://img.shields.io/github/actions/workflow/status/your-org/probot-sharp/ci.yml?branch=main)](https://github.com/your-org/probot-sharp/actions)
[![Coverage](https://img.shields.io/codecov/c/github/your-org/probot-sharp)](https://codecov.io/gh/your-org/probot-sharp)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download)
[![Docker](https://img.shields.io/badge/Docker-Ready-blue.svg)](https://hub.docker.com/r/your-org/probot-sharp)

Probot-Sharp is a .NET 8 implementation insipred by the popular [Probot](https://github.com/probot/probot) GitHub App framework. It mirrors the original project's developer experience while embracing a hexagonal (ports & adapters) architecture to keep domain logic isolated from runtime concerns.

We really like Probot and have tried to make a C# version which will be familiar to Probot developers. This includes adopting the same best practices (and making some of the same jokes). If you find this on the nose please note the offense is not intended.

All product and company names are trademarks or registered trademarks of their respective holders. Use of them does not imply any affiliation with or endorsement by them. THIS PROJECT IS IN NO WAY AFFILIATED WITH [PROBOT](https://github.com/probot/probot)

Note: Code samples in the documentation marked as `csharp` are automatically tested as part of our test suite. Code samples marked as `text` are not automatically tested, so please verify them manually if you use them.

## Table of Contents
- [Quick Start](#quick-start)
  - [Docker Compose (Running the Framework)](#docker-compose-running-the-framework)
  - [Local .NET Development](#local-net-development)
- [What's Different from Probot (Node.js)](#whats-different-from-probot-nodejs)
- [Repository Layout](#repository-layout)
- [Architecture Overview](#architecture-overview)
- [Development Workflow](#development-workflow)
- [Testing & Coverage](#testing--coverage)
- [Deployment](#deployment)
- [Documentation Index](#documentation-index)
 - [Probot ↔ Probot Sharp Guide](#probot--probot-sharp-guide)

## Quick Start

### Philosophy: Start Simple, Scale Later

ProbotSharp embraces the Probot philosophy: **don't use a database unless you need it**. Start with zero infrastructure and scale up incrementally as your bot grows.

### Option 1: Minimal Setup (No Infrastructure Required)

The absolute fastest way to get started - no Docker, no databases, just run:

```bash
# Clone the repository
git clone https://github.com/your-org/probot-sharp.git
cd probot-sharp

# Install the template
dotnet new install ./templates

# Create a new bot with minimal configuration
dotnet new probotsharp-app -n MyBot -o mybot
cd mybot

# Create minimal appsettings.json (in-memory mode)
cat > appsettings.json << 'EOF'
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
    "AppId": "YOUR_GITHUB_APP_ID",
    "WebhookSecret": "development",
    "PrivateKeyPath": "private-key.pem",

    "Adapters": {
      "Cache": {
        "Provider": "InMemory",
        "Options": {
          "ExpirationMinutes": "60"
        }
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
          "RetryBaseDelayMs": "1000"
        }
      },
      "Metrics": {
        "Provider": "NoOp",
        "Options": {}
      }
    }
  }
}
EOF

# Get your GitHub App credentials from https://github.com/settings/apps/new
# Download private-key.pem to this directory

# Run with zero infrastructure dependencies
dotnet run
```

Your bot is now running at http://localhost:5000 - no Docker, no databases!

**Perfect for:**
- Learning ProbotSharp (5-minute setup)
- Simple automation bots (auto-labelers, welcome bots)
- Prototypes and proof-of-concepts
- Low-traffic deployments (< 100 webhooks/hour)

See [Minimal Deployment Guide](docs/MinimalDeployment.md) for deployment options without infrastructure.

### Option 2: Create a New Bot (Recommended for Production)

For production bots that need persistence and scaling:

```bash
# Install the template (for in-repo development)
dotnet new install ./templates

# Create a new bot
dotnet new probotsharp-app -n MyAwesomeBot -o my-awesome-bot

# Configure your bot
cd my-awesome-bot
cp .env.example .env
# Edit .env with your GitHub App credentials (APP_ID, WEBHOOK_SECRET, PRIVATE_KEY)

# Run your bot
dotnet run
```

The generated bot includes:
- Sample event handler for `issues.opened`
- Complete project structure with best practices
- Environment configuration template
- Comprehensive README with usage examples

**Next steps:**
- [Create a GitHub App](https://github.com/settings/apps/new) and get your credentials
- Add the credentials to `.env`
- Add event handlers to respond to GitHub webhooks
- Follow [Best Practices](docs/BestPractices.md) when building your app
- Test locally using the `receive` command (see [docs/LocalDevelopment.md](docs/LocalDevelopment.md))
- Review with the [Pre-Release Checklist](docs/BestPractices-Checklist.md) before deployment

### Scaling Up

Start minimal and scale incrementally:

1. **Minimal** (In-memory) - No infrastructure, perfect for learning
2. **File-based Queue** - Add replay queue persistence
3. **SQLite** - Add database for webhook history
4. **PostgreSQL** - Upgrade to production database
5. **PostgreSQL + Redis** - Full infrastructure for high-traffic

See [Deployment Guide](docs/Deployment.md) for migration paths.

### Docker Compose (Running the Framework)

To run the Probot Sharp framework itself with Docker Compose:

```bash
# Clone the repository
git clone https://github.com/your-org/probot-sharp.git
cd probot-sharp

# Copy environment template and configure your GitHub App credentials
cp .env.example .env
# Edit .env with your GitHub App ID, webhook secret, and private key path

# Place your GitHub App private key in the project root
# Download from: https://github.com/settings/apps/your-app
cp ~/Downloads/your-app.private-key.pem ./private-key.pem

# Start all services (PostgreSQL + Redis + API)
docker-compose up

# The API will be available at http://localhost:8080
# Webhooks endpoint: http://localhost:8080/webhooks
```

**Optional:** For local webhook testing with [smee.io](https://smee.io/):

```bash
# Get a webhook proxy URL from https://smee.io
# Add SMEE_URL=https://smee.io/your-channel to .env
docker-compose --profile smee up
```

### Local .NET Development

For local development without Docker:

```bash
# Prerequisites: .NET SDK 8.0.1xx+, optional Docker/Redis/PostgreSQL for adapters

git clone https://github.com/your-org/probot-sharp.git
cd probot-sharp

# Restore dependencies and build everything
dotnet restore
dotnet build

# Run the test suite
dotnet test

# Generate coverage artifacts (requires reportgenerator)
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"tests/**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:HtmlSummary;JsonSummary
```

**Running the minimal API prototype:**

```bash
dotnet run --project src/ProbotSharp.Bootstrap.Api
```

**Running the CLI bootstrap prototype:**

```bash
dotnet run --project src/ProbotSharp.Bootstrap.Cli -- --help
```

## Built-in Extensions

Probot Sharp includes three powerful batteries-included extensions that make it easy to build interactive GitHub Apps. These provide the same functionality as popular Node.js Probot extensions, but with strong typing and better integration.

### Slash Commands

Parse and route `/command arguments` from issue and PR comments:

```csharp
[SlashCommandHandler("label")]
public class LabelCommand : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>() ?? 0;
        var labels = command.Arguments.Split(',').Select(l => l.Trim()).ToArray();

        await context.GitHub.Issue.Labels.AddToIssue(
            context.Repository.Owner,
            context.Repository.Name,
            issueNumber,
            labels);
    }
}
```

**Features:**
- Case-insensitive command matching
- Multiple commands per comment
- Automatic handler discovery via attributes
- Works with issue comments and PR review comments

[Learn more about Slash Commands](docs/SlashCommands.md)

### Metadata Storage

Persist key-value data scoped to specific issues or pull requests using PostgreSQL:

```csharp
[EventHandler("issues", "edited")]
public class EditTracker : IEventHandler
{
    private readonly IMetadataPort _metadataPort;

    public EditTracker(IMetadataPort metadataPort)
    {
        _metadataPort = metadataPort;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        var metadata = new MetadataService(_metadataPort, context);

        var count = await metadata.GetAsync("edit_count", ct);
        var newCount = int.TryParse(count, out var c) ? c + 1 : 1;
        await metadata.SetAsync("edit_count", newCount.ToString(), ct);
    }
}
```

**Features:**
- PostgreSQL storage (durable and reliable)
- Automatic scoping to repository + issue/PR
- Simple CRUD operations
- Faster than Node.js probot-metadata (direct DB vs GitHub API)

[Learn more about Metadata Storage](docs/Metadata.md)

### Comment Attachments

Add rich, structured content to comments without modifying user text:

```csharp
[EventHandler("issue_comment", "created")]
public class BuildStatusAttachment : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        var attachments = new CommentAttachmentService(context);

        await attachments.AddAsync(new CommentAttachment
        {
            Title = "Build Status",
            TitleLink = "https://ci.example.com/builds/123",
            Text = "Build completed successfully",
            Color = "green",
            Fields = new List<AttachmentField>
            {
                new() { Title = "Duration", Value = "2m 34s", Short = true },
                new() { Title = "Tests", Value = "142 passed", Short = true }
            }
        }, ct);
    }
}
```

**Features:**
- Non-invasive (preserves original comment text)
- Idempotent updates (replaces instead of duplicates)
- Markdown-based rendering
- Multiple attachments support

[Learn more about Comment Attachments](docs/Attachments.md)

### Repository Configuration

Load settings from `.github/config.yml` files with cascading, inheritance, and flexible merge strategies:

```csharp
public class BotSettings
{
    public string WelcomeMessage { get; set; } = "Welcome!";
}

[EventHandler("issues", "opened")]
public class ConfigurableBot : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        var config = await context.GetConfigAsync<BotSettings>(
            "bot-config.yml",
            new BotSettings { WelcomeMessage = "Hello!" },
            ct);

        if (config != null && context.Repository != null)
        {
            var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>() ?? 0;

            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber,
                config.WelcomeMessage);
        }
    }
}
```

**Features:**
- Configuration cascading (root → .github → org)
- `_extends` inheritance from other repositories
- 3 array merge strategies (Replace, Concatenate, DeepMergeByIndex)
- 5-minute caching with SHA-based keys
- Strongly-typed and dictionary support

[Learn more about Repository Configuration](docs/RepositoryConfiguration.md)

### Working Examples

- [`examples/ExtensionsBot/`](examples/ExtensionsBot/) - Slash commands, metadata, and attachments working together
- [`examples/ConfigBot/`](examples/ConfigBot/) - Repository-backed configuration with cascading and inheritance

**Complete Extensions Documentation:** [docs/Extensions.md](docs/Extensions.md)

## What's Different from Probot (Node.js)

While Probot-Sharp maintains API familiarity with the original Node.js Probot framework, it introduces several architectural and technical improvements:

| Feature | Probot (Node.js) | Probot-Sharp (.NET) |
|---------|------------------|---------------------|
| **Architecture** | Layered architecture | Hexagonal (ports & adapters) - domain logic isolated from infrastructure |
| **Type Safety** | TypeScript optional types | Full C# strong typing with compile-time guarantees |
| **Error Handling** | Try/catch with exceptions | `Result<T>` pattern - explicit success/failure states, no hidden control flow |
| **Domain Logic** | Business rules mixed with framework code | Pure domain model with specifications, value objects, and domain events |
| **Querying** | Direct data access | Specification pattern - composable, testable domain queries |
| **Observability** | Custom logging/metrics | Built-in OpenTelemetry support for distributed tracing and metrics |
| **Deployment** | Node.js runtime (serverless, containers) | Multiple options: Docker, Kubernetes, AWS ECS, Azure Web Apps, serverless-ready |
| **Resilience** | Manual retry logic | Built-in circuit breakers, retry policies with exponential backoff, and timeout handling |
| **Testing** | Jest/Mocha | xUnit with strong mocking (NSubstitute), property-based testing support |
| **Dependency Injection** | Manual wiring | First-class DI container with lifetime scoping |
| **Webhook Deduplication** | Manual (app responsibility) | Automatic `UseIdempotency()` middleware with dual-layer strategy (database + distributed lock) |

**Key Architectural Benefits:**

- **Testability:** Domain logic is pure C# with no external dependencies - tests run in microseconds
- **Maintainability:** Clear boundaries between domain, application, and infrastructure layers
- **Extensibility:** Add new adapters (gRPC, message queues) without touching domain code
- **Performance:** Compiled binary with AOT support, smaller memory footprint than Node.js runtime

## Repository Layout

```
src/
  ProbotSharp.Domain/              # Aggregate roots, value objects, domain services & events
  ProbotSharp.Application/         # Ports, DTOs & use cases (hexagonal core)
  ProbotSharp.Infrastructure/      # Outbound adapters (GitHub HTTP, persistence stubs, etc.)
  ProbotSharp.Adapters.Http/       # Inbound HTTP adapter (minimal API endpoints)
  ProbotSharp.Adapters.Cli/        # CLI adapter (Spectre.Console host in-progress)
  ProbotSharp.Adapters.Workers/    # Worker/queue adapter (scaffolding placeholder)
  ProbotSharp.Bootstrap.Api/       # Minimal API host wiring adapters + app
  ProbotSharp.Bootstrap.Cli/       # CLI host wiring
  ProbotSharp.Shared/              # Cross-cutting primitives (Result, Ensure, Error)

tests/
  ProbotSharp.Domain.Tests/        # Domain unit specs (336 tests)
  ProbotSharp.Application.Tests/   # Use case specs with mocked ports (193 tests)
  ProbotSharp.Infrastructure.Tests/# Adapter specs (336 tests)
  ProbotSharp.Adapter.Tests/       # HTTP, CLI, and Worker adapter tests (88 tests)
  ProbotSharp.Bootstrap.Api.Tests/ # WebApplicationFactory smoke tests (48 tests)
  ProbotSharp.Shared.Tests/        # Shared utility tests (147 tests)
  ProbotSharp.IntegrationTests/    # End-to-end scenarios (73 tests)

deploy/
  aws/                             # AWS ECS deployment (CloudFormation, task definitions)
  azure/                           # Azure Web Apps deployment (Bicep templates)
  k8s/                             # Kubernetes deployment (plain manifests + Helm chart)
    helm/                          # Helm chart with PostgreSQL/Redis subcharts

docs/                              # Architecture, testing, operations, deployment guides
```

## Architecture Overview

Probot-Sharp embraces ports & adapters to keep the GitHub automation domain independent of runtime infrastructure:

| Layer           | Components | Purpose                                                                                          |
|-----------------|------------|--------------------------------------------------------------------------------------------------|
| **Shared**      | Result<T>, Ensure, DTOs, mappings | Cross-cutting primitives for error handling, validation, and data transfer.                                          |
| **Domain**      | 9 value objects, 4 entities, 1 service, 6 events, 11 specifications | Pure C# domain model: aggregates (`GitHubApp`, `Installation`), value objects, domain services, events, and specifications. |
| **Application** | 6 inbound ports, 18 outbound ports, 5 use cases | Hexagonal core ports defining contracts, DTOs for data transfer, and orchestrating use cases (`ProcessWebhookUseCase`, etc.).     |
| **Infrastructure** | 24 adapters | Outbound adapters for GitHub API, caching (Redis), persistence (PostgreSQL), metrics, and resilience. |
| **Adapters**    | HTTP, CLI, Workers | Inbound adapters translating external requests into application commands. |
| **Bootstrap**   | API & CLI hosts | Composition roots wiring dependency injection, middleware, and runtime infrastructure.    |

**Domain-Driven Design Patterns:**
- **Value Objects:** Immutable types modeling domain concepts (AppId, WebhookSecret, InstallationId)
- **Entities:** Objects with identity and lifecycle (GitHubApp, Installation, WebhookEvent)
- **Specifications:** Composable query objects for domain filtering and validation
- **Domain Events:** Capturing significant state changes (AppInstalled, WebhookReceived, EventProcessed)
- **Aggregate Roots:** Enforcing consistency boundaries and business invariants

See [`docs/Architecture.md`](docs/Architecture.md) for sequence diagrams, port descriptions, and extension guidance.

## Development Workflow

1. **Model the domain first**: new scenarios start in the Domain project with value objects/events.
2. **Define ports** in the Application layer before writing adapters.
3. **Add tests** alongside changes (domain/application tests should stay pure and deterministic).
4. **Wire adapters** only after ports exist. Infrastructure concerns live outside the domain.
5. **Keep bootstraps thin**: the API/CLI projects should primarily register services and start hosts.

See [`docs/Operations.md`](docs/Operations.md) for configuration, environment setup and runtime behaviours.

## Testing & Coverage

Probot-Sharp has comprehensive test coverage across all architectural layers:

**Test Suite Statistics (1221 tests, 59.1% overall coverage):**

| Test Suite | Test Count | Coverage | Focus |
|------------|------------|----------|-------|
| **Domain** | 336 tests | 91.9% | Value objects, entities, specifications, domain services, events |
| **Application** | 193 tests | 66.3% | Use cases, port contracts, DTOs, application orchestration |
| **Infrastructure** | 336 tests | 69.1% | GitHub API adapters, caching, persistence, metrics, resilience |
| **HTTP Adapters** | 14 tests | 87.4% | HTTP endpoints, middleware, health checks, webhooks |
| **CLI Adapters** | 60 tests | N/A | CLI commands, local webhook replay, setup wizard |
| **Workers** | 14 tests | 86.6% | Background workers, webhook replay, dead-letter queue |
| **Integration** | 73 tests | 17.0% | End-to-end webhook processing, database interactions, API flows |
| **Shared** | 147 tests | 71.0% | Result types, validation, error handling primitives |
| **Bootstrap.Api** | 48 tests | 70.0% | API bootstrapping, configuration validation, setup middleware |

> **Note:** Coverage percentages in the table above are manually updated and representative only. For precise coverage metrics, generate a coverage report locally using the commands below.

**Test Execution:**

```bash
# Run all tests
dotnet test

# Run tests with coverage (excludes generated code)
dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"

# Generate HTML coverage report (with filters)
reportgenerator -reports:"tests/**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:"HtmlSummary;JsonSummary" \
  -classfilters:"-*Migration*;-*DbContextModelSnapshot"

# Run specific test suite
dotnet test tests/ProbotSharp.Domain.Tests
```

**Testing Approach:**
- **Unit tests:** Fast, isolated tests with no external dependencies (domain and application layers)
- **Integration tests:** End-to-end tests with real PostgreSQL and Redis using Testcontainers
- **Contract tests:** API endpoint validation using `WebApplicationFactory`
- **Property-based tests:** FsCheck for generating edge cases automatically

For a comprehensive explanation of the multi-layered testing strategy, validation approaches, and why configuration issues can slip through different testing layers, see [`docs/TestingStrategy.md`](docs/TestingStrategy.md).

### Testing GitHub Actions Workflows Locally

Test GitHub Actions workflows locally before pushing using [nektos/act](https://nektosact.com/) via the GitHub CLI extension:

**Prerequisites:**

```bash
# Install act via GitHub CLI (if not already installed)
gh extension install https://github.com/nektos/gh-act

# Verify installation (requires v0.2.82+)
gh act --version
```

**Test Kubernetes Validation Workflows:**

```bash
# List all available workflows and jobs
gh act -l

# Test manifest validation
gh act pull_request -W .github/workflows/validate-kubernetes.yml -j validate-manifests

# Test Helm chart validation
gh act pull_request -W .github/workflows/validate-kubernetes.yml -j validate-helm

# Test all .NET CI jobs
gh act pull_request -W .github/workflows/dotnet.yml
```

**Expected Results:**

- **validate-manifests:** Validates 8 Kubernetes resources in 6 files
- **validate-helm:** Lints Helm chart and validates 4 templated resources
- Both jobs should show `🏁 Job succeeded` on success

**Note:** The deployment test jobs (`deploy-test-manifests` and `deploy-test-helm`) only run on pushes to the main branch and require kind cluster creation, which may not work in all local Docker environments.

### Validation Scripts

ProbotSharp includes several validation scripts to catch configuration issues before deployment:

**Pre-commit Validation:**

```bash
# Validate Kubernetes configuration consistency
./scripts/validate-k8s-config.sh

# Validates:
# - Environment variable paths (e.g., ProbotSharp__AppId not ProbotSharp__GitHub__AppId)
# - ConfigMap key references exist
# - Required volume mounts for non-root containers
# - Security context configuration
# - Helm template consistency
```

**Integration Testing:**

```bash
# Test deployed Kubernetes application (requires running cluster)
./scripts/k8s-integration-test.sh

# Validates:
# - Pods running without crashes
# - Application logs clean (no errors)
# - Environment variables properly set
# - Health endpoint responding (HTTP 200)
# - Root endpoint returns metadata
# - No container restarts
```

**Why These Are Important:**

Static validation (kubeconform) catches YAML syntax errors but misses runtime configuration issues. The validation scripts catch configuration path errors, missing keys, and permission issues that only appear when the actual application tries to start.

See [`docs/TestingStrategy.md`](docs/TestingStrategy.md) for a comprehensive explanation of the multi-layered testing approach and why configuration issues can slip through different validation layers.

## Deployment

Probot-Sharp provides production-ready deployment templates for multiple platforms:

### Container Orchestration

- **Kubernetes** ([`deploy/k8s/`](deploy/k8s/)) - Two deployment methods:
  - **Helm Chart** (recommended): Single-command deployment with built-in PostgreSQL/Redis subcharts
  - **Plain Manifests**: Individual YAML files for full control (deployments, services, ingress, ConfigMaps, secrets)
  - Horizontal Pod Autoscaler (HPA) for scaling
  - Health checks and readiness probes
  - See [`deploy/k8s/README.md`](deploy/k8s/README.md) for both approaches

### Cloud Platforms

- **AWS ECS** ([`deploy/aws/`](deploy/aws/)) - Amazon ECS deployment with:
  - CloudFormation templates for infrastructure
  - ECS task definitions with Fargate support
  - Application Load Balancer configuration
  - RDS PostgreSQL and ElastiCache Redis
  - See [`deploy/aws/README.md`](deploy/aws/README.md) for details

- **Azure Web Apps** ([`deploy/azure/`](deploy/azure/)) - Azure deployment with:
  - Bicep templates for infrastructure as code
  - App Service configuration with container support
  - Azure Database for PostgreSQL
  - Azure Cache for Redis
  - Application Insights for observability
  - See [`deploy/azure/README.md`](deploy/azure/README.md) for details

### Local/Development

- **Docker Compose** ([`docker-compose.yml`](docker-compose.yml)) - Local development stack:
  - All services (API, PostgreSQL, Redis) with health checks
  - Volume mounts for logs and replay queues
  - Optional smee.io webhook proxy for local testing
  - Environment-based configuration via `.env`

See [`docs/Deployment.md`](docs/Deployment.md) for comprehensive deployment guidance, environment variables, and production considerations.

## Documentation Index

### Architecture & Design
- [`docs/Architecture.md`](docs/Architecture.md) - Deep dive into the hexagonal design, project boundaries and flows.
- [`docs/BestPractices.md`](docs/BestPractices.md) - Comprehensive best practices guide for building effective GitHub Apps.
- [`docs/BestPractices-Checklist.md`](docs/BestPractices-Checklist.md) - Pre-release checklist for app developers.

### Getting Started
- [`docs/LocalDevelopment.md`](docs/LocalDevelopment.md) - Local development setup, Docker Compose usage, and debugging.
- [`docs/MinimalDeployment.md`](docs/MinimalDeployment.md) - Minimal viable deployment configurations, zero-infrastructure setup, and scaling paths.
- [`docs/EventHandlers.md`](docs/EventHandlers.md) - Event handler discovery, attribute-based routing, wildcard handlers, error handling, and lifecycle management.

### Core Features
- [`docs/ContextHelpers.md`](docs/ContextHelpers.md) - Context helper methods for extracting webhook data and API shortcuts.
- [`docs/GraphQL.md`](docs/GraphQL.md) - Complete guide to using GitHub's GraphQL API v4 with context.GraphQL.
- [`docs/Pagination.md`](docs/Pagination.md) - Pagination best practices, helper methods, performance considerations, and LINQ patterns.

### Extensions
- [`docs/Extensions.md`](docs/Extensions.md) - Built-in extensions: slash commands, metadata storage, comment attachments, and repository configuration.
- [`docs/SlashCommands.md`](docs/SlashCommands.md) - Detailed guide to slash command handlers and patterns.
- [`docs/Metadata.md`](docs/Metadata.md) - Metadata storage guide with PostgreSQL persistence and CRUD operations.
- [`docs/Attachments.md`](docs/Attachments.md) - Comment attachment guide for adding rich structured content.
- [`docs/RepositoryConfiguration.md`](docs/RepositoryConfiguration.md) - Repository-backed configuration with cascading, inheritance, and merge strategies.

### Deployment & Operations
- [`docs/Deployment.md`](docs/Deployment.md) - Comprehensive deployment guide for all supported platforms.
- [`docs/Operations.md`](docs/Operations.md) - Runtime modes, configuration, observability and deployment notes.
- [`docs/AdapterConfiguration.md`](docs/AdapterConfiguration.md) - Adapter patterns, provider configuration, cache/idempotency/persistence adapters, and scaling strategies.
- [`docs/ConfigurationBestPractices.md`](docs/ConfigurationBestPractices.md) - JSON schema, inline documentation, explicit options, environment-specific configuration, and secrets management.

### Testing & Quality Assurance
- [`docs/TestingStrategy.md`](docs/TestingStrategy.md) - Multi-layered testing approach, unit/integration/contract tests, Kubernetes testing layers, validation scripts, and CI/CD pipeline explanation.

## Probot ↔ Probot Sharp Guide

For a practical, side-by-side mapping from Probot (Node.js) docs to Probot Sharp (.NET) examples in this repo, see:

- [`docs/Probot-to-ProbotSharp-Guide.md`](docs/Probot-to-ProbotSharp-Guide.md)
