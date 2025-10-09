# Kubernetes Deployment Guide

This guide covers deploying ProbotSharp to Kubernetes clusters (EKS, GKE, AKS, or self-hosted).

## Architecture Overview

- **Container Runtime**: Docker containers running ASP.NET Core 8.0
- **Container Registry**: GitHub Container Registry (ghcr.io)
- **Database**: PostgreSQL (external or in-cluster)
- **Cache**: Redis (external or in-cluster)
- **Service Type**: LoadBalancer with optional Ingress
- **Auto-scaling**: Horizontal Pod Autoscaler (HPA)
- **Monitoring**: Prometheus-compatible metrics endpoint

## Prerequisites

### Required Tools

1. **kubectl** - Kubernetes command-line tool
   ```bash
   # Install on Linux
   curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
   chmod +x kubectl
   sudo mv kubectl /usr/local/bin/

   # Install on macOS
   brew install kubectl

   # Verify installation
   kubectl version --client
   ```

2. **Access to a Kubernetes cluster** (v1.24+)
   - Amazon EKS, Google GKE, Azure AKS, or self-hosted
   - Configure your kubeconfig: `kubectl config use-context your-cluster`

3. **Container image** pushed to a registry
   - GitHub Container Registry (recommended)
   - Docker Hub, AWS ECR, GCR, or ACR

### Optional Tools

- **helm** - Package manager for Kubernetes (required for Helm deployment method)
- **cert-manager** - For automatic TLS certificate management
- **NGINX Ingress Controller** - For ingress support

## Deployment Methods

This directory provides two deployment approaches:

### Method 1: Helm Chart (Recommended)

**Location:** [`helm/`](helm/)

**Best for:**
- Production deployments
- Teams familiar with Helm
- Need for parameterized configuration
- Automated PostgreSQL/Redis deployment
- Simplified upgrades and rollbacks

**Features:**
- Single command deployment
- Built-in PostgreSQL and Redis subcharts
- Comprehensive `values.yaml` configuration
- Template-based manifest generation
- Version management with `helm upgrade`/`rollback`

**Quick start:**
```bash
# Install ProbotSharp with Helm
helm install probotsharp ./deploy/k8s/helm \
  --namespace probotsharp \
  --create-namespace \
  --set secrets.github.appId="YOUR_APP_ID" \
  --set secrets.github.webhookSecret="YOUR_SECRET" \
  --set secrets.github.privateKey="YOUR_PRIVATE_KEY"
```

