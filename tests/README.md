# ProbotSharp Tests

This directory contains all test projects for the ProbotSharp solution.

## Test Projects

### Unit Tests
- **ProbotSharp.Domain.Tests** - Domain layer unit tests (entities, value objects, specifications)
- **ProbotSharp.Application.Tests** - Application layer unit tests (use cases, commands, queries)
- **ProbotSharp.Infrastructure.Tests** - Infrastructure layer unit tests (adapters, persistence, external services)
- **ProbotSharp.Adapter.Tests** - Adapter layer unit tests (HTTP, CLI, Workers)
- **ProbotSharp.Bootstrap.Api.Tests** - API bootstrap tests
- **ProbotSharp.Shared.Tests** - Shared utilities tests (mapping, abstractions)

### Integration Tests
- **ProbotSharp.IntegrationTests** - End-to-end integration tests using Testcontainers
- **ProbotSharp.Infrastructure.Tests/Integration** - Infrastructure integration tests with Redis and PostgreSQL

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Tests by Project
```bash
dotnet test tests/ProbotSharp.Domain.Tests
dotnet test tests/ProbotSharp.Application.Tests
dotnet test tests/ProbotSharp.Infrastructure.Tests
```

### Run Integration Tests

Integration tests use [Testcontainers](https://dotnet.testcontainers.org/) to spin up real Docker containers (Redis, PostgreSQL) for testing.

**Prerequisites:**
- Docker must be installed and running
- Docker daemon must be accessible

**IMPORTANT - Ryuk Resource Reaper:**
The Testcontainers Ryuk resource reaper has issues on some systems. You **MUST** set the environment variable to disable Ryuk before running integration tests:

```bash
# Linux/macOS - set for current session
export TESTCONTAINERS_RYUK_DISABLED=true

# Linux/macOS - set permanently in ~/.bashrc or ~/.zshrc
echo 'export TESTCONTAINERS_RYUK_DISABLED=true' >> ~/.bashrc

# Windows PowerShell
$env:TESTCONTAINERS_RYUK_DISABLED="true"

# Windows CMD
set TESTCONTAINERS_RYUK_DISABLED=true
```

**Run Integration Tests:**
```bash
# Database integration tests (PostgreSQL)
TESTCONTAINERS_RYUK_DISABLED=true dotnet test tests/ProbotSharp.IntegrationTests

# Redis integration tests
TESTCONTAINERS_RYUK_DISABLED=true dotnet test tests/ProbotSharp.Infrastructure.Tests --filter "FullyQualifiedName~Integration"

# All integration tests
TESTCONTAINERS_RYUK_DISABLED=true dotnet test --filter "FullyQualifiedName~Integration"
```

**Alternative - Using RunSettings:**
You can also use the provided .runsettings file:
```bash
dotnet test tests/ProbotSharp.Infrastructure.Tests --settings tests/integration.runsettings --filter "FullyQualifiedName~Integration"
dotnet test tests/ProbotSharp.IntegrationTests --settings tests/integration.runsettings
```

### Run with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Generate Coverage Report
```bash
# Install ReportGenerator if not already installed
dotnet tool install --global dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator -reports:"tests/**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html

# Open the report
open coverage-report/index.html  # macOS
xdg-open coverage-report/index.html  # Linux
start coverage-report/index.html  # Windows
```

## Test Organization

Tests follow the **Arrange-Act-Assert** pattern and use:
- **xUnit** - Testing framework
- **FluentAssertions** - Assertion library for readable tests
- **NSubstitute** - Mocking framework
- **Testcontainers** - Docker-based integration testing

## Integration Test Details

### Database Integration Tests (ProbotSharp.IntegrationTests)
- Uses PostgreSQL 16 Alpine container
- Tests database schema creation, CRUD operations, transactions, and concurrency
- 17 tests covering webhook deliveries, manifests, and idempotency records

### Redis Integration Tests (ProbotSharp.Infrastructure.Tests/Integration)
- Uses Redis 7 Alpine container
- Tests access token caching and idempotency locking
- 26 tests total:
  - 11 tests for RedisAccessTokenCacheAdapter
  - 15 tests for RedisIdempotencyAdapter

### Test Collections
Integration tests use xUnit's `[Collection]` attribute to ensure they run sequentially, preventing Docker container conflicts:
- `"Database Integration Tests"` - PostgreSQL tests
- `"Redis Integration Tests"` - Redis cache and idempotency tests

## Troubleshooting

### Docker Issues
- Ensure Docker is running: `docker ps`
- Check Docker socket permissions: `ls -la /var/run/docker.sock`
- Pull images manually if needed:
  ```bash
  docker pull redis:7-alpine
  docker pull postgres:16-alpine
  ```

### Port Conflicts
Testcontainers automatically assigns random ports to avoid conflicts. If you see port binding errors, ensure no other services are using the ephemeral port range.

### Timeout Issues
Integration tests may timeout on first run while Docker pulls images. Subsequent runs will be faster as images are cached.

## Contributing

When adding new tests:
1. Follow existing test patterns and naming conventions
2. Use FluentAssertions for readable assertions
3. Add integration tests for new adapters or external integrations
4. Ensure tests are independent and can run in any order
5. Add tests to appropriate test collections if using Testcontainers
