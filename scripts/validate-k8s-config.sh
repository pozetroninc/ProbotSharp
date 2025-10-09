#!/usr/bin/env bash
# Validates Kubernetes manifests for configuration consistency
set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

ERRORS=0
WARNINGS=0

echo "🔍 Validating Kubernetes configuration consistency..."
echo ""

# Function to check if an environment variable path is valid
validate_env_path() {
  local env_name=$1
  local expected_pattern=$2
  local file=$3

  if echo "$env_name" | grep -qE "$expected_pattern"; then
    echo -e "${GREEN}✓${NC} Valid: $env_name"
    return 0
  else
    echo -e "${RED}✗${NC} Invalid: $env_name (expected pattern: $expected_pattern) in $file"
    ERRORS=$((ERRORS + 1))
    return 1
  fi
}

# Test 1: Validate GitHub configuration paths
echo "Test 1: Validating GitHub App configuration paths..."
GITHUB_ENV_VARS=$(grep -h "name: ProbotSharp__.*AppId\|name: ProbotSharp__.*WebhookSecret\|name: ProbotSharp__.*PrivateKey" \
  deploy/k8s/base/deployment.yaml \
  deploy/k8s/helm/templates/deployment.yaml 2>/dev/null | \
  sed 's/.*name: //' | sed 's/$//' | sort -u)

for env_var in $GITHUB_ENV_VARS; do
  # Should NOT contain "GitHub" in the path (incorrect: ProbotSharp__GitHub__AppId)
  if echo "$env_var" | grep -q "GitHub"; then
    echo -e "${RED}✗${NC} Invalid path: $env_var (should not contain 'GitHub' level)"
    echo -e "   ${YELLOW}Hint:${NC} Use 'ProbotSharp__AppId' not 'ProbotSharp__GitHub__AppId'"
    ERRORS=$((ERRORS + 1))
  else
    validate_env_path "$env_var" "^ProbotSharp__(AppId|WebhookSecret|PrivateKey)$" "deployment files"
  fi
done
echo ""

# Test 2: Validate adapter configuration paths
echo "Test 2: Validating adapter configuration paths..."
ADAPTER_ENV_VARS=$(grep -h "name: ProbotSharp__Adapters__" \
  deploy/k8s/base/deployment.yaml \
  deploy/k8s/helm/templates/deployment.yaml 2>/dev/null | \
  grep "ConnectionString\|Provider" | \
  sed 's/.*name: //' | sed 's/$//' | sort -u)

for env_var in $ADAPTER_ENV_VARS; do
  # ConnectionString should follow pattern: ProbotSharp__Adapters__[Type]__Options__ConnectionString
  if echo "$env_var" | grep -q "ConnectionString"; then
    validate_env_path "$env_var" "^ProbotSharp__Adapters__[A-Za-z]+__Options__ConnectionString$" "deployment files"
  fi

  # Provider should follow pattern: ProbotSharp__Adapters__[Type]__Provider
  if echo "$env_var" | grep -q "Provider"; then
    validate_env_path "$env_var" "^ProbotSharp__Adapters__[A-Za-z]+__Provider$" "deployment files"
  fi
done
echo ""

# Test 3: Check for ConfigMap key references
echo "Test 3: Validating ConfigMap key references..."
if [ -f "deploy/k8s/base/configmap.yaml" ]; then
  CONFIGMAP_KEYS=$(grep "^  [A-Za-z]" deploy/k8s/base/configmap.yaml | awk '{print $1}' | sed 's/:$//')

  # Check deployment.yaml for configMapKeyRef references
  REFERENCED_KEYS=$(grep -h "configMapKeyRef:" -A 2 deploy/k8s/base/deployment.yaml 2>/dev/null | \
    grep "key:" | awk '{print $NF}' | sort -u)

  for key in $REFERENCED_KEYS; do
    if echo "$CONFIGMAP_KEYS" | grep -q "^$key$"; then
      echo -e "${GREEN}✓${NC} ConfigMap key exists: $key"
    else
      echo -e "${RED}✗${NC} ConfigMap key NOT found: $key"
      echo -e "   ${YELLOW}Available keys:${NC}"
      echo "$CONFIGMAP_KEYS" | sed 's/^/     /'
      ERRORS=$((ERRORS + 1))
    fi
  done
else
  echo -e "${YELLOW}⚠${NC} No ConfigMap file found (deploy/k8s/base/configmap.yaml)"
  WARNINGS=$((WARNINGS + 1))
