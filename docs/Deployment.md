# Deployment Guide

This guide provides an overview of deploying ProbotSharp to production environments.

## Deployment Philosophy: Start Simple, Scale Later

ProbotSharp embraces the Probot philosophy: **don't use a database unless you need it**. You can start with a minimal deployment (no infrastructure dependencies) and scale up incrementally as your bot grows.

### Deployment Tiers

ProbotSharp supports three deployment tiers, each appropriate for different use cases:

| Tier | Infrastructure | Setup Time | Cost | Best For |
|------|----------------|------------|------|----------|
| **Minimal** | None (in-memory only) | 5 minutes | $0-13/mo | Learning, simple bots, low traffic |
| **Standard** | PostgreSQL | 30 minutes | $20-40/mo | Production single-instance |
| **Enterprise** | PostgreSQL + Redis + Queue | 2 hours | $85-90/mo | High-traffic, multi-instance |

**Start with Minimal** - Most GitHub Apps can run perfectly fine without databases. Only scale up when you actually need it.

See [Minimal Deployment Guide](MinimalDeployment.md) for zero-infrastructure deployment.

## Deployment Options

ProbotSharp supports multiple deployment platforms, each with complete Infrastructure as Code (IaC) templates and CI/CD configurations.

### Supported Platforms

| Platform | Best For | Complexity | Cost (est.) | Guide |
|----------|----------|------------|-------------|-------|
| **Minimal Deployment** | Learning, simple bots, prototypes | Very Low | $0-13/mo | [Minimal Guide](MinimalDeployment.md) |
| **AWS ECS Fargate** | AWS-native deployments, serverless containers | Medium | ~$85-90/mo | [AWS Guide](deployment/AWS.md) |
| **Kubernetes** | Multi-cloud, on-premises, maximum flexibility | High | Varies | [Kubernetes Guide](deployment/Kubernetes.md) |
| **Azure Web Apps** | Azure-native, simplest setup, PaaS | Low | ~$41-56/mo | [Azure Guide](deployment/Azure.md) |

### Quick Start: Choose Your Path

**Path 1: Minimal (Recommended for New Users)**
- No databases, no Docker, just run
- Perfect for learning and simple automation
- 5-minute setup
- See [Minimal Deployment Guide](MinimalDeployment.md)

**Path 2: Standard (Production-Ready)**
- PostgreSQL for persistence
- Single-instance deployment
- 30-minute setup
- Choose AWS, Kubernetes, or Azure below

**Path 3: Enterprise (High-Traffic)**
- PostgreSQL + Redis + Load Balancer
- Multi-instance, auto-scaling
- 2-hour setup
- Full platform guides below

### Quick Comparison

**AWS ECS Fargate**
- ✅ Fully managed container orchestration
- ✅ Auto-scaling, load balancing, health checks included
- ✅ Tight AWS ecosystem integration (RDS, ElastiCache, Secrets Manager)
- ⚠️ AWS vendor lock-in
- ⚠️ More expensive than self-managed Kubernetes

**Kubernetes**
- ✅ Cloud-agnostic (runs on EKS, GKE, AKS, on-prem)
- ✅ Maximum control and flexibility
- ✅ Large ecosystem of tools (Helm, Prometheus, ArgoCD)
- ⚠️ Requires Kubernetes expertise
- ⚠️ More operational overhead

**Azure Web Apps**
- ✅ Simplest deployment (PaaS)
- ✅ Built-in scaling, monitoring, logging
- ✅ Lowest operational overhead
- ✅ Deployment slots for zero-downtime updates
- ⚠️ Azure vendor lock-in
- ⚠️ Less control than containers

## Architecture Overview

All deployment options include these components:

### Core Application
- **ProbotSharp API** - ASP.NET Core 8 application handling webhooks
- **Worker Service** - Background service for webhook replay queue processing

### Data Layer
- **PostgreSQL** - Primary data store for webhooks, idempotency records
- **Redis** - Distributed cache for GitHub access tokens, idempotency tracking

