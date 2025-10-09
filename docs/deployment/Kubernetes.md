# Kubernetes Deployment Guide

This guide covers deploying ProbotSharp to Kubernetes clusters, including managed services like Amazon EKS, Google GKE, Azure AKS, and self-hosted clusters.

## Architecture Overview

The Kubernetes deployment includes:
- **Deployment** - Application pods with 2+ replicas
- **Service** - ClusterIP service for internal routing
- **Ingress** - NGINX Ingress for external access with TLS
- **HorizontalPodAutoscaler** - Automatic scaling based on CPU/memory
- **PersistentVolumeClaim** - Shared storage for replay queue and DLQ
- **ConfigMap** - Non-sensitive configuration
- **Secret** - Sensitive credentials (GitHub, database, Redis)
- **ServiceAccount** - Pod identity for accessing cloud resources

## Prerequisites

### Required Tools

- [kubectl](https://kubernetes.io/docs/tasks/tools/) v1.25 or later
- [Helm](https://helm.sh/docs/intro/install/) v3.x
- Access to a Kubernetes cluster (EKS, GKE, AKS, or self-hosted)
- [Docker](https://www.docker.com/products/docker-desktop) for building images
- Domain name and DNS access (for Ingress)

### Kubernetes Cluster Requirements

Minimum cluster specifications:
- **Nodes**: 2+ nodes (for high availability)
- **Node resources**: 2 vCPU, 4 GB RAM per node
- **Kubernetes version**: 1.25+
- **Networking**: CNI plugin installed (Calico, Weave, etc.)
- **Storage**: Dynamic volume provisioning (for PVC)

### Required Kubernetes Addons

Install these addons if not already present:

**1. NGINX Ingress Controller**

```bash
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update

helm install ingress-nginx ingress-nginx/ingress-nginx \
  --namespace ingress-nginx \
  --create-namespace \
  --set controller.service.type=LoadBalancer
```

**2. cert-manager (for TLS certificates)**

```bash
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml
```

Create ClusterIssuer for Let's Encrypt:

```yaml
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: your-email@example.com
    privateKeySecretRef:
      name: letsencrypt-prod
    solvers:
      - http01:
          ingress:
            class: nginx
```

```bash
kubectl apply -f cluster-issuer.yaml
```

**3. Metrics Server (for HPA)**

```bash
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml
```

### GitHub App Credentials

You need:
- GitHub App ID
- GitHub App Private Key (PEM format)
- GitHub Webhook Secret

See the [Local Development Guide](../LocalDevelopment.md#2-create-a-github-app) for GitHub App setup instructions.

## Deployment Methods

### Method 1: Helm Chart (Recommended)

Helm provides the easiest way to deploy and manage ProbotSharp on Kubernetes.

#### Step 1: Add Container Registry

If using a private registry (e.g., GitHub Container Registry):

```bash
kubectl create secret docker-registry ghcr-secret \
  --docker-server=ghcr.io \
  --docker-username=your-github-username \
  --docker-password=your-github-token \
  --namespace default
```

#### Step 2: Build and Push Container Image

```bash
# Build image
docker build -t probotsharp:latest .

# Tag for your registry
docker tag probotsharp:latest ghcr.io/yourusername/probotsharp:latest
docker tag probotsharp:latest ghcr.io/yourusername/probotsharp:$(git rev-parse --short HEAD)

# Push to registry
docker push ghcr.io/yourusername/probotsharp:latest
docker push ghcr.io/yourusername/probotsharp:$(git rev-parse --short HEAD)
```

#### Step 3: Configure values.yaml

Create a custom `values-production.yaml`:

```yaml
replicaCount: 2

image:
  repository: ghcr.io/yourusername/probotsharp
  tag: "latest"
  pullPolicy: Always

imagePullSecrets:
  - name: ghcr-secret

ingress:
  enabled: true
  className: "nginx"
  annotations:
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
  hosts:
    - host: probotsharp.yourdomain.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: probotsharp-tls
      hosts:
        - probotsharp.yourdomain.com

secrets:
  databaseConnectionString: "Host=postgres;Port=5432;Database=probotsharp;Username=probotsharp;Password=YOUR_PASSWORD"
  github:
    appId: "123456"
    webhookSecret: "your-webhook-secret"
    privateKey: |
      -----BEGIN RSA PRIVATE KEY-----
      YOUR_PRIVATE_KEY_HERE
      -----END RSA PRIVATE KEY-----
  redis:
    connectionString: "redis:6379"

# If using managed PostgreSQL and Redis, disable embedded instances
postgresql:
  enabled: false

redis:
  enabled: false

resources:
  limits:
    cpu: 1000m
    memory: 1Gi
  requests:
    cpu: 250m
    memory: 512Mi

autoscaling:
  enabled: true
  minReplicas: 2
  maxReplicas: 10
  targetCPUUtilizationPercentage: 70
  targetMemoryUtilizationPercentage: 80
```

#### Step 4: Install with Helm

```bash
helm install probotsharp ./deploy/k8s/helm \
  --namespace probotsharp \
  --create-namespace \
  --values values-production.yaml
```

#### Step 5: Verify Deployment

```bash
# Check pods
kubectl get pods -n probotsharp

# Check services
kubectl get svc -n probotsharp

# Check ingress
kubectl get ingress -n probotsharp

# View logs
kubectl logs -n probotsharp -l app.kubernetes.io/name=probotsharp --tail=100 -f
```

#### Step 6: Run Database Migrations

Run migrations as a Kubernetes Job:

```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: probotsharp-migration
  namespace: probotsharp
spec:
  template:
    spec:
      containers:
        - name: migration
          image: ghcr.io/yourusername/probotsharp:latest
          command:
            - dotnet
            - ef
            - database
            - update
            - --project
            - /app/ProbotSharp.Infrastructure.dll
          env:
            - name: ConnectionStrings__ProbotSharp
              valueFrom:
                secretKeyRef:
                  name: probotsharp-secrets
                  key: database-connection-string
      restartPolicy: OnFailure
```

```bash
kubectl apply -f migration-job.yaml
kubectl wait --for=condition=complete --timeout=300s job/probotsharp-migration -n probotsharp
kubectl logs job/probotsharp-migration -n probotsharp
```

#### Step 7: Configure DNS

Point your domain to the Ingress load balancer:

```bash
# Get Ingress IP/hostname
kubectl get ingress -n probotsharp probotsharp -o jsonpath='{.status.loadBalancer.ingress[0].ip}'
# Or for hostname-based (AWS ELB)
kubectl get ingress -n probotsharp probotsharp -o jsonpath='{.status.loadBalancer.ingress[0].hostname}'
```

Create DNS A record (or CNAME for hostname) pointing to this address.

#### Step 8: Update GitHub Webhook URL

1. Wait for TLS certificate to be issued (check with `kubectl get certificate -n probotsharp`)
2. Go to your GitHub App settings
3. Update **Webhook URL** to `https://probotsharp.yourdomain.com/webhooks`
4. Save changes

#### Step 9: Test Deployment

```bash
# Health check
curl https://probotsharp.yourdomain.com/health

# Root endpoint
curl https://probotsharp.yourdomain.com/
```

### Method 2: kubectl with Raw Manifests

If you prefer not to use Helm, deploy using raw Kubernetes manifests.

#### Step 1: Create Namespace

```bash
kubectl create namespace probotsharp
```

#### Step 2: Create Secrets

Copy and customize the secrets template:

```bash
cp deploy/k8s/secret.yaml.example secrets.yaml
```

Edit `secrets.yaml` with your actual credentials, then apply:

```bash
kubectl apply -f secrets.yaml -n probotsharp
```

#### Step 3: Create ConfigMap

```bash
kubectl apply -f deploy/k8s/configmap.yaml -n probotsharp
```

#### Step 4: Deploy Application

```bash
kubectl apply -f deploy/k8s/deployment.yaml -n probotsharp
```

This creates:
- Deployment with 2 replicas
- Service (ClusterIP)
- PersistentVolumeClaim
- Ingress
- HorizontalPodAutoscaler

#### Step 5: Verify and Continue

Follow steps 5-9 from Method 1 (Helm).

### Method 3: GitHub Actions (CI/CD)

Automate deployments using GitHub Actions.

#### Step 1: Set Up Kubernetes Access

Create a service account with deployment permissions:

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: github-actions
  namespace: probotsharp
---
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: deployment-manager
  namespace: probotsharp
rules:
  - apiGroups: ["apps", ""]
    resources: ["deployments", "pods", "services", "configmaps", "secrets"]
    verbs: ["get", "list", "watch", "create", "update", "patch"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: github-actions-deployment
  namespace: probotsharp
subjects:
  - kind: ServiceAccount
    name: github-actions
    namespace: probotsharp
roleRef:
  kind: Role
  name: deployment-manager
  apiGroup: rbac.authorization.k8s.io
```

```bash
kubectl apply -f github-actions-rbac.yaml
```

#### Step 2: Get Service Account Token

```bash
# Create token
kubectl create token github-actions -n probotsharp --duration=87600h

# Or for older clusters, get secret token
SECRET_NAME=$(kubectl get serviceaccount github-actions -n probotsharp -o jsonpath='{.secrets[0].name}')
kubectl get secret $SECRET_NAME -n probotsharp -o jsonpath='{.data.token}' | base64 -d
```

#### Step 3: Get Cluster Info

```bash
# Get cluster CA certificate
kubectl config view --raw --minify --flatten -o jsonpath='{.clusters[0].cluster.certificate-authority-data}'

# Get cluster endpoint
kubectl config view --raw --minify --flatten -o jsonpath='{.clusters[0].cluster.server}'
```

#### Step 4: Configure GitHub Secrets

Add to repository secrets:
- `KUBECONFIG` - Base64-encoded kubeconfig (or use `KUBE_CONFIG_DATA`)
- `GITHUB_APP_ID` - GitHub App ID
- `GITHUB_WEBHOOK_SECRET` - Webhook secret
- `GITHUB_PRIVATE_KEY` - Private key
- `DATABASE_CONNECTION_STRING` - Database connection string
- `REDIS_CONNECTION_STRING` - Redis connection string

Alternatively, create a minimal kubeconfig:

```yaml
apiVersion: v1
kind: Config
clusters:
  - name: production
    cluster:
      server: https://your-cluster-endpoint
      certificate-authority-data: YOUR_CA_CERT_BASE64
contexts:
  - name: production
    context:
      cluster: production
      user: github-actions
      namespace: probotsharp
current-context: production
users:
  - name: github-actions
    user:
      token: YOUR_SERVICE_ACCOUNT_TOKEN
```

Base64 encode and add as `KUBECONFIG` secret:

```bash
cat kubeconfig.yaml | base64 | pbcopy
```

#### Step 5: Push to Main

Every push to `main` triggers deployment via `.github/workflows/deploy-k8s.yml`.

## Managing the Deployment

### Updating the Application

#### Using Helm

```bash
helm upgrade probotsharp ./deploy/k8s/helm \
  --namespace probotsharp \
  --values values-production.yaml \
  --set image.tag=new-version
```

#### Using kubectl

```bash
kubectl set image deployment/probotsharp \
  probotsharp=ghcr.io/yourusername/probotsharp:new-version \
  -n probotsharp

# Or update the deployment YAML and apply
kubectl apply -f deployment.yaml -n probotsharp
```

### Scaling

#### Manual Scaling

```bash
kubectl scale deployment probotsharp --replicas=5 -n probotsharp
```

#### Adjust Autoscaling

```bash
kubectl patch hpa probotsharp -n probotsharp --patch '{"spec":{"maxReplicas":20}}'
```

### Rolling Back

```bash
# View deployment history
kubectl rollout history deployment/probotsharp -n probotsharp

# Rollback to previous version
kubectl rollout undo deployment/probotsharp -n probotsharp

# Rollback to specific revision
kubectl rollout undo deployment/probotsharp --to-revision=2 -n probotsharp
```

### Updating Configuration

#### ConfigMap

```bash
kubectl edit configmap probotsharp-config -n probotsharp
# Restart pods to pick up changes
kubectl rollout restart deployment/probotsharp -n probotsharp
```

#### Secrets

```bash
kubectl edit secret probotsharp-secrets -n probotsharp
kubectl rollout restart deployment/probotsharp -n probotsharp
```

## Monitoring and Logging

### Viewing Logs

```bash
# All pods
kubectl logs -n probotsharp -l app.kubernetes.io/name=probotsharp --tail=100 -f

# Specific pod
kubectl logs -n probotsharp probotsharp-abc123-xyz -f

# Previous crashed container
kubectl logs -n probotsharp probotsharp-abc123-xyz --previous
```

### Checking Pod Status

```bash
# List pods
kubectl get pods -n probotsharp

# Describe pod (for events and issues)
kubectl describe pod probotsharp-abc123-xyz -n probotsharp

# Get pod YAML
kubectl get pod probotsharp-abc123-xyz -n probotsharp -o yaml
```

### Checking Health

```bash
# Port-forward for local testing
kubectl port-forward -n probotsharp svc/probotsharp 8080:80

# In another terminal
curl http://localhost:8080/health
```

### Resource Usage

```bash
# CPU and memory usage
kubectl top pods -n probotsharp

# Node usage
kubectl top nodes

# HPA status
kubectl get hpa probotsharp -n probotsharp
```

### Events

```bash
kubectl get events -n probotsharp --sort-by='.lastTimestamp'
```

## Advanced Configuration

### Using External PostgreSQL and Redis

If you have managed database services (e.g., AWS RDS, Azure Database, GCP Cloud SQL):

1. Disable embedded databases in `values.yaml`:

```yaml
postgresql:
  enabled: false

redis:
  enabled: false
```

2. Update connection strings in secrets:

```yaml
secrets:
  databaseConnectionString: "Host=mydb.region.rds.amazonaws.com;Port=5432;Database=probotsharp;Username=probotsharp;Password=SECURE_PASSWORD;SSL Mode=Require"
  redis:
    connectionString: "myredis.region.cache.amazonaws.com:6379,ssl=True,abortConnect=False"
```

### Using Cloud-Specific Features

#### Amazon EKS with IAM Roles for Service Accounts (IRSA)

Instead of storing credentials in secrets, use IRSA:

1. Create IAM role with necessary permissions (Secrets Manager, RDS)
2. Annotate service account:

```yaml
serviceAccount:
  create: true
  annotations:
    eks.amazonaws.com/role-arn: arn:aws:iam::ACCOUNT_ID:role/probotsharp-sa-role
```

3. Application can now access AWS services without credentials

#### Google GKE with Workload Identity

```yaml
serviceAccount:
  create: true
  annotations:
    iam.gke.io/gcp-service-account: probotsharp@PROJECT_ID.iam.gserviceaccount.com
```

#### Azure AKS with Azure AD Pod Identity

```yaml
apiVersion: aadpodidentity.k8s.io/v1
kind: AzureIdentity
metadata:
  name: probotsharp-identity
spec:
  type: 0
  resourceID: /subscriptions/SUBSCRIPTION_ID/resourcegroups/RG_NAME/providers/Microsoft.ManagedIdentity/userAssignedIdentities/probotsharp-identity
  clientID: CLIENT_ID
```

### Persistent Storage Options

By default, a `ReadWriteMany` PVC is used. Adjust for your cluster:

**AWS EKS (EFS)**

```yaml
persistence:
  enabled: true
  storageClass: "efs-sc"
  accessMode: ReadWriteMany
  size: 10Gi
```

**GKE (Filestore)**

```yaml
persistence:
  enabled: true
  storageClass: "standard-rwx"
  accessMode: ReadWriteMany
  size: 10Gi
```

**AKS (Azure Files)**

```yaml
persistence:
  enabled: true
  storageClass: "azurefile"
  accessMode: ReadWriteMany
  size: 10Gi
```

## Troubleshooting

### Pods Not Starting

```bash
# Check pod status
kubectl get pods -n probotsharp

# Describe pod for events
kubectl describe pod probotsharp-abc123-xyz -n probotsharp

# Check logs
kubectl logs probotsharp-abc123-xyz -n probotsharp
```

Common issues:
- **ImagePullBackOff** - Check image name/tag and registry credentials
- **CrashLoopBackOff** - Application is crashing, check logs
- **Pending** - Insufficient resources or PVC not binding

### Ingress Not Working

```bash
# Check Ingress status
kubectl get ingress -n probotsharp
kubectl describe ingress probotsharp -n probotsharp

# Check Ingress controller logs
kubectl logs -n ingress-nginx -l app.kubernetes.io/name=ingress-nginx

# Check certificate
kubectl get certificate -n probotsharp
kubectl describe certificate probotsharp-tls -n probotsharp
```

### Database Connection Failures

```bash
# Exec into pod to test connectivity
kubectl exec -it -n probotsharp probotsharp-abc123-xyz -- /bin/sh

# Test DNS resolution
nslookup postgres

# Test database connection (if psql available)
apt-get update && apt-get install -y postgresql-client
psql -h postgres -U probotsharp -d probotsharp
```

### High Memory/CPU Usage

Scale up resources:

```yaml
resources:
  limits:
    cpu: 2000m
    memory: 2Gi
  requests:
    cpu: 500m
    memory: 1Gi
```

```bash
helm upgrade probotsharp ./deploy/k8s/helm \
  --namespace probotsharp \
  --values values-production.yaml \
  --reuse-values
```

## Cleanup

### Uninstall with Helm

```bash
helm uninstall probotsharp -n probotsharp
kubectl delete namespace probotsharp
```

### Delete with kubectl

```bash
kubectl delete -f deploy/k8s/deployment.yaml -n probotsharp
kubectl delete namespace probotsharp
```

## Cost Optimization

- Use **spot instances** for non-production workloads
- Enable **cluster autoscaling** to scale nodes down during low usage
- Use **Horizontal Pod Autoscaler** to scale pods based on load
- Set appropriate **resource requests/limits** to avoid over-provisioning
- Use **storage classes** with appropriate IOPS for your needs

## Next Steps

- Set up [Prometheus and Grafana](https://prometheus.io/docs/visualization/grafana/) for monitoring
- Configure [Jaeger or Zipkin](https://www.jaegertracing.io/) for distributed tracing
- Implement [Network Policies](https://kubernetes.io/docs/concepts/services-networking/network-policies/) for pod-to-pod security
- Set up [Pod Disruption Budgets](https://kubernetes.io/docs/concepts/workloads/pods/disruptions/) for high availability
- Use [Kustomize](https://kustomize.io/) or [ArgoCD](https://argoproj.github.io/cd/) for GitOps workflows