fi
echo ""

# Test 4: Check for required volume mounts for non-root containers
echo "Test 4: Validating volume mounts for non-root containers..."
REQUIRED_VOLUMES=("tmp" "nginx-cache")

for vol in "${REQUIRED_VOLUMES[@]}"; do
  if grep -q "name: $vol" deploy/k8s/base/deployment.yaml && \
     grep -q "mountPath:" deploy/k8s/base/deployment.yaml; then
    echo -e "${GREEN}✓${NC} Volume mount exists: $vol"
  else
    echo -e "${RED}✗${NC} Missing volume mount: $vol"
    echo -e "   ${YELLOW}Hint:${NC} Non-root containers need writable directories"
    ERRORS=$((ERRORS + 1))
  fi
done
echo ""

# Test 5: Verify securityContext is set
echo "Test 5: Validating security context..."
if grep -q "runAsNonRoot: true" deploy/k8s/base/deployment.yaml && \
   grep -q "runAsUser:" deploy/k8s/base/deployment.yaml; then
  echo -e "${GREEN}✓${NC} Security context configured"
else
  echo -e "${YELLOW}⚠${NC} Security context not fully configured"
  WARNINGS=$((WARNINGS + 1))
fi
echo ""

# Test 6: Check Helm template consistency
echo "Test 6: Validating Helm template configuration..."
if [ -f "deploy/k8s/helm/templates/deployment.yaml" ]; then
  # Check environment variable counts
  PLAIN_ENV_COUNT=$(grep -c "name: ProbotSharp__" deploy/k8s/base/deployment.yaml || echo 0)
  HELM_ENV_COUNT=$(grep -c "name: ProbotSharp__" deploy/k8s/helm/templates/deployment.yaml || echo 0)

  if [ "$PLAIN_ENV_COUNT" -eq "$HELM_ENV_COUNT" ]; then
    echo -e "${GREEN}✓${NC} Both deployment methods have full feature parity ($PLAIN_ENV_COUNT variables)"
    echo "     Plain manifests and Helm charts are production-ready with:"
    echo "       • Retry policies (configurable webhook replay)"
    echo "       • Data retention (dead letter queue management)"
    echo "       • Observability (OpenTelemetry metrics and tracing)"
  elif [ "$HELM_ENV_COUNT" -gt "$PLAIN_ENV_COUNT" ]; then
    # Helm having MORE variables suggests plain manifests are missing features
    echo -e "${YELLOW}⚠${NC} Helm template has MORE configuration than plain manifest:"
    echo "     Plain manifest: $PLAIN_ENV_COUNT"
    echo "     Helm template:  $HELM_ENV_COUNT"
    echo ""
    echo "     Consider adding missing configuration to plain manifests for feature parity."
    WARNINGS=$((WARNINGS + 1))
  else
    # Plain manifest having MORE variables is suspicious
    echo -e "${YELLOW}⚠${NC} Plain manifest has MORE variables than Helm template:"
    echo "     Plain manifest: $PLAIN_ENV_COUNT"
    echo "     Helm template:  $HELM_ENV_COUNT"
    echo ""
    echo "     This may indicate missing Helm configuration."
    WARNINGS=$((WARNINGS + 1))
  fi
else
  echo -e "${YELLOW}⚠${NC} No Helm template found"
  WARNINGS=$((WARNINGS + 1))
fi
echo ""

# Summary
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
if [ $ERRORS -eq 0 ] && [ $WARNINGS -eq 0 ]; then
  echo -e "${GREEN}✅ All validation checks passed!${NC}"
  exit 0
elif [ $ERRORS -eq 0 ]; then
  echo -e "${YELLOW}⚠️  Validation completed with $WARNINGS warning(s)${NC}"
  exit 0
else
  echo -e "${RED}❌ Validation failed with $ERRORS error(s) and $WARNINGS warning(s)${NC}"
  echo ""
  echo "Common fixes:"
  echo "  • GitHub config: Use 'ProbotSharp__AppId' not 'ProbotSharp__GitHub__AppId'"
  echo "  • Adapter paths: Use 'ProbotSharp__Adapters__Cache__Options__ConnectionString'"
  echo "  • ConfigMap keys: Ensure all referenced keys exist in configmap.yaml"
  echo "  • Volume mounts: Add nginx-cache volume for non-root nginx containers"
  exit 1
fi
