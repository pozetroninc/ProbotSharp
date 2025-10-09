#!/bin/bash
# Build all ProbotSharp examples using Docker
# Usage: ./docker-build-all.sh

set -e

EXAMPLES=(
  "MinimalBot"
  "HelloWorldBot"
  "AttachmentsBot"
  "SlashCommandsBot"
  "DryRunBot"
  "PaginationBot"
  "MetadataBot"
  "ExtensionsBot"
  "GraphQLBot"
  "HttpExtensibilityBot"
)

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"

echo "========================================="
echo "Building All ProbotSharp Examples"
echo "========================================="
echo ""
echo "Total examples: ${#EXAMPLES[@]}"
echo ""

SUCCESS=0
FAILED=0
FAILED_EXAMPLES=()

for EXAMPLE in "${EXAMPLES[@]}"; do
  IMAGE_NAME=$(echo "$EXAMPLE" | tr '[:upper:]' '[:lower:]')

  printf "%-25s" "$EXAMPLE:"

  if docker build \
    -f "$REPO_ROOT/examples/Dockerfile" \
    --build-arg EXAMPLE_NAME="$EXAMPLE" \
    -t "$IMAGE_NAME:latest" \
    "$REPO_ROOT" > /dev/null 2>&1; then

    echo "✅ Built $IMAGE_NAME:latest"
    ((SUCCESS++))
  else
    echo "❌ Build failed"
    ((FAILED++))
    FAILED_EXAMPLES+=("$EXAMPLE")
  fi
done

echo ""
echo "========================================="
echo "Build Summary"
echo "========================================="
echo "Total:   ${#EXAMPLES[@]}"
echo "Success: $SUCCESS ✅"
echo "Failed:  $FAILED"
echo ""

if [ $FAILED -eq 0 ]; then
  echo "🎉 All examples built successfully!"
  echo ""
  echo "To test all examples:"
  echo "  make test-all"
  echo "  OR"
  echo "  cd examples && make test-all"
  exit 0
else
  echo "⚠️  Some examples failed to build:"
  for FAILED_EXAMPLE in "${FAILED_EXAMPLES[@]}"; do
    echo "  - $FAILED_EXAMPLE"
  done
  echo ""
  echo "To debug a specific example:"
  echo "  ./docker-build.sh <ExampleName>"
  exit 1
fi
