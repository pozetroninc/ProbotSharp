# Kubernetes Base Configuration

This directory contains the base Kubernetes manifests for ProbotSharp.

## Contents

- `namespace.yaml` - Namespace and ServiceAccount
- `configmap.yaml` - Configuration for adapters (cache, idempotency, etc.)
- `deployment.yaml` - Main application deployment with production image
- `service.yaml` - LoadBalancer and headless services
- `kustomization.yaml` - Base kustomization configuration

## Usage

Deploy base configuration directly (production):
```bash
kubectl apply -k deploy/k8s/base/
```

Preview manifests:
```bash
kubectl kustomize deploy/k8s/base/
```

## Image Configuration

The base deployment uses: `ghcr.io/your-org/probot-sharp:latest`

This is a placeholder. For production, replace with your actual container registry.

## Overlays

Base configuration is extended by overlays in `../overlays/`:
- `test/` - CI/CD testing overlay (uses nginx:alpine)

See `../overlays/README.md` for details.
