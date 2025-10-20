#!/bin/bash
set -e

echo "=== Testing Coverage Extraction Logic ==="

# Create test directory
mkdir -p /tmp/test-coverage

# Test Case 1: Actual ReportGenerator Summary.json format
cat > /tmp/test-coverage/Summary.json << 'EOF'
{
  "summary": {
    "generatedon": "2025-10-20T04:27:46Z",
    "linecoverage": 69.8,
    "branchcoverage": 60.8,
    "methodcoverage": 80.5,
    "fullmethodcoverage": 63.2
  },
  "coverage": {
    "assemblies": []
  }
}
EOF

echo "=== Test 1: Extract from actual format ==="
LINE=$(jq -r '.summary.linecoverage // 0' /tmp/test-coverage/Summary.json 2>/dev/null)
BRANCH=$(jq -r '.summary.branchcoverage // 0' /tmp/test-coverage/Summary.json 2>/dev/null)
METHOD=$(jq -r '.summary.methodcoverage // 0' /tmp/test-coverage/Summary.json 2>/dev/null)

echo "Extracted LINE: $LINE (expected: 69.8)"
echo "Extracted BRANCH: $BRANCH (expected: 60.8)"
echo "Extracted METHOD: $METHOD (expected: 80.5)"

if [ "$LINE" = "69.8" ] && [ "$BRANCH" = "60.8" ] && [ "$METHOD" = "80.5" ]; then
    echo "✅ Test 1 PASSED"
else
    echo "❌ Test 1 FAILED"
    exit 1
fi

# Test Case 2: Missing summary object (should fallback to 0)
cat > /tmp/test-coverage/broken.json << 'EOF'
{
  "coverage": {
    "assemblies": []
  }
}
EOF

echo ""
echo "=== Test 2: Missing summary (should fallback to 0) ==="
LINE=$(jq -r '.summary.linecoverage // 0' /tmp/test-coverage/broken.json 2>/dev/null)
echo "Extracted LINE: $LINE (expected: 0)"

if [ "$LINE" = "0" ]; then
    echo "✅ Test 2 PASSED"
else
    echo "❌ Test 2 FAILED"
    exit 1
fi

# Test Case 3: Delta calculation
echo ""
echo "=== Test 3: Delta calculation ==="
PR_LINE=72.5
BASE_LINE=69.8
DELTA=$(echo "$PR_LINE - $BASE_LINE" | bc)
echo "Delta: $DELTA (expected: 2.7)"

if [ "$DELTA" = "2.7" ]; then
    echo "✅ Test 3 PASSED"
else
    echo "❌ Test 3 FAILED"
    exit 1
fi

# Cleanup
rm -rf /tmp/test-coverage

echo ""
echo "=== All Tests Passed ✅ ==="
