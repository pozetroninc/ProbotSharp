#!/usr/bin/env bash
# Enhanced Kubernetes integration tests that validate actual application behavior
set -euo pipefail

NAMESPACE="probotsharp"
TIMEOUT=120

echo "🧪 Running Kubernetes integration tests..."

# Test 1: Pods are running
echo "✓ Test 1: Checking if pods are running..."
POD_COUNT=$(kubectl get pods -n "$NAMESPACE" -l app=probotsharp --field-selector=status.phase=Running --no-headers 2>/dev/null | wc -l)
if [ "$POD_COUNT" -lt 1 ]; then
  echo "❌ No running pods found!"
  kubectl get pods -n "$NAMESPACE" -l app=probotsharp
  exit 1
fi
echo "✅ Found $POD_COUNT running pod(s)"

# Test 2: Check for CrashLoopBackOff or ImagePullBackOff
echo "✓ Test 2: Checking for pod errors..."
ERROR_PODS=$(kubectl get pods -n "$NAMESPACE" -l app=probotsharp -o jsonpath='{.items[?(@.status.phase!="Running")].metadata.name}' 2>/dev/null || echo "")
if [ -n "$ERROR_PODS" ]; then
  echo "❌ Found pods in error state: $ERROR_PODS"
  kubectl describe pods -n "$NAMESPACE" -l app=probotsharp | grep -A 10 "Events:"
  exit 1
fi
echo "✅ No pods in error state"

# Test 3: Check pod logs for configuration errors
echo "✓ Test 3: Checking logs for configuration errors..."
POD_NAME=$(kubectl get pods -n "$NAMESPACE" -l app=probotsharp -o jsonpath='{.items[0].metadata.name}')
if kubectl logs -n "$NAMESPACE" "$POD_NAME" --tail=100 | grep -i "error\|exception\|failed to bind\|unknown.*provider\|configuration"; then
  echo "❌ Found errors in pod logs!"
  echo "Recent logs:"
  kubectl logs -n "$NAMESPACE" "$POD_NAME" --tail=50
  exit 1
fi
echo "✅ No configuration errors in logs"

# Test 4: Verify environment variables are set
echo "✓ Test 4: Verifying environment variables..."
REQUIRED_ENVS=(
  "ProbotSharp__AppId"
  "ProbotSharp__WebhookSecret"
  "ProbotSharp__PrivateKey"
  "ProbotSharp__Adapters__Cache__Options__ConnectionString"
)
for env_var in "${REQUIRED_ENVS[@]}"; do
  if ! kubectl exec -n "$NAMESPACE" "$POD_NAME" -- env | grep -q "^$env_var="; then
    echo "❌ Required environment variable missing: $env_var"
    echo "Available environment variables:"
    kubectl exec -n "$NAMESPACE" "$POD_NAME" -- env | grep "ProbotSharp" || echo "(none)"
    exit 1
  fi
done
echo "✅ All required environment variables are set"

# Test 5: Health endpoint responds
echo "✓ Test 5: Testing health endpoint..."
kubectl port-forward -n "$NAMESPACE" "service/probotsharp" 8080:8080 &
PF_PID=$!
sleep 5

if ! curl -f -s --max-time 10 http://localhost:8080/health > /dev/null; then
  echo "❌ Health endpoint failed!"
  kill $PF_PID 2>/dev/null || true
  exit 1
fi
echo "✅ Health endpoint responding"

# Test 6: Root endpoint returns metadata
echo "✓ Test 6: Testing root endpoint..."
RESPONSE=$(curl -s --max-time 10 http://localhost:8080/ || echo "")
if ! echo "$RESPONSE" | grep -q "application\|webhooks"; then
  echo "❌ Root endpoint not returning expected metadata!"
  echo "Response: $RESPONSE"
  kill $PF_PID 2>/dev/null || true
  exit 1
fi
echo "✅ Root endpoint returning metadata"

kill $PF_PID 2>/dev/null || true

# Test 7: Validate ConfigMap keys exist
echo "✓ Test 7: Validating ConfigMap keys..."
CONFIGMAP_KEYS=$(kubectl get configmap probotsharp-config -n "$NAMESPACE" -o jsonpath='{.data}' 2>/dev/null || echo "{}")
if [ "$CONFIGMAP_KEYS" == "{}" ]; then
  echo "⚠️  Warning: ConfigMap is empty or doesn't exist"
else
  echo "✅ ConfigMap has keys"
fi

# Test 8: Check for container restart count
echo "✓ Test 8: Checking container restart count..."
RESTART_COUNT=$(kubectl get pods -n "$NAMESPACE" -l app=probotsharp -o jsonpath='{.items[0].status.containerStatuses[0].restartCount}')
if [ "$RESTART_COUNT" -gt 0 ]; then
  echo "⚠️  Warning: Container has restarted $RESTART_COUNT time(s)"
  echo "Recent events:"
  kubectl get events -n "$NAMESPACE" --sort-by='.lastTimestamp' | tail -10
else
  echo "✅ No container restarts"
fi

echo ""
echo "🎉 All integration tests passed!"
exit 0
