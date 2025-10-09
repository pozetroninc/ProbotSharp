#!/bin/bash
# Test a ProbotSharp example bot with Docker
# Usage: ./test-example.sh EXAMPLE_NAME [PORT]
#
# Examples:
#   ./test-example.sh MinimalBot
#   ./test-example.sh HelloWorldBot 8080

set -e

if [ -z "$1" ]; then
    echo "Usage: $0 EXAMPLE_NAME [PORT]"
    echo "Example: $0 MinimalBot 8080"
    exit 1
fi

EXAMPLE_NAME="$1"
PORT="${2:-8080}"
CONTAINER_NAME="${EXAMPLE_NAME,,}-test"
IMAGE_NAME="${EXAMPLE_NAME,,}:latest"

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}Testing $EXAMPLE_NAME${NC}"

# Build
echo -e "\n${YELLOW}Building Docker image...${NC}"
if ! docker build -f examples/Dockerfile --build-arg EXAMPLE_NAME="$EXAMPLE_NAME" -t "$IMAGE_NAME" . > /tmp/${EXAMPLE_NAME}_build.log 2>&1; then
    echo -e "${RED}✗ Build failed${NC}"
    tail -20 /tmp/${EXAMPLE_NAME}_build.log
    exit 1
fi
echo -e "${GREEN}✓ Build succeeded${NC}"

# Run
echo -e "\n${YELLOW}Starting container...${NC}"
docker run -d --name "$CONTAINER_NAME" -p $PORT:5000 \
  -e ProbotSharp__App__AppId=123456 \
  -e ProbotSharp__App__WebhookSecret=development \
  -e "ProbotSharp__App__PrivateKey=-----BEGIN RSA PRIVATE KEY-----
test
-----END RSA PRIVATE KEY-----" \
  "$IMAGE_NAME" > /dev/null

sleep 3

# Test health
echo -e "${YELLOW}Testing health endpoint...${NC}"
if ! curl -sf http://localhost:$PORT/health > /dev/null; then
    echo -e "${RED}✗ Health check failed${NC}"
    docker logs "$CONTAINER_NAME" | tail -20
    docker stop "$CONTAINER_NAME" > /dev/null 2>&1
    docker rm "$CONTAINER_NAME" > /dev/null 2>&1
    exit 1
fi
echo -e "${GREEN}✓ Health check passed${NC}"

# Test webhook
echo -e "${YELLOW}Testing webhook endpoint...${NC}"
PAYLOAD=$(cat fixtures/test-webhook-payload.json)
SECRET="development"
SIGNATURE=$(echo -n "$PAYLOAD" | openssl dgst -sha256 -hmac "$SECRET" | awk '{print $2}')

HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST http://localhost:$PORT/webhooks \
  -H "Content-Type: application/json" \
  -H "X-GitHub-Event: issues" \
  -H "X-GitHub-Delivery: test-delivery-123" \
  -H "X-Hub-Signature-256: sha256=$SIGNATURE" \
  -d "$PAYLOAD")

if [ "$HTTP_CODE" = "202" ] || [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✓ Webhook accepted (HTTP $HTTP_CODE)${NC}"
else
    # Try template-style endpoint
    HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST http://localhost:$PORT/api/github/webhooks \
      -H "Content-Type: application/json" \
      -H "X-GitHub-Event: issues" \
      -H "X-GitHub-Delivery: test-delivery-123" \
      -H "X-Hub-Signature-256: sha256=$SIGNATURE" \
      -d "$PAYLOAD")
    
    if [ "$HTTP_CODE" = "202" ] || [ "$HTTP_CODE" = "200" ]; then
        echo -e "${GREEN}✓ Webhook accepted at /api/github/webhooks (HTTP $HTTP_CODE)${NC}"
    else
        echo -e "${RED}✗ Webhook failed (HTTP $HTTP_CODE)${NC}"
        docker logs "$CONTAINER_NAME" | tail -10
        docker stop "$CONTAINER_NAME" > /dev/null 2>&1
        docker rm "$CONTAINER_NAME" > /dev/null 2>&1
        exit 1
    fi
fi

# Cleanup
echo -e "\n${YELLOW}Cleaning up...${NC}"
docker stop "$CONTAINER_NAME" > /dev/null 2>&1
docker rm "$CONTAINER_NAME" > /dev/null 2>&1

echo -e "\n${GREEN}✓ All tests passed for $EXAMPLE_NAME${NC}"
