# ProbotSharp Testing Scripts

This directory contains scripts for testing ProbotSharp examples and bots.

## Scripts

### test-template.sh

Test the ProbotSharp project template by generating a fresh project and verifying it works.

**Usage:**
```bash
# Default (generates "TemplateTest" on port 5000)
./test-template.sh

# Custom project name
./test-template.sh MyCustomBot

# Custom name and port
./test-template.sh MyCustomBot 8080
```

**What it tests:**
1. Template generation (`dotnet new probotsharp-app`)
2. Project reference resolution
3. Docker build
4. Container startup
5. Health endpoint (GET /health)
6. Webhook endpoint (POST /api/github/webhooks)
7. Automatic cleanup

### send-test-webhook.sh

Send a test GitHub webhook to a running ProbotSharp bot.

**Usage:**
```bash
# Default (http://localhost:5000/webhooks with default payload)
./send-test-webhook.sh

# Custom URL
./send-test-webhook.sh http://localhost:8080/webhooks

# Custom payload
./send-test-webhook.sh "" path/to/custom-payload.json

# Custom webhook secret
WEBHOOK_SECRET=mysecret ./send-test-webhook.sh
```

**Environment Variables:**
- `WEBHOOK_SECRET` - Webhook secret for HMAC signature (default: "development")

### test-example.sh

Test a single ProbotSharp example end-to-end with Docker.

**Usage:**
```bash
# Test MinimalBot
./test-example.sh MinimalBot

# Test on custom port
./test-example.sh HelloWorldBot 8080
```

**What it tests:**
1. Docker build succeeds
2. Container starts without errors
3. Health endpoint responds (GET /health)
4. Webhook endpoint accepts events (POST /webhooks or /api/github/webhooks)

### test-all-examples.sh

Test all ProbotSharp examples in the repository.

**Usage:**
```bash
# Run all tests (concise output)
./test-all-examples.sh

# Verbose output
./test-all-examples.sh --verbose

# Include template test
./test-all-examples.sh --with-template

# Verbose with template
./test-all-examples.sh --verbose --with-template
```

**Tested examples:**
- MinimalBot
- HelloWorldBot
- WildcardBot
- AttachmentsBot
- MetadataBot
- SlashCommandsBot
- GraphQLBot
- PaginationBot
- HttpExtensibilityBot
- DryRunBot
- ConfigBot
- ExtensionsBot

## Test Fixtures

Test webhook payloads are located in `fixtures/test-webhook-payload.json`.

**Default payload:**
- Event: `issues.opened`
- Action: `opened`
- Issue number: 1
- Repository: testorg/test-repo
- Sender: testuser
- Installation ID: 123456

## Prerequisites

- Docker installed and running
- curl installed
- openssl installed (for HMAC signature generation)
- Repository built (at least once)

## Running from Repository Root

All scripts should be run from the repository root:

```bash
# Test single example
./scripts/testing/test-example.sh MinimalBot

# Test all examples
./scripts/testing/test-all-examples.sh

# Send webhook to running bot
./scripts/testing/send-test-webhook.sh http://localhost:5000/webhooks
```

## CI/CD Integration

These scripts can be integrated into CI/CD pipelines:

```yaml
# .github/workflows/test-examples.yml
name: Test Examples

on: [push, pull_request]

jobs:
  test-all:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Test all examples
        run: ./scripts/testing/test-all-examples.sh
```

Or test examples in parallel:

```yaml
jobs:
  test:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        example:
          - MinimalBot
          - HelloWorldBot
          - WildcardBot
    steps:
      - uses: actions/checkout@v4
      - name: Test ${{ matrix.example }}
        run: ./scripts/testing/test-example.sh ${{ matrix.example }}
```

## Custom Payloads

Create custom webhook payloads in the `fixtures/` directory:

```json
{
  "action": "closed",
  "issue": {
    "number": 2,
    "title": "Custom test issue",
    "state": "closed",
    "user": {"login": "testuser"}
  },
  "repository": {
    "name": "test-repo",
    "full_name": "testorg/test-repo",
    "owner": {"login": "testorg"}
  },
  "sender": {"login": "testuser"},
  "installation": {"id": 123456}
}
```

Then use with send-test-webhook.sh:

```bash
./scripts/testing/send-test-webhook.sh "" fixtures/custom-payload.json
```

## Troubleshooting

### Webhook returns 401 Unauthorized

- Check webhook secret matches: `WEBHOOK_SECRET=yoursecret ./send-test-webhook.sh`
- Verify bot configuration has same secret

### Webhook returns 404 Not Found

- Check webhook endpoint path
- Examples use `/webhooks` 
- Template-generated bots use `/api/github/webhooks`
- Try: `./send-test-webhook.sh http://localhost:5000/api/github/webhooks`

### Health check fails

- Check container logs: `docker logs <container-name>`
- Verify container is running: `docker ps`
- Check port mapping: `docker port <container-name>`

### Build fails

- Check build logs in `/tmp/<ExampleName>_build.log`
- Ensure all project references are correct
- Restore dependencies: `dotnet restore`

## Clean Up

Remove test containers and images:

```bash
# Stop and remove all test containers
docker ps -a | grep "-test" | awk '{print $1}' | xargs docker rm -f

# Remove test images
docker images | grep -E "(minimalbot|helloworldbot|wildcardbot)" | awk '{print $3}' | xargs docker rmi
```

## See Also

- [Scripts Directory](../README.md) - Scripts overview including verify-github-links.py and verify-local-links.py
- [Local Development Guide](../../docs/LocalDevelopment.md) - Local development setup and debugging
- [Deployment Guide](../../docs/Deployment.md) - Production deployment documentation
