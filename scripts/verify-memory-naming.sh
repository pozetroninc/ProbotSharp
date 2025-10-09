#!/bin/bash
set -e

echo "=== Memory/InMemory Naming Verification ==="

echo "1. Finding all 'Memory' provider references..."
grep -r '"Provider".*"Memory"' --include="*.json" --include="*.md" . | grep -v node_modules | grep -v bin | grep -v obj | tee /tmp/memory-providers.txt

echo ""
echo "2. Finding all MemoryAccessTokenCacheAdapter references..."
grep -r "MemoryAccessTokenCacheAdapter" --include="*.cs" --include="*.md" . | grep -v bin | grep -v obj | tee /tmp/memory-cache-refs.txt

echo ""
echo "3. Finding all MemoryIdempotencyAdapter references..."
grep -r "MemoryIdempotencyAdapter" --include="*.cs" --include="*.md" . | grep -v bin | grep -v obj | tee /tmp/memory-idempotency-refs.txt

echo ""
echo "4. Counting InMemory references for comparison..."
INMEMORY_COUNT=$(grep -r '"Provider".*"InMemory"' --include="*.json" . | grep -v node_modules | grep -v bin | grep -v obj | wc -l)
echo "InMemory provider count: $INMEMORY_COUNT"

echo ""
echo "=== Verification complete. Review files in /tmp/ ==="
