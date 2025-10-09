#!/bin/bash
# Test all ProbotSharp examples with Docker
# Usage: ./test-all-examples.sh [--verbose] [--with-template]

set -e

VERBOSE=false
TEST_TEMPLATE=false

for arg in "$@"; do
    case $arg in
        --verbose)
            VERBOSE=true
            ;;
        --with-template)
            TEST_TEMPLATE=true
            ;;
    esac
done

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Track results
declare -a PASSED
declare -a FAILED

test_example() {
    local NAME=$1
    echo -e "\n${YELLOW}=== Testing $NAME ===${NC}"
    
    # Build
    if [ "$VERBOSE" = true ]; then
        echo "Building..."
    fi
    if ! docker build -f examples/Dockerfile --build-arg EXAMPLE_NAME=$NAME -t ${NAME,,}:latest . > /tmp/${NAME}_build.log 2>&1; then
        echo -e "${RED}✗ Build failed${NC}"
        if [ "$VERBOSE" = true ]; then
            tail -20 /tmp/${NAME}_build.log
        fi
        FAILED+=("$NAME (build)")
        return 1
    fi
    if [ "$VERBOSE" = true ]; then
        echo -e "${GREEN}✓ Build succeeded${NC}"
    fi
    
    # Run
    if [ "$VERBOSE" = true ]; then
        echo "Starting container..."
    fi
    CONTAINER_ID=$(docker run -d --name ${NAME,,}-test -p 8080:5000 \
      -e ProbotSharp__App__AppId=123456 \
      -e ProbotSharp__App__WebhookSecret=development \
      -e "ProbotSharp__App__PrivateKey=-----BEGIN RSA PRIVATE KEY-----
test
-----END RSA PRIVATE KEY-----" \
      ${NAME,,}:latest)
    
    sleep 4
    
    # Test health
    if [ "$VERBOSE" = true ]; then
        echo "Testing health endpoint..."
    fi
    if ! curl -sf http://localhost:8080/health > /dev/null; then
        echo -e "${RED}✗ Health check failed${NC}"
        if [ "$VERBOSE" = true ]; then
            docker logs ${NAME,,}-test | tail -20
        fi
        docker stop ${NAME,,}-test > /dev/null 2>&1
        docker rm ${NAME,,}-test > /dev/null 2>&1
        FAILED+=("$NAME (health)")
        return 1
    fi
    if [ "$VERBOSE" = true ]; then
        echo -e "${GREEN}✓ Health check passed${NC}"
    fi
    
    # Test webhook
    if [ "$VERBOSE" = true ]; then
        echo "Testing webhook endpoint..."
    fi
    PAYLOAD=$(cat fixtures/test-webhook-payload.json)
    SECRET="development"
    SIGNATURE=$(echo -n "$PAYLOAD" | openssl dgst -sha256 -hmac "$SECRET" | awk '{print $2}')
    
    HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST http://localhost:8080/webhooks \
      -H "Content-Type: application/json" \
      -H "X-GitHub-Event: issues" \
      -H "X-GitHub-Delivery: test-delivery-123" \
      -H "X-Hub-Signature-256: sha256=$SIGNATURE" \
      -d "$PAYLOAD")
    
    if [ "$HTTP_CODE" = "202" ] || [ "$HTTP_CODE" = "200" ]; then
        echo -e "${GREEN}✓ $NAME passed${NC}"
        PASSED+=("$NAME")
    else
        # Try template-style endpoint
        HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST http://localhost:8080/api/github/webhooks \
          -H "Content-Type: application/json" \
          -H "X-GitHub-Event: issues" \
          -H "X-GitHub-Delivery: test-delivery-123" \
          -H "X-Hub-Signature-256: sha256=$SIGNATURE" \
          -d "$PAYLOAD")
        
        if [ "$HTTP_CODE" = "202" ] || [ "$HTTP_CODE" = "200" ]; then
            echo -e "${GREEN}✓ $NAME passed${NC}"
            PASSED+=("$NAME")
        else
            echo -e "${RED}✗ Webhook failed (HTTP $HTTP_CODE)${NC}"
            if [ "$VERBOSE" = true ]; then
                docker logs ${NAME,,}-test | tail -10
            fi
            FAILED+=("$NAME (webhook)")
        fi
    fi
    
    # Cleanup
    docker stop ${NAME,,}-test > /dev/null 2>&1
    docker rm ${NAME,,}-test > /dev/null 2>&1
}

# Test all examples
EXAMPLES=(
    "MinimalBot"
    "HelloWorldBot"
    "WildcardBot"
    "AttachmentsBot"
    "MetadataBot"
    "SlashCommandsBot"
    "GraphQLBot"
    "PaginationBot"
    "HttpExtensibilityBot"
    "DryRunBot"
    "ConfigBot"
    "ExtensionsBot"
)

TOTAL_TESTS=${#EXAMPLES[@]}
if [ "$TEST_TEMPLATE" = true ]; then
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
fi

echo -e "${YELLOW}Testing $TOTAL_TESTS ProbotSharp examples${NC}"
if [ "$TEST_TEMPLATE" = true ]; then
    echo -e "${YELLOW}(includes template test)${NC}"
fi

for example in "${EXAMPLES[@]}"; do
    test_example "$example"
done

# Test template if requested
if [ "$TEST_TEMPLATE" = true ]; then
    echo -e "\n${YELLOW}=== Testing Template ===${NC}"
    SCRIPT_DIR=$(cd "$(dirname "$0")" && pwd)

    if "$SCRIPT_DIR/test-template.sh"; then
        echo -e "${GREEN}✓ Template passed${NC}"
        PASSED+=("Template")
    else
        echo -e "${RED}✗ Template failed${NC}"
        FAILED+=("Template")
    fi
fi

# Summary
echo -e "\n${YELLOW}=== Test Summary ===${NC}"
echo -e "${GREEN}Passed (${#PASSED[@]}/$TOTAL_TESTS):${NC}"
for p in "${PASSED[@]}"; do
    echo "  ✓ $p"
done

if [ ${#FAILED[@]} -gt 0 ]; then
    echo -e "\n${RED}Failed (${#FAILED[@]}):${NC}"
    for f in "${FAILED[@]}"; do
        echo "  ✗ $f"
    done
    exit 1
else
    echo -e "\n${GREEN}All tests passed!${NC}"
    exit 0
fi