### Supporting Services
- **Load Balancer** - HTTPS termination, traffic routing
- **Secrets Management** - Secure storage for GitHub credentials, database passwords
- **Monitoring** - Application metrics, logs, health checks
- **Container Registry** - Docker image storage

## Prerequisites

Before deploying to any platform, you need:

### 1. GitHub App

Create a GitHub App with:
- **App ID** - Unique identifier
- **Private Key** - RSA key in PEM format for JWT signing
- **Webhook Secret** - Secret for validating webhook signatures
- **Permissions** - Appropriate repository/organization permissions
- **Events** - Subscriptions to relevant webhook events

See [Local Development Guide - Create a GitHub App](LocalDevelopment.md#2-create-a-github-app) for detailed setup instructions.

### 2. Domain Name (Optional but Recommended)

For production deployments, you should have:
- A registered domain name
- DNS access to create A/CNAME records
- SSL/TLS certificate (or use Let's Encrypt/cloud-managed certificates)

### 3. Container Image

Build and push your Docker image:

```bash
# Build image
docker build -t probotsharp:latest .

# Tag for your registry
docker tag probotsharp:latest your-registry/probotsharp:v1.0.0

# Push to registry
docker push your-registry/probotsharp:v1.0.0
```

## Deployment Process

The general deployment workflow for all platforms:

### Step 1: Provision Infrastructure

Use the provided Infrastructure as Code templates:

- **AWS**: CloudFormation template (`deploy/aws/cloudformation.yaml`)
- **Kubernetes**: Helm chart (`deploy/k8s/helm/`) or plain manifests (`deploy/k8s/`)
- **Azure**: Bicep template (`deploy/azure/azuredeploy.bicep`)

These templates create:
- Compute resources (containers, VMs, serverless)
- Managed database (PostgreSQL)
- Managed cache (Redis)
- Networking (load balancers, firewalls, VPC/VNet)
- Security (secrets management, IAM roles, managed identities)
- Observability (logging, metrics, tracing)

### Step 2: Deploy Application

Deploy the ProbotSharp container image:

- **AWS**: Create ECS service using task definition
- **Kubernetes**: Install Helm chart or apply manifests
- **Azure**: Deploy to App Service via zip or container

### Step 3: Run Database Migrations

Initialize the database schema:

```bash
dotnet ef database update \
  --project src/ProbotSharp.Infrastructure \
  --startup-project src/ProbotSharp.Bootstrap.Api \
  --connection "Your connection string"
```

### Step 4: Configure GitHub Webhook

Update your GitHub App's webhook URL to point to your deployment:

```
https://your-domain.com/webhooks
```

Or use the load balancer/ingress URL provided by your cloud platform.

### Step 5: Verify Deployment

Test the deployment:

```bash
# Health check
curl https://your-domain.com/health

# Expected response:
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "duration": 123.45,
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "duration": 45.12
    },
    {
      "name": "cache",
      "status": "Healthy",
      "duration": 12.34
    },
    {
      "name": "github_api",
      "status": "Healthy",
      "duration": 234.56
    }
  ]
}
```

Test webhook processing by creating an issue or PR in a repository where your GitHub App is installed.

## Configuration

### Environment-Specific Settings

ProbotSharp uses environment-specific configuration files:

- `appsettings.json` - Development defaults
- `appsettings.Production.json` - Production overrides
- `appsettings.Staging.json` - Staging overrides

Set the `ASPNETCORE_ENVIRONMENT` environment variable to load the appropriate configuration:

```bash
ASPNETCORE_ENVIRONMENT=Production
```

### Required Configuration

All deployments require these settings:

```json
{
  "ConnectionStrings": {
    "ProbotSharp": "Host=db-host;Port=5432;Database=probotsharp;Username=user;Password=pass"
  },
  "ProbotSharp": {
    "GitHub": {
      "AppId": "123456",
      "WebhookSecret": "your-webhook-secret",
      "PrivateKey": "-----BEGIN RSA PRIVATE KEY-----\n...\n-----END RSA PRIVATE KEY-----"
    },
    "Adapters": {
      "Cache": {
        "Provider": "Redis",
        "Options": {
          "ConnectionString": "redis-host:6379"
        }
      },
      "Idempotency": {
        "Provider": "Redis",
        "Options": {
          "ConnectionString": "redis-host:6379"
        }
      },
      "Persistence": { "Provider": "PostgreSQL" },
      "ReplayQueue": { "Provider": "InMemory" },
      "DeadLetterQueue": { "Provider": "Database" },
      "Metrics": { "Provider": "OpenTelemetry" },
      "Tracing": { "Provider": "OpenTelemetry" }
    }
  }
}
```

### Secrets Management

**Never commit secrets to source control.** Use platform-specific secrets management:

- **AWS**: AWS Secrets Manager
- **Kubernetes**: Kubernetes Secrets or external secrets operators
- **Azure**: Azure Key Vault

Reference secrets in configuration:

**AWS ECS Task Definition:**
```json
{
  "secrets": [
    {
      "name": "ProbotSharp__GitHub__PrivateKey",
      "valueFrom": "arn:aws:secretsmanager:region:account:secret:name"
    }
  ]
}
```

**Kubernetes:**
```yaml
env:
  - name: ProbotSharp__GitHub__PrivateKey
    valueFrom:
      secretKeyRef:
        name: probotsharp-secrets
        key: github-private-key
```

**Azure:**
```
ProbotSharp__GitHub__PrivateKey = @Microsoft.KeyVault(SecretUri=https://vault.vault.azure.net/secrets/github-private-key)
```

## CI/CD

### GitHub Actions Workflows

Pre-configured workflows for each platform:

- `.github/workflows/deploy-aws.yml` - Deploy to AWS ECS
- `.github/workflows/deploy-k8s.yml` - Deploy to Kubernetes
- `.github/workflows/deploy-azure.yml` - Deploy to Azure Web Apps

#### Workflow Triggers

All workflows trigger on:
- Push to `main` branch (automatic deployment)
- Manual workflow dispatch (deploy on demand)

#### Required Secrets

Configure in GitHub repository settings → Secrets and variables → Actions:

**AWS:**
- `AWS_ROLE_ARN` or `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY`
- `DATABASE_CONNECTION_STRING`

**Kubernetes:**
- `KUBECONFIG` - Base64-encoded kubeconfig
- `GITHUB_APP_ID`, `GITHUB_WEBHOOK_SECRET`, `GITHUB_PRIVATE_KEY`
- `DATABASE_CONNECTION_STRING`, `REDIS_CONNECTION_STRING`

**Azure:**
- `AZURE_CREDENTIALS` - Service principal JSON
- `AZURE_DATABASE_CONNECTION_STRING`

### Continuous Deployment Workflow

1. Developer pushes code to `main` branch
2. GitHub Actions workflow triggered
3. Build application (restore, build, test)
4. Build and push Docker image (if applicable)
5. Deploy to cloud platform
6. Run database migrations
7. Health check verification
8. Notify on success/failure

## Scaling

### Horizontal Scaling

All platforms support horizontal scaling (multiple instances):

**AWS ECS:**
```bash
aws ecs update-service \
  --cluster probotsharp-cluster \
  --service probotsharp-service \
  --desired-count 5
```

**Kubernetes:**
```bash
kubectl scale deployment probotsharp --replicas=5 -n probotsharp
```

**Azure:**
```bash
az appservice plan update \
  --name probotsharp-plan \
  --resource-group probotsharp-rg \
  --number-of-workers 5
```

### Auto-Scaling

Configure auto-scaling based on metrics:

**AWS ECS** - Application Auto Scaling with CPU/memory targets

**Kubernetes** - HorizontalPodAutoscaler (HPA)
```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: probotsharp
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: probotsharp
  minReplicas: 2
  maxReplicas: 10
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70
```

**Azure** - Auto-scale rules based on CPU, memory, or custom metrics

### Vertical Scaling

Increase resources per instance:

**AWS ECS** - Update task definition CPU/memory

**Kubernetes** - Update deployment resources
```yaml
resources:
  requests:
    cpu: 500m
    memory: 1Gi
  limits:
    cpu: 2000m
    memory: 2Gi
```

**Azure** - Scale up App Service Plan tier (B1 → S1 → P1v2)

## Monitoring

### Health Checks

All deployments expose a `/health` endpoint that checks:
- Database connectivity
- Cache connectivity
- GitHub API connectivity

Configure health check probes:

**AWS ECS:**
```json
{
  "healthCheck": {
    "command": ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"],
    "interval": 30,
    "timeout": 5,
    "retries": 3
  }
}
```

**Kubernetes:**
```yaml
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

**Azure:**
Auto-configured in App Service with path `/health`

### Logging

Centralized logging is configured for all platforms:

- **AWS** - CloudWatch Logs
- **Kubernetes** - Aggregated with Fluentd/Fluent Bit to CloudWatch, Elasticsearch, or Loki
- **Azure** - Application Insights and Log Analytics

View logs:

```bash
# AWS
aws logs tail /ecs/probotsharp --follow

# Kubernetes
kubectl logs -n probotsharp -l app=probotsharp --tail=100 -f

# Azure
az webapp log tail --resource-group rg --name probotsharp
```

### Metrics

Application metrics are collected via:

- **OpenTelemetry** - Configured with `ProbotSharp__Adapters__Metrics__Provider=OpenTelemetry`
- **Platform-native** - CloudWatch (AWS), Prometheus (K8s), Application Insights (Azure)

Key metrics to monitor:
- Request rate and latency
- Error rate (4xx, 5xx responses)
- Webhook processing time
- Replay queue depth
- Dead-letter queue size
- Database connection pool utilization
- Cache hit rate

## Troubleshooting

### Common Issues

**Application Not Starting**
1. Check logs for startup errors
2. Verify environment variables are set correctly
3. Ensure database is accessible (firewall rules, security groups)
4. Verify secrets are accessible (IAM permissions, managed identity)

**Webhooks Not Received**
1. Check GitHub App webhook settings (correct URL)
2. Verify load balancer/ingress is routing traffic
3. Check webhook signature validation (correct webhook secret)
4. Review idempotency logs (possible duplicates being filtered)

**Database Connection Failures**
1. Verify connection string is correct
2. Check network connectivity (VPC/VNet peering, firewall rules)
3. Ensure database user has correct permissions
4. Test connection from application pod/container

**High CPU/Memory Usage**
1. Review Application Insights/CloudWatch for bottlenecks
2. Check for slow database queries (enable query logging)
3. Verify cache hit rate (low hit rate = more database queries)
4. Scale vertically (more CPU/RAM per instance) or horizontally (more instances)

### Platform-Specific Troubleshooting

See detailed troubleshooting sections in platform guides:
- [AWS Troubleshooting](deployment/AWS.md#troubleshooting)
- [Kubernetes Troubleshooting](deployment/Kubernetes.md#troubleshooting)
- [Azure Troubleshooting](deployment/Azure.md#troubleshooting)

## Security Best Practices

### Network Security

- Use private subnets for application and database (not internet-accessible)
- Restrict security group/firewall rules to minimum required ports
- Enable VPC/VNet peering between services
- Use TLS/SSL for all connections (database, cache, external APIs)

### Secrets Management

- Store all credentials in managed secrets services (Secrets Manager, Key Vault, Kubernetes Secrets)
- Rotate secrets regularly (GitHub App private key, database passwords)
- Use managed identities/service accounts instead of access keys where possible
- Never log or expose secrets in application logs or error messages

### Application Security

- Enable HTTPS only (redirect HTTP → HTTPS)
- Validate webhook signatures (prevents spoofed webhooks)
- Implement rate limiting (prevent abuse)
- Keep dependencies up to date (use Dependabot)
- Run security scans on container images

### Access Control

- Use principle of least privilege for IAM roles/policies
- Enable MFA for cloud account access
- Use separate environments (dev, staging, production) with different credentials
- Audit access logs regularly

## Maintenance

### Database Migrations

Run migrations before deploying new application versions:

```bash
# Check current migration status
dotnet ef migrations list --project src/ProbotSharp.Infrastructure

# Create new migration
dotnet ef migrations add MigrationName --project src/ProbotSharp.Infrastructure

# Apply migrations
dotnet ef database update --project src/ProbotSharp.Infrastructure
```

In CI/CD pipelines, migrations run automatically after deployment.

### Backup and Recovery

**Database Backups:**
- AWS RDS - Automated daily backups, point-in-time recovery
- Kubernetes - Manual backups with `pg_dump` or Velero
- Azure Database - Automated backups with geo-redundancy option

**Application Data:**
- Webhook replay queue and dead-letter queue are persisted to disk/volume
- Use persistent volumes with regular snapshots (EBS, Azure Disk, PersistentVolumeClaims)

### Updates

**Application Updates:**
1. Build and test new version locally
2. Deploy to staging environment
3. Run integration tests
4. Deploy to production via CI/CD or manual deployment
5. Monitor metrics and logs for issues

**Infrastructure Updates:**
1. Update IaC templates (CloudFormation, Bicep, Helm)
2. Test changes in non-production environment
3. Apply updates to production with change management process
4. Use blue/green or canary deployments for zero-downtime updates

## Cost Optimization

### General Tips

- Right-size resources (don't over-provision CPU/memory)
- Use auto-scaling to scale down during low traffic periods
- Use spot instances/preemptible VMs for non-production workloads
- Set appropriate log retention periods (7-30 days typically sufficient)
- Use reserved instances/savings plans for predictable workloads (20-40% savings)

### Platform-Specific

**AWS:**
- Use Fargate Spot for cost savings (up to 70% discount)
- Use Aurora Serverless v2 for variable database load
- Enable S3 lifecycle policies for log archival

**Kubernetes:**
- Use cluster autoscaler to scale nodes down
- Use spot/preemptible nodes for batch workloads
- Consolidate small workloads on fewer nodes

**Azure:**
- Use Azure Hybrid Benefit if you have Windows licenses
- Use Azure Reserved Instances for 1-3 year commitments
- Use Azure Dev/Test pricing for non-production

## Support and Resources

### Documentation

- [Local Development Guide](LocalDevelopment.md) - Set up local dev environment
- [Architecture Documentation](Architecture.md) - Understand the codebase structure
- [Operations Guide](Operations.md) - Day-to-day operations and monitoring

### Platform-Specific Guides

- [AWS Deployment Guide](deployment/AWS.md) - Complete AWS ECS setup
- [Kubernetes Deployment Guide](deployment/Kubernetes.md) - Complete Kubernetes setup
- [Azure Deployment Guide](deployment/Azure.md) - Complete Azure Web Apps setup

### External Resources

- [GitHub Apps Documentation](https://docs.github.com/en/developers/apps)
- [Probot Framework](https://probot.github.io/) - Conceptual foundation
- [ASP.NET Core Deployment](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)

### Getting Help

- **GitHub Issues** - Report bugs or request features
- **Discussions** - Ask questions and share knowledge
- **Stack Overflow** - Tag questions with `probotsharp` or `github-apps`

## Next Steps

1. Choose your deployment platform
2. Read the platform-specific deployment guide
3. Provision infrastructure using provided templates
4. Deploy the application
5. Configure monitoring and alerts
6. Set up automated backups
7. Document your deployment for your team
