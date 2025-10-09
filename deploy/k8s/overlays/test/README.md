# Test Overlay

Kustomize overlay for CI/CD testing of Kubernetes manifests.

## Purpose

This overlay extends the base configuration (`../../base/`) and replaces the production Docker image with `nginxinc/nginx-unprivileged:alpine`. This allows GitHub Actions workflows to validate Kubernetes deployment structure without requiring the actual ProbotSharp image.

## What It Does

- **Extends base:** Includes all base resources (namespace, configmap, deployment, service)
- **Image replacement:** Changes `ghcr.io/your-org/probot-sharp:latest` → `nginxinc/nginx-unprivileged:alpine`
- **Test-ready:** nginx-unprivileged runs as non-root, listens on port 8080, and matches production security constraints

## Usage

### Apply to cluster:
```bash
kubectl apply -k deploy/k8s/overlays/test/
```

### Preview manifests:
```bash
kubectl kustomize deploy/k8s/overlays/test/
```

### Validate without applying:
```bash
kubectl kustomize deploy/k8s/overlays/test/ | kubectl apply --dry-run=client -f -
```

## GitHub Actions Integration

This overlay is used by `.github/workflows/validate-kubernetes.yml`:

```yaml
- name: Deploy with test image
  run: kubectl apply -k deploy/k8s/overlays/test/
```

## Why This Structure?

**Standard Kustomize Pattern:**
```
deploy/k8s/
├── base/              <- Base configuration
└── overlays/
    └── test/          <- Test overlay (references ../../base)
```

This structure:
- ✅ Follows Kustomize best practices
- ✅ No security flags needed
- ✅ Clean separation of base vs environment-specific config
- ✅ Easy to add more overlays (staging, production, etc.)

## Extending the Overlay

Add patches to `kustomization.yaml`:

```yaml
# Reduce replicas for testing
patchesStrategicMerge:
  - |-
    apiVersion: apps/v1
    kind: Deployment
    metadata:
      name: probotsharp
      namespace: probotsharp
    spec:
      replicas: 1
```

## Related Documentation

- [Base Configuration](../../base/README.md)
- [Kustomize Documentation](https://kustomize.io/)
- [ProbotSharp Deployment Guide](../../../../docs/deployment/Kubernetes.md)