See [Helm Chart Documentation](#helm-chart-deployment) for detailed configuration.

---

### Method 2: kubectl with Plain Manifests

**Location:** Root of `deploy/k8s/` directory

**Best for:**
- Learning Kubernetes concepts
- Full control over every manifest
- Custom deployments requiring manual tweaks
- Environments where Helm is not available
- GitOps workflows with tools like ArgoCD

**Features:**
- Individual YAML manifests
- No Helm dependency
- Direct kubectl apply workflow
- Full transparency of all resources
- Production-ready configuration (retry policies, data retention, observability)
- Feature parity with Helm charts

**Quick start:**
```bash
# Apply all manifests
kubectl apply -f deploy/k8s/namespace.yaml
kubectl apply -f deploy/k8s/secret.yaml
kubectl apply -f deploy/k8s/configmap.yaml
kubectl apply -f deploy/k8s/deployment.yaml
kubectl apply -f deploy/k8s/service.yaml
```

See [kubectl Deployment](#kubectl-deployment) for detailed steps.

---

## Helm Chart Deployment

### Prerequisites

Ensure you have Helm 3.x installed:

```bash
# Install Helm on Linux
curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash

# Install Helm on macOS
brew install helm

# Verify installation
helm version
```

### Step 1: Review Configuration

Edit `helm/values.yaml` to configure your deployment. Key settings:

```yaml
# Container image
image:
  repository: ghcr.io/your-org/probot-sharp
  tag: "latest"

# GitHub App credentials
secrets:
  github:
    appId: "YOUR_APP_ID"
    webhookSecret: "YOUR_WEBHOOK_SECRET"
    privateKey: "YOUR_PRIVATE_KEY_PEM"

# Database and cache
postgresql:
  enabled: true  # Deploy PostgreSQL in-cluster
  auth:
    password: "SET_STRONG_PASSWORD"

redis:
  enabled: true  # Deploy Redis in-cluster
```

See [`helm/values.yaml`](helm/values.yaml) for all configuration options.

### Step 2: Install Chart

```bash
# Install with custom values
helm install probotsharp ./deploy/k8s/helm \
  --namespace probotsharp \
  --create-namespace \
  --values deploy/k8s/helm/values.yaml

# Or install with command-line overrides
helm install probotsharp ./deploy/k8s/helm \
  --namespace probotsharp \
  --create-namespace \
  --set image.tag="v1.0.0" \
  --set secrets.github.appId="123456" \
  --set secrets.github.webhookSecret="secret" \
  --set secrets.github.privateKey="$(cat private-key.pem)"
```

### Step 3: Verify Deployment

```bash
# Check all resources
helm status probotsharp -n probotsharp

# Watch pods come up
kubectl get pods -n probotsharp -w

# Check logs
kubectl logs -n probotsharp -l app.kubernetes.io/name=probotsharp --tail=50
```

### Step 4: Upgrade

```bash
# Upgrade with new values
helm upgrade probotsharp ./deploy/k8s/helm \
  --namespace probotsharp \
  --values deploy/k8s/helm/values.yaml

# Rollback if needed
helm rollback probotsharp -n probotsharp
```

### Step 5: Uninstall

```bash
helm uninstall probotsharp -n probotsharp
```

---

## kubectl Deployment

### 1. Create Namespace and Service Account

```bash
kubectl apply -f deploy/k8s/namespace.yaml
```

This creates:
- `probotsharp` namespace
- `probotsharp` service account

### 2. Create Secrets

Copy the example secret file and customize it:

```bash
# Copy the example
cp deploy/k8s/secret.yaml.example deploy/k8s/secret.yaml

# Edit with your values
nano deploy/k8s/secret.yaml
```

Required secrets:
- `database-connection-string` - PostgreSQL connection string
- `github-app-id` - Your GitHub App ID
- `github-webhook-secret` - GitHub webhook secret
- `github-private-key` - GitHub App private key (PEM format)
- `redis-connection-string` - Redis connection string

Apply the secrets:

```bash
kubectl apply -f deploy/k8s/secret.yaml
```

**Important:** Do NOT commit `secret.yaml` to version control. It's in `.gitignore`.

### 3. Apply Configuration

```bash
kubectl apply -f deploy/k8s/configmap.yaml
```

**Configuration Highlights:**

The plain manifests now include production-ready configuration options for:

- **Retry Policies** - Configure webhook replay behavior:
  - `MaxRetryAttempts: "5"` - Number of retry attempts for failed webhooks
  - `RetryBaseDelayMs: "2000"` - Base delay between retries (exponential backoff)
  - `PollIntervalMs: "1000"` - Polling frequency for replay queue

- **Data Retention** - Control dead letter queue retention:
  - `RetentionDays: "30"` - How long to keep failed webhooks

- **Observability** - OpenTelemetry integration:
  - `OtlpEndpoint: "http://otel-collector:4317"` - Metrics and tracing endpoint
  - Compatible with Prometheus, Grafana, Jaeger, Zipkin, and other OTLP-compatible tools

All configuration values are stored in `configmap.yaml` and can be customized per environment without rebuilding container images.

### 4. Deploy the Application

```bash
# Update the image in deployment.yaml to match your registry
kubectl apply -f deploy/k8s/deployment.yaml
```

### 5. Create Service

```bash
kubectl apply -f deploy/k8s/service.yaml
```

This creates:
- A LoadBalancer service for external access
- A headless service for internal pod-to-pod communication

### 6. Set Up Ingress (Optional)

If you have an Ingress controller installed:

```bash
# Edit ingress.yaml to set your domain
nano deploy/k8s/ingress.yaml

# Apply ingress
kubectl apply -f deploy/k8s/ingress.yaml
```

### 7. Enable Auto-scaling (Optional)

```bash
kubectl apply -f deploy/k8s/hpa.yaml
```

### 8. Verify Deployment

```bash
# Check pod status
kubectl get pods -n probotsharp

# Check service
kubectl get services -n probotsharp

# Check logs
kubectl logs -n probotsharp -l app=probotsharp --tail=50
```

## CI/CD Validation

The repository includes automated Kubernetes validation workflows that run on every pull request and push to main.

### Validation Workflow

**Workflow:** `.github/workflows/validate-kubernetes.yml`

**What it does:**

1. **Validate Plain Manifests** (runs on every PR)
   - Validates all YAML files in `deploy/k8s/` with kubeconform
   - Checks against Kubernetes schema for correctness
   - Runs in strict mode to catch errors early

2. **Validate Helm Chart** (runs on every PR)
   - Lints the Helm chart for best practices
   - Templates the chart with test values
   - Validates rendered templates with kubeconform

3. **Deploy Test with Manifests** (runs on push to main)
   - Creates ephemeral kind cluster
   - Deploys using plain manifests
   - Runs smoke tests to verify functionality
   - Cleans up automatically

4. **Deploy Test with Helm** (runs on push to main)
   - Creates separate ephemeral kind cluster
   - Installs Helm chart
   - Tests upgrade and rollback functionality
   - Runs smoke tests to verify functionality

### Running Validation Locally

Validate manifests before committing:

```bash
# Validate plain manifests
./tools/kubeconform/kubeconform \
  -schema-location 'tools/kubeconform/schemas/{{ .NormalizedKubernetesVersion }}-standalone{{ .StrictSuffix }}/{{ .ResourceKind }}{{ .KindSuffix }}.json' \
  -strict -summary deploy/k8s/*.yaml

# Lint Helm chart
helm lint deploy/k8s/helm --strict

# Validate Helm templates
helm template test deploy/k8s/helm \
  --values deploy/k8s/helm/test-values.yaml | \
  ./tools/kubeconform/kubeconform \
    -schema-location 'tools/kubeconform/schemas/{{ .NormalizedKubernetesVersion }}-standalone{{ .StrictSuffix }}/{{ .ResourceKind }}{{ .KindSuffix }}.json' \
    -strict -summary -
```

**Pre-commit hook:** Kubeconform validation runs automatically via Husky pre-commit hooks for changed K8s files.

## GitHub Actions Deployment

### Setup GitHub Secrets

Add these secrets to your GitHub repository:

1. **KUBECONFIG** - Base64-encoded kubeconfig file
   ```bash
   cat ~/.kube/config | base64 -w 0
   ```

2. **DATABASE_CONNECTION_STRING** - PostgreSQL connection string

3. **GITHUB_APP_ID** - Your GitHub App ID

4. **GITHUB_WEBHOOK_SECRET** - GitHub webhook secret

5. **GITHUB_PRIVATE_KEY** - GitHub App private key

6. **REDIS_CONNECTION_STRING** - Redis connection string

### Trigger Deployment

Push to main branch:
```bash
git push origin main
```

Or manually trigger:
```bash
gh workflow run deploy-k8s.yml
```

## Manual Deployment Steps

### Build and Push Docker Image

```bash
# Build image (from repository root)
docker build -f src/ProbotSharp.Bootstrap.Api/Dockerfile -t probotsharp:latest .

# Tag for registry
docker tag probotsharp:latest ghcr.io/your-org/probot-sharp:latest
docker tag probotsharp:latest ghcr.io/your-org/probot-sharp:v1.0.0

# Login to GitHub Container Registry
echo $GITHUB_TOKEN | docker login ghcr.io -u your-username --password-stdin

# Push images
docker push ghcr.io/your-org/probot-sharp:latest
docker push ghcr.io/your-org/probot-sharp:v1.0.0
```

### Update Deployment Image

```bash
# Option 1: Using kubectl set image
kubectl set image deployment/probotsharp \
  probotsharp=ghcr.io/your-org/probot-sharp:v1.0.0 \
  -n probotsharp

# Option 2: Edit deployment YAML and reapply
kubectl apply -f deploy/k8s/deployment.yaml

# Watch rollout
kubectl rollout status deployment/probotsharp -n probotsharp
```

### Run Database Migrations

```bash
# Option 1: From local machine
# Get database connection string from secret
DB_CONN=$(kubectl get secret probotsharp-secrets -n probotsharp \
  -o jsonpath='{.data.database-connection-string}' | base64 -d)

dotnet ef database update \
  --project src/ProbotSharp.Infrastructure \
  --startup-project src/ProbotSharp.Bootstrap.Api \
  --connection "$DB_CONN"

# Option 2: Run as Kubernetes Job
kubectl run migrations \
  --image=ghcr.io/your-org/probot-sharp:latest \
  --restart=Never \
  --namespace=probotsharp \
  --env="ConnectionStrings__ProbotSharp=$DB_CONN" \
  --command -- dotnet ef database update
```

## Configuration

### Environment Variables

Set in `configmap.yaml` - see [Adapter Configuration](../../docs/AdapterConfiguration.md) for complete details:
- Adapter providers (Cache, Idempotency, Persistence, ReplayQueue, DeadLetterQueue, Metrics, Tracing)
- Provider-specific options (connection strings, timeouts, retention policies, etc.)

### Resource Limits

Default resource requests and limits:

```yaml
resources:
  requests:
    cpu: 250m
    memory: 512Mi
  limits:
    cpu: 1000m
    memory: 1Gi
```

Adjust based on your workload in `deployment.yaml`.

### Scaling

#### Manual Scaling

```bash
# Scale to 5 replicas
kubectl scale deployment probotsharp --replicas=5 -n probotsharp
```

#### Auto-scaling (HPA)

The HPA is configured to:
- Minimum replicas: 2
- Maximum replicas: 10
- Target CPU utilization: 70%
- Target memory utilization: 80%

Modify `hpa.yaml` to adjust scaling behavior.

### Health Checks

Three types of probes configured:

1. **Liveness Probe** - Restarts unhealthy pods
   - Path: `/health`
   - Initial delay: 30s
   - Period: 10s

2. **Readiness Probe** - Controls traffic routing
   - Path: `/health`
   - Initial delay: 10s
   - Period: 5s

3. **Startup Probe** - Protects slow-starting containers
   - Path: `/health`
   - Max wait: 150s (30 failures × 5s)

## Ingress Setup

### Install NGINX Ingress Controller

```bash
# Using Helm
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update
helm install ingress-nginx ingress-nginx/ingress-nginx \
  --namespace ingress-nginx \
  --create-namespace

# Or using kubectl
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.8.1/deploy/static/provider/cloud/deploy.yaml
```

### Install cert-manager (for TLS)

```bash
# Install cert-manager
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml

# Wait for cert-manager to be ready
kubectl wait --for=condition=ready pod -l app.kubernetes.io/instance=cert-manager -n cert-manager --timeout=300s
```

### Configure DNS

Point your domain to the Ingress LoadBalancer IP:

```bash
# Get LoadBalancer IP
kubectl get service ingress-nginx-controller -n ingress-nginx

# Create A record: probotsharp.yourdomain.com -> LoadBalancer IP
```

### Apply Ingress

```bash
# Edit ingress.yaml with your domain
sed -i 's/probotsharp.yourdomain.com/your-actual-domain.com/g' deploy/k8s/ingress.yaml

# Apply
kubectl apply -f deploy/k8s/ingress.yaml

# Check certificate status
kubectl describe certificate probotsharp-tls -n probotsharp
```

## Monitoring and Logging

### View Logs

```bash
# All pods
kubectl logs -n probotsharp -l app=probotsharp --tail=100 -f

# Specific pod
kubectl logs -n probotsharp probotsharp-xxxx-yyyy -f

# Previous container (if crashed)
kubectl logs -n probotsharp probotsharp-xxxx-yyyy --previous
```

### Check Pod Status

```bash
# List pods
kubectl get pods -n probotsharp

# Describe pod
kubectl describe pod probotsharp-xxxx-yyyy -n probotsharp

# Get events
kubectl get events -n probotsharp --sort-by='.lastTimestamp'
```

### Metrics

Access Prometheus-compatible metrics:

```bash
# Port-forward to local machine
kubectl port-forward -n probotsharp service/probotsharp 8080:80

# Access metrics
curl http://localhost:8080/metrics
```

### Resource Usage

```bash
# Pod resource usage
kubectl top pods -n probotsharp

# Node resource usage
kubectl top nodes
```

## Troubleshooting

### Pods Not Starting

1. **Check pod events**:
   ```bash
   kubectl describe pod probotsharp-xxxx-yyyy -n probotsharp
   ```

2. **Common issues**:
   - Image pull errors: Check image name and registry credentials
   - Secret not found: Ensure `probotsharp-secrets` exists
   - Resource limits: Nodes may not have enough resources

3. **Fix ImagePullBackOff**:
   ```bash
   # Create image pull secret for private registries
   kubectl create secret docker-registry ghcr-secret \
     --docker-server=ghcr.io \
     --docker-username=your-username \
     --docker-password=$GITHUB_TOKEN \
     -n probotsharp

   # Add to deployment.yaml under spec.template.spec:
   # imagePullSecrets:
   #   - name: ghcr-secret
   ```

### Database Connection Issues

1. **Verify secret**:
   ```bash
   kubectl get secret probotsharp-secrets -n probotsharp \
     -o jsonpath='{.data.database-connection-string}' | base64 -d
   ```

2. **Test connectivity from pod**:
   ```bash
   kubectl exec -it -n probotsharp probotsharp-xxxx-yyyy -- /bin/sh
   nc -zv postgres-host 5432
   ```

3. **Check network policies**: Ensure pods can reach database

### Redis Connection Issues

1. **Verify Redis secret**:
   ```bash
   kubectl get secret probotsharp-secrets -n probotsharp \
     -o jsonpath='{.data.redis-connection-string}' | base64 -d
   ```

2. **Test Redis connectivity**:
   ```bash
   kubectl exec -it -n probotsharp probotsharp-xxxx-yyyy -- /bin/sh
   nc -zv redis-host 6379
   ```

### LoadBalancer Pending

If service stays in `Pending` state:

```bash
kubectl get service probotsharp -n probotsharp

# Possible causes:
# 1. Cloud provider doesn't support LoadBalancer
# 2. Quota limits reached
# 3. Incorrect cloud provider configuration

# Workaround: Use NodePort or Ingress instead
# Edit service.yaml and change type to NodePort
```

### Certificate Issues

1. **Check cert-manager logs**:
   ```bash
   kubectl logs -n cert-manager -l app=cert-manager
   ```

2. **Check certificate status**:
   ```bash
   kubectl describe certificate probotsharp-tls -n probotsharp
   kubectl describe certificaterequest -n probotsharp
   ```

3. **Check challenge status**:
   ```bash
   kubectl get challenges -n probotsharp
   kubectl describe challenge -n probotsharp
   ```

### High Memory Usage

1. **Check pod metrics**:
   ```bash
   kubectl top pods -n probotsharp
   ```

2. **Increase memory limits** in deployment.yaml

3. **Check for memory leaks** in application logs

### Deployment Rollout Failed

1. **Check rollout status**:
   ```bash
   kubectl rollout status deployment/probotsharp -n probotsharp
   kubectl rollout history deployment/probotsharp -n probotsharp
   ```

2. **Rollback to previous version**:
   ```bash
   kubectl rollout undo deployment/probotsharp -n probotsharp
   ```

## Security Best Practices

1. **Use RBAC**: Create minimal service account permissions
2. **Network Policies**: Restrict pod-to-pod communication
3. **Pod Security Standards**: Enable restricted PSS
4. **Secrets Management**: Consider external secret stores
   - Sealed Secrets
   - External Secrets Operator
   - HashiCorp Vault
5. **Image Scanning**: Scan container images for vulnerabilities
6. **Read-only filesystem**: Enabled in deployment.yaml
7. **Non-root user**: Container runs as user 1000
8. **Drop capabilities**: All Linux capabilities dropped

## Production Checklist

- [ ] Configure persistent storage for stateful data (if needed)
- [ ] Set up monitoring (Prometheus, Grafana)
- [ ] Configure log aggregation (ELK, Loki)
- [ ] Enable auto-scaling (HPA)
- [ ] Set up ingress with TLS
- [ ] Configure network policies
- [ ] Set resource requests and limits
- [ ] Enable pod disruption budgets
- [ ] Set up backup strategy for database
- [ ] Configure alerting
- [ ] Test disaster recovery procedures
- [ ] Document runbooks for common operations
- [ ] Set up cost monitoring and budgets

## Cost Optimization

1. **Right-size resources**: Monitor actual usage and adjust limits
2. **Use cluster autoscaler**: Scale nodes based on demand
3. **Use spot/preemptible instances**: For non-critical workloads
4. **Set pod disruption budgets**: Allow safe node draining
5. **Use namespace resource quotas**: Prevent resource exhaustion
6. **Consider managed databases**: May be more cost-effective

## Additional Resources

- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [kubectl Cheat Sheet](https://kubernetes.io/docs/reference/kubectl/cheatsheet/)
- [NGINX Ingress Controller](https://kubernetes.github.io/ingress-nginx/)
- [cert-manager Documentation](https://cert-manager.io/docs/)
- [Horizontal Pod Autoscaler](https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/)
- [Pod Security Standards](https://kubernetes.io/docs/concepts/security/pod-security-standards/)
