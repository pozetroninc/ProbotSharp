#!/usr/bin/env bash
set -euo pipefail

NAMESPACE="probotsharp"

echo "🧪 Running Kubernetes smoke tests..."

# Test 1: Pods are running
echo "✓ Test 1: Checking if pods are running..."
POD_COUNT=$(kubectl get pods -n "$NAMESPACE" -l "app.kubernetes.io/name=probotsharp" --field-selector=status.phase=Running --no-headers 2>/dev/null | wc -l)
if [ "$POD_COUNT" -lt 1 ]; then
  echo "❌ No running pods found!"
  exit 1
fi
echo "✅ Found $POD_COUNT running pod(s)"

# Test 2: Service exists
echo "✓ Test 2: Checking if service exists..."
if ! kubectl get service/probotsharp -n "$NAMESPACE" >/dev/null 2>&1; then
  echo "❌ Service not found!"
  exit 1
fi
echo "✅ Service exists"

# Test 3: Deployment is available
echo "✓ Test 3: Checking if deployment is available..."
DEPLOYMENT_STATUS=$(kubectl get deployment/probotsharp -n "$NAMESPACE" -o jsonpath='{.status.conditions[?(@.type=="Available")].status}' 2>/dev/null)
if [ "$DEPLOYMENT_STATUS" != "True" ]; then
  echo "❌ Deployment not available!"
  exit 1
fi
echo "✅ Deployment is available"

# Test 4: ConfigMap exists
echo "✓ Test 4: Checking if ConfigMap exists..."
if ! kubectl get configmap -n "$NAMESPACE" >/dev/null 2>&1; then
  echo "❌ No ConfigMaps found!"
  exit 1
fi
echo "✅ ConfigMap(s) exist"

# Test 5: Secrets exist
echo "✓ Test 5: Checking if secrets exist..."
if ! kubectl get secrets -n "$NAMESPACE" >/dev/null 2>&1; then
  echo "❌ No Secrets found!"
  exit 1
fi
echo "✅ Secret(s) exist"

echo ""
echo "🎉 All smoke tests passed!"
exit 0
