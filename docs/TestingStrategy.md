# Testing Strategy

This document explains the multi-layered testing approach for ProbotSharp and the trade-offs between different testing strategies.

## Table of Contents

- [Testing Pyramid](#testing-pyramid)
- [Kubernetes Testing Layers](#kubernetes-testing-layers)
- [Why Configuration Issues Can Slip Through](#why-configuration-issues-can-slip-through)
- [Validation Scripts](#validation-scripts)
- [Testing Locally](#testing-locally)
- [CI/CD Pipeline](#cicd-pipeline)
- [Recommendations](#recommendations)

## Testing Pyramid

ProbotSharp uses a comprehensive testing pyramid:

```
                    ╱╲
                   ╱  ╲
                  ╱ E2E ╲           Manual testing in production-like environments
                 ╱────────╲
                ╱          ╲
               ╱ Integration╲       Testcontainers, WebApplicationFactory
              ╱──────────────╲
             ╱                ╲
            ╱  Unit + Property ╲    Fast, in-memory, high coverage
           ╱────────────────────╲
```

### Unit Tests (1221 tests)
- **Location**: `tests/ProbotSharp.Domain.Tests`, `tests/ProbotSharp.Application.Tests`
- **Purpose**: Test business logic in isolation
- **Coverage**: Domain 91.9%, Application 66.3%
- **Speed**: Very fast (<1 second)
- **Dependencies**: None (pure in-memory)

**Example**:
```csharp
public class WebhookSignatureValidatorTests
{
    [Fact]
    public void ValidateSignature_WithValidSignature_ShouldReturnSuccess()
    {
        // Arrange
        var validator = new WebhookSignatureValidator();
        var payload = "{\"action\":\"opened\"}";
        var secret = "test-secret";
        var expectedSignature = "sha256=8c9f3d8e5f2a1b0c6d4e7f9a3b2c5d8e1f4a7b0c3d6e9f2a5b8c1d4e7f0a3b6";

        // Act
        var isValid = validator.IsSignatureValid(payload, secret, expectedSignature);

        // Assert
        Assert.True(isValid);
    }
}
```

### Integration Tests
- **Location**: `tests/ProbotSharp.Infrastructure.Tests`, `tests/ProbotSharp.IntegrationTests`
- **Purpose**: Test infrastructure adapters with real dependencies
- **Coverage**: 17.0% (focused on critical paths)
- **Speed**: Slow (requires Docker + Testcontainers)
- **Dependencies**: PostgreSQL, Redis via Testcontainers

**Example**:
```csharp
public class WebhookProcessingIntegrationTests
{
    [Fact]
    public async Task ProcessWebhook_WithValidPayload_ShouldStoreInDatabase()
    {
        // Arrange - Uses real PostgreSQL container (requires Testcontainers NuGet package)
        // await using var container = new PostgreSqlBuilder()
        //     .WithDatabase("testdb")
        //     .WithUsername("postgres")
        //     .WithPassword("postgres")
        //     .Build();
        // await container.StartAsync();

        // Create webhook delivery
        var deliveryId = DeliveryId.Create(Guid.NewGuid().ToString());
        var eventName = WebhookEventName.Create("issues");
        var payload = WebhookPayload.Create("{\"action\":\"opened\"}");
        var command = new ProcessWebhookCommand(deliveryId, eventName, payload, null, null, null);

        // Act - Test actual webhook processing
        var processingPort = CreateMockProcessingPort();
        var result = await processingPort.ProcessAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    private static IWebhookProcessingPort CreateMockProcessingPort()
    {
        // In real tests, this would setup DI container with test database connection
        // For this example, return a mock implementation
        throw new NotImplementedException("Configure DI container with test database");
    }
}
```

### Contract Tests
- **Location**: `tests/ProbotSharp.Bootstrap.Api.Tests`
- **Purpose**: Test HTTP endpoints and middleware
- **Coverage**: 70.0%
- **Speed**: Fast (WebApplicationFactory in-memory)
- **Dependencies**: None

**Example**:
```csharp
// Contract tests use WebApplicationFactory from Microsoft.AspNetCore.Mvc.Testing package
public class WebhookEndpointTests
{
    private readonly HttpClient _client;

    public WebhookEndpointTests()
    {
        // In real tests: _client = factory.CreateClient();
        // where factory is WebApplicationFactory<Program> injected via IClassFixture
        _client = new HttpClient { BaseAddress = new Uri("http://localhost") };
    }

    [Fact]
    public async Task Webhook_WithValidSignature_ShouldReturn202Accepted()
    {
        // Arrange
        var payload = "{\"action\":\"opened\",\"installation\":{\"id\":123}}";
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Add required GitHub webhook headers
        content.Headers.Add("X-GitHub-Event", "issues");
        content.Headers.Add("X-GitHub-Delivery", Guid.NewGuid().ToString());
        content.Headers.Add("X-Hub-Signature-256", "sha256=valid_signature_here");

        // Act
        var response = await _client.PostAsync("/webhooks", content);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }
}
```

## Kubernetes Testing Layers

### Layer 1: Static Validation (Fast, Limited Coverage)

**Tool**: Kubeconform
**Speed**: <5 seconds
**What it validates**:
- ✅ YAML syntax is valid
- ✅ Resource schemas match Kubernetes API
- ✅ Required fields are present
- ✅ Field types are correct

**What it DOESN'T validate**:
- ❌ Environment variables map to actual configuration paths
- ❌ Referenced ConfigMap keys exist
- ❌ Referenced Secret keys exist
- ❌ Application can load configuration
- ❌ Runtime permissions (file system, networking)

**Example**: This passes kubeconform but fails at runtime:
```yaml
env:
  - name: ProbotSharp__GitHub__AppId  # ❌ Wrong path! Should be ProbotSharp__AppId
    valueFrom:
      secretKeyRef:
        name: probotsharp-secrets
        key: github-app-id
```

### Layer 2: Configuration Consistency Validation (Medium Coverage)

**Tool**: `scripts/validate-k8s-config.sh`
**Speed**: <10 seconds
**What it validates**:
- ✅ Environment variable paths follow correct patterns
- ✅ Referenced ConfigMap keys exist in configmap.yaml
- ✅ Required volume mounts for non-root containers
- ✅ Security context is configured
- ✅ Helm template consistency with plain manifests

**What it DOESN'T validate**:
- ❌ Secrets exist and have correct keys (can't inspect secrets)
- ❌ Application successfully loads configuration
- ❌ External dependencies (Redis, PostgreSQL) are reachable

**Example**: This now fails validation:
```bash
$ ./scripts/validate-k8s-config.sh
✗ Invalid path: ProbotSharp__GitHub__AppId (should not contain 'GitHub' level)
  Hint: Use 'ProbotSharp__AppId' not 'ProbotSharp__GitHub__AppId'
```

### Layer 3: Deployment Structure Testing (Fast, Stub Image)

**Tool**: GitHub Actions `deploy-test-manifests` job
**Speed**: ~2 minutes
**What it validates**:
- ✅ Deployment can be created
- ✅ Pods reach Running state
- ✅ Service is created
- ✅ ConfigMaps and Secrets are applied
- ✅ Deployment becomes Available

**What it DOESN'T validate**:
- ❌ **Actual application behavior** (uses `nginx:alpine` stub image)
- ❌ Configuration is loaded correctly
- ❌ Health endpoints work
- ❌ Webhook processing succeeds

**Why use a stub image?**
- ⚡ Fast: No need to build the actual Docker image
- 🔄 CI-friendly: Works without image registry credentials
- 🎯 Focused: Tests deployment structure only

**Trade-off**: This is why configuration path errors weren't caught until production deployment with the real image.

### Layer 4: Integration Testing (Slow, Real Image)

**Tool**: `scripts/k8s-integration-test.sh`
**Speed**: ~5 minutes
**What it validates**:
- ✅ Pods are running without crashes
- ✅ No CrashLoopBackOff or ImagePullBackOff
- ✅ Application logs don't contain errors
- ✅ Required environment variables are set
- ✅ Health endpoint responds (HTTP 200)
- ✅ Root endpoint returns metadata
- ✅ No container restarts

**When to run**:
- Before production deployments
- After infrastructure changes
- When adding new configuration
- For release validation

**Example**:
```bash
$ ./scripts/k8s-integration-test.sh
✓ Test 1: Checking if pods are running...
✅ Found 2 running pod(s)
✓ Test 2: Checking for pod errors...
✅ No pods in error state
✓ Test 3: Checking logs for configuration errors...
✅ No configuration errors in logs
✓ Test 4: Verifying environment variables...
✅ All required environment variables are set
✓ Test 5: Testing health endpoint...
✅ Health endpoint responding
✓ Test 6: Testing root endpoint...
✅ Root endpoint returning metadata
🎉 All integration tests passed!
```

## Why Configuration Issues Can Slip Through

### Issue 1: Wrong Configuration Paths

**What happened**:
```yaml
- name: ProbotSharp__GitHub__AppId  # ❌ Has extra "GitHub" level
```

**Why it wasn't caught**:
- ✅ Kubeconform: Valid YAML syntax, correct Kubernetes schema
- ❌ Deployment test: `nginx:alpine` doesn't read ASP.NET configuration
- ❌ Runtime: Application failed to load configuration

**Now caught by**: `validate-k8s-config.sh` (added to CI pipeline)

### Issue 2: Missing ConfigMap Key

**What happened**:
```yaml
- name: ProbotSharp__Adapters__Metrics__Options__OtlpEndpoint
  valueFrom:
    configMapKeyRef:
      name: probotsharp-config
      key: otlp-endpoint  # ❌ Key doesn't exist in ConfigMap
```

**Why it wasn't caught**:
- ✅ Kubeconform: Valid reference structure
- ❌ Deployment test: ConfigMap applied, but pod used stub image
- ❌ Runtime: Pod failed with `CreateContainerConfigError`

**Now caught by**: `validate-k8s-config.sh` validates referenced keys exist

### Issue 3: Nginx Cache Permission Error

**What happened**:
```yaml
securityContext:
  runAsNonRoot: true
  runAsUser: 1000
  readOnlyRootFilesystem: true
# ❌ nginx can't write to /var/cache/nginx
```

**Why it wasn't caught**:
- ✅ Kubeconform: Valid security context
- ❌ Deployment test: `nginx:alpine` has different cache behavior
- ❌ Runtime: Production nginx failed with permission denied

**Fixed by**: Adding `nginx-cache` volume mount

**Now caught by**: `validate-k8s-config.sh` checks for required volume mounts

## Validation Scripts

### validate-k8s-config.sh

Run before committing Kubernetes changes:

```bash
./scripts/validate-k8s-config.sh
```

**Checks**:
1. GitHub App configuration paths (`ProbotSharp__AppId` not `ProbotSharp__GitHub__AppId`)
2. Adapter configuration paths (`ProbotSharp__Adapters__Cache__Options__ConnectionString`)
3. ConfigMap key references exist
4. Required volume mounts for non-root containers
5. Security context is configured
6. Helm template consistency

### k8s-integration-test.sh

Run after deploying to a test cluster:

```bash
./scripts/k8s-integration-test.sh
```

**Requirements**:
- Kubernetes cluster with ProbotSharp deployed
- `kubectl` configured and connected
- Deployment uses **real ProbotSharp image** (not stub)

**Checks**:
1. Pods running without errors
2. No CrashLoopBackOff or restarts
3. Application logs clean (no errors/exceptions)
4. Environment variables properly set
5. Health endpoint HTTP 200
6. Root endpoint returns metadata
7. ConfigMap keys exist

## Testing Locally

### Pre-commit Validation

```bash
# Run all pre-commit hooks (includes k8s validation)
dotnet husky run

# Or run validation manually
./scripts/validate-k8s-config.sh
```

### Local Kubernetes Testing

```bash
# 1. Start a local kind cluster
kind create cluster --name probotsharp-test

# 2. Build and load Docker image
docker build -t ghcr.io/your-org/probot-sharp:test .
kind load docker-image ghcr.io/your-org/probot-sharp:test --name probotsharp-test

# 3. Deploy to kind cluster
kubectl apply -f deploy/k8s/namespace.yaml
kubectl create secret generic probotsharp-secrets -n probotsharp \
  --from-literal=database-connection-string="Host=postgres;Database=test" \
  --from-literal=github-app-id="12345" \
  --from-literal=github-webhook-secret="test-secret" \
  --from-literal=github-private-key="$(cat private-key.pem)" \
  --from-literal=redis-connection-string="redis:6379"
kubectl apply -f deploy/k8s/configmap.yaml
kubectl apply -f deploy/k8s/deployment.yaml
kubectl apply -f deploy/k8s/service.yaml

# 4. Wait for deployment
kubectl wait --for=condition=available --timeout=120s deployment/probotsharp -n probotsharp

# 5. Run integration tests
./scripts/k8s-integration-test.sh
```

### Testing with Act (GitHub Actions Locally)

```bash
# Test Kubernetes validation workflow
gh act pull_request -W .github/workflows/validate-kubernetes.yml -j validate-manifests

# Test Helm validation
gh act pull_request -W .github/workflows/validate-kubernetes.yml -j validate-helm
```

**Note**: Act runs fast static validation only (kubeconform + config validation), not full deployment with real image.

## CI/CD Pipeline

### Pull Request Pipeline

```
┌─────────────────────────────────────┐
│  validate-manifests                 │
│  ├─ Checkout                        │
│  ├─ Validate config consistency ◄── NEW!
│  └─ Kubeconform validation          │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│  validate-helm                      │
│  ├─ Checkout                        │
│  ├─ Helm lint                       │
│  └─ Kubeconform (Helm templates)    │
└─────────────────────────────────────┘
```

### Main Branch Pipeline

```
┌─────────────────────────────────────┐
│  deploy-test-manifests              │
│  ├─ Create kind cluster             │
│  ├─ Apply manifests                 │
│  ├─ Deploy with nginx:alpine stub   │  ⚠️ Doesn't test actual app config
│  └─ Basic smoke tests               │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│  deploy-test-helm                   │
│  ├─ Create kind cluster             │
│  ├─ Helm install                    │
│  ├─ Test upgrade/rollback           │
│  └─ Basic smoke tests               │
└─────────────────────────────────────┘
```

## Recommendations

### For Every PR
- ✅ Run `./scripts/validate-k8s-config.sh` before committing
- ✅ Let CI run kubeconform validation
- ✅ Review CI warnings about Helm consistency

### Before Production Deployment
- ✅ Build the actual Docker image
- ✅ Deploy to a staging/test cluster
- ✅ Run `./scripts/k8s-integration-test.sh`
- ✅ Verify health and webhook endpoints manually
- ✅ Test actual webhook delivery
- ✅ Check application logs for errors

### For Configuration Changes
- ✅ Validate environment variable paths match ASP.NET conventions
- ✅ Ensure ConfigMap/Secret keys exist
- ✅ Update both plain manifests AND Helm templates
- ✅ Test with real image, not stub

### To Improve CI Coverage

Consider adding a GitHub Actions job that:
1. Builds the actual ProbotSharp Docker image
2. Loads it into kind cluster
3. Deploys with real image (not nginx:alpine stub)
4. Runs `k8s-integration-test.sh`

**Trade-off**: Slower CI (5-10 minutes), but catches configuration issues earlier.

**Example workflow job**:
```yaml
deploy-test-real-image:
  name: Deploy Test (Real Image)
  runs-on: ubuntu-latest
  needs: validate-manifests
  if: github.event_name == 'push' && github.ref == 'refs/heads/main'
  steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Build Docker image
      run: docker build -t ghcr.io/probotsharp/probot-sharp:test .

    - name: Create kind cluster
      uses: helm/kind-action@v1

    - name: Load image into kind
      run: kind load docker-image ghcr.io/probotsharp/probot-sharp:test

    - name: Deploy with real image
      run: |
        kubectl apply -f deploy/k8s/namespace.yaml
        kubectl create secret generic probotsharp-secrets -n probotsharp \
          --from-literal=... # Create secrets
        kubectl apply -f deploy/k8s/configmap.yaml
        kubectl apply -f deploy/k8s/deployment.yaml

    - name: Run integration tests
      run: ./scripts/k8s-integration-test.sh
```

## Summary

| Testing Layer | Speed | Coverage | Catches Config Errors? | Runs On |
|---------------|-------|----------|------------------------|---------|
| Kubeconform | ⚡⚡⚡ Very Fast | Low | ❌ No | Every PR |
| Config Validation | ⚡⚡ Fast | Medium | ✅ Yes (paths, keys) | Every PR |
| Deployment Test (Stub) | ⚡ Medium | Medium | ❌ No | Push to main |
| Integration Test (Real) | 🐢 Slow | High | ✅ Yes (runtime) | Manual / Pre-release |

**Key Takeaway**: Static validation is fast but limited. Runtime testing with the real image is essential for catching configuration issues before production.
