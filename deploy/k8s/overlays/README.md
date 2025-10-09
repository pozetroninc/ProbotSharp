# Kubernetes Overlays

This directory contains Kustomize overlays that extend the base configuration for specific environments.

## Structure

```
overlays/
└── test/           - CI/CD testing overlay
```

## Test Overlay

The test overlay (`test/`) is used by GitHub Actions to validate Kubernetes manifests without requiring the actual ProbotSharp Docker image.

**Key modifications:**
- Replaces production image with `nginx:alpine`
- Can add test-specific resource adjustments

**Usage:**
```bash
kubectl apply -k deploy/k8s/overlays/test/
```

**See:** `test/README.md` for details

## Creating New Overlays

To create a new overlay (e.g., for staging):

1. Create directory: `mkdir -p deploy/k8s/overlays/staging`
2. Create kustomization.yaml:

```yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

resources:
  - ../../base

# Add staging-specific modifications here
```

## Best Practices

- Base configuration should be environment-agnostic
- Overlays should contain only environment-specific changes
- Use image tags (not `latest`) for production overlays
- Test overlays locally before pushing:
  ```bash
  kubectl kustomize deploy/k8s/overlays/<overlay-name>
  ```
