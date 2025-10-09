#!/bin/bash
# Build and test a specific ProbotSharp example using Docker
# Usage: ./docker-test.sh <ExampleName>
# Example: ./docker-test.sh MinimalBot

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
CONTAINER_NAME="${IMAGE_NAME}-test"
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"

echo "========================================="
echo "Testing $EXAMPLE_NAME"
echo "========================================="
echo ""

# Build
echo "Step 1/4: Building image..."
docker build \
  -f "$REPO_ROOT/examples/Dockerfile" \
  --build-arg EXAMPLE_NAME="$EXAMPLE_NAME" \
  -t "$IMAGE_NAME:latest" \
  "$REPO_ROOT" > /dev/null 2>&1

if [ $? -eq 0 ]; then
  echo "✅ Build successful"
else
  echo "❌ Build failed"
  exit 1
fi

# Run container
echo "Step 2/4: Starting container..."
CONTAINER_ID=$(docker run -d -p 8080:5000 --name "$CONTAINER_NAME" "$IMAGE_NAME:latest" 2>/dev/null)

if [ $? -eq 0 ]; then
  echo "✅ Container started: $CONTAINER_ID"
else
  echo "❌ Container failed to start"
  exit 1
fi

# Wait for startup
echo "Step 3/4: Waiting for application startup..."
sleep 3

# Test health endpoint
echo "Step 4/4: Testing endpoints..."
HEALTH_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/health 2>/dev/null)

if [ "$HEALTH_CODE" = "200" ]; then
  echo "✅ Health check passed (HTTP $HEALTH_CODE)"
else
  echo "❌ Health check failed (HTTP $HEALTH_CODE)"
  docker logs "$CONTAINER_NAME" 2>&1 | tail -20
  docker stop "$CONTAINER_NAME" > /dev/null 2>&1
  docker rm "$CONTAINER_NAME" > /dev/null 2>&1
  exit 1
fi

# Test webhook endpoint
SIGNATURE=$(echo -n '{"test":true}' | openssl dgst -sha256 -hmac 'development' | awk '{print $2}')
WEBHOOK_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST http://localhost:8080/webhooks \
  -H "Content-Type: application/json" \
  -H "X-GitHub-Event: issues" \
  -H "X-GitHub-Delivery: test-123" \
  -H "X-Hub-Signature-256: sha256=$SIGNATURE" \
  -d '{"test":true}' 2>/dev/null)

if [ "$WEBHOOK_CODE" = "202" ]; then
  echo "✅ Webhook test passed (HTTP $WEBHOOK_CODE)"
else
  echo "❌ Webhook test failed (HTTP $WEBHOOK_CODE)"
  docker logs "$CONTAINER_NAME" 2>&1 | tail -20
  docker stop "$CONTAINER_NAME" > /dev/null 2>&1
  docker rm "$CONTAINER_NAME" > /dev/null 2>&1
  exit 1
fi

# Cleanup
echo ""
echo "Cleaning up..."
docker stop "$CONTAINER_NAME" > /dev/null 2>&1
docker rm "$CONTAINER_NAME" > /dev/null 2>&1

echo ""
echo "========================================="
echo "✅ $EXAMPLE_NAME - ALL TESTS PASSED"
echo "========================================="
echo ""
echo "Summary:"
echo "  Build:   ✅ Success"
echo "  Health:  ✅ HTTP $HEALTH_CODE"
echo "  Webhook: ✅ HTTP $WEBHOOK_CODE"
