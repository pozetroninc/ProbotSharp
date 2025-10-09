#!/bin/bash
# Send a test GitHub webhook to a ProbotSharp bot
# Usage: ./send-test-webhook.sh [URL] [PAYLOAD_FILE]
#
# Examples:
#   ./send-test-webhook.sh                                    # Defaults to http://localhost:5000/webhooks
#   ./send-test-webhook.sh http://localhost:8080/webhooks    # Custom URL
#   ./send-test-webhook.sh "" custom-payload.json            # Custom payload

set -e

# Configuration
WEBHOOK_URL="${1:-http://localhost:5000/webhooks}"
PAYLOAD_FILE="${2:-fixtures/test-webhook-payload.json}"
WEBHOOK_SECRET="${WEBHOOK_SECRET:-development}"

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if payload file exists
if [ ! -f "$PAYLOAD_FILE" ]; then
    echo -e "${RED}Error: Payload file not found: $PAYLOAD_FILE${NC}"
    exit 1
fi

# Read payload
PAYLOAD=$(cat "$PAYLOAD_FILE")

# Generate HMAC-SHA256 signature
SIGNATURE=$(echo -n "$PAYLOAD" | openssl dgst -sha256 -hmac "$WEBHOOK_SECRET" | awk '{print $2}')

echo -e "${YELLOW}Sending webhook to: ${WEBHOOK_URL}${NC}"
echo -e "${YELLOW}Payload file: ${PAYLOAD_FILE}${NC}"
echo -e "${YELLOW}Secret: ${WEBHOOK_SECRET}${NC}"
echo ""

# Send webhook
HTTP_CODE=$(curl -s -o /tmp/webhook-response.txt -w "%{http_code}" -X POST "$WEBHOOK_URL" \
  -H "Content-Type: application/json" \
  -H "X-GitHub-Event: issues" \
  -H "X-GitHub-Delivery: test-delivery-$(date +%s)" \
  -H "X-Hub-Signature-256: sha256=$SIGNATURE" \
  -d "$PAYLOAD")

# Check response
if [ "$HTTP_CODE" = "202" ] || [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✓ Webhook accepted (HTTP $HTTP_CODE)${NC}"
    exit 0
else
    echo -e "${RED}✗ Webhook failed (HTTP $HTTP_CODE)${NC}"
    echo ""
    echo "Response:"
    cat /tmp/webhook-response.txt
    echo ""
    exit 1
fi
