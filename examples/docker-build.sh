#!/bin/bash
# Build a specific ProbotSharp example using Docker
# Usage: ./docker-build.sh <ExampleName>
# Example: ./docker-build.sh MinimalBot

set -e

if [ -z "$1" ]; then
  echo "Usage: $0 <ExampleName>"
  echo ""
  echo "Available examples:"
  echo "  - MinimalBot"
  echo "  - HelloWorldBot"
  echo "  - AttachmentsBot"
  echo "  - SlashCommandsBot"
  echo "  - DryRunBot"
  echo "  - PaginationBot"
  echo "  - MetadataBot"
  echo "  - ExtensionsBot"
  echo "  - GraphQLBot"
  echo "  - HttpExtensibilityBot"
  exit 1
fi

EXAMPLE_NAME="$1"
IMAGE_NAME=$(echo "$EXAMPLE_NAME" | tr '[:upper:]' '[:lower:]')
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"

echo "========================================="
echo "Building $EXAMPLE_NAME"
echo "========================================="
echo ""
echo "Example:     $EXAMPLE_NAME"
echo "Image:       $IMAGE_NAME:latest"
echo "Dockerfile:  examples/Dockerfile"
echo "Context:     $REPO_ROOT"
echo ""

docker build \
  -f "$REPO_ROOT/examples/Dockerfile" \
  --build-arg EXAMPLE_NAME="$EXAMPLE_NAME" \
  -t "$IMAGE_NAME:latest" \
  "$REPO_ROOT"

echo ""
echo "✅ Successfully built $IMAGE_NAME:latest"
echo ""
echo "To run:"
echo "  docker run -p 8080:5000 -e ProbotSharp__WebhookSecret=development $IMAGE_NAME:latest"
echo ""
echo "To test:"
echo "  ./docker-test.sh $EXAMPLE_NAME"
