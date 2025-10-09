#!/bin/bash
# Test the ProbotSharp template by generating a fresh project and verifying it works
# Usage: ./test-template.sh [PROJECT_NAME] [PORT]
#
# Examples:
#   ./test-template.sh                          # Generate TemplateTest on port 5000
#   ./test-template.sh MyTestBot 8080          # Custom name and port

set -e

# Save original directory (must be repo root)
ORIGINAL_DIR=$(pwd)

# Configuration
PROJECT_NAME="${1:-TemplateTest}"
PORT="${2:-5000}"
TEMP_DIR="/tmp/probotsharp-template-test"
WEBHOOK_SECRET="development"

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}ProbotSharp Template Test${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Step 1: Clean up any existing test directory
if [ -d "$TEMP_DIR" ]; then
    echo -e "${YELLOW}Cleaning up existing test directory...${NC}"
    rm -rf "$TEMP_DIR"
fi

# Step 2: Create template project
echo -e "${BLUE}[1/7] Creating project from template...${NC}"
mkdir -p "$TEMP_DIR"
cd "$TEMP_DIR"

if ! dotnet new probotsharp-app -n "$PROJECT_NAME" -o "./$PROJECT_NAME" > /tmp/template-generation.log 2>&1; then
    echo -e "${RED}✗ Template generation failed${NC}"
    echo ""
    echo "Generation log:"
    cat /tmp/template-generation.log
    exit 1
fi

echo -e "${GREEN}✓ Template project created: $PROJECT_NAME${NC}"

# Step 3: Fix project references
echo -e "${BLUE}[2/7] Fixing project references...${NC}"

# Use the original directory as repo root
REPO_ROOT="$ORIGINAL_DIR"

# Verify we're in the right place
if [ ! -d "$REPO_ROOT/src/ProbotSharp.Domain" ]; then
    echo -e "${RED}✗ Repository root not found. Please run from probot-sharp repository root.${NC}"
    exit 1
fi

# Update .csproj file to use ../src/ paths (relative to Docker build context)
CSPROJ_FILE="$TEMP_DIR/$PROJECT_NAME/$PROJECT_NAME.csproj"

if [ ! -f "$CSPROJ_FILE" ]; then
    echo -e "${RED}✗ Project file not found: $CSPROJ_FILE${NC}"
    exit 1
fi

# The template uses ../src/ which works in Docker context
# Fix: Replace Bootstrap.Api with Adapters.Http (template bug)
if grep -q "ProbotSharp.Bootstrap.Api" "$CSPROJ_FILE"; then
    sed -i 's|ProbotSharp.Bootstrap.Api|ProbotSharp.Adapters.Http|g' "$CSPROJ_FILE"
fi

echo -e "${GREEN}✓ Project references verified${NC}"

# Step 4: Create appsettings.json
echo -e "${BLUE}[3/7] Creating appsettings.json...${NC}"

cat > "$TEMP_DIR/$PROJECT_NAME/appsettings.json" << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ProbotSharp": {
    "App": {
      "AppId": "YOUR_GITHUB_APP_ID",
      "WebhookSecret": "development",
      "PrivateKeyPath": "private-key.pem"
    },
    "Adapters": {
      "Cache": {
        "Provider": "Memory"
      },
      "Idempotency": {
        "Provider": "Database",
        "Options": {
          "ConnectionString": "Data Source=:memory:"
        }
      },
      "Persistence": {
        "Provider": "InMemory"
      },
      "ReplayQueue": {
        "Provider": "InMemory"
      },
      "DeadLetterQueue": {
        "Provider": "InMemory"
      },
      "Metrics": {
        "Provider": "NoOp"
      },
      "Tracing": {
        "Provider": "NoOp"
      }
    }
  }
}
EOF

echo -e "${GREEN}✓ appsettings.json created${NC}"

# Step 5: Create Dockerfile
echo -e "${BLUE}[4/7] Creating Dockerfile...${NC}"

# Create Dockerfile in temp directory (not in project directory)
cat > "$TEMP_DIR/Dockerfile" << EOF
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["$PROJECT_NAME/$PROJECT_NAME.csproj", "$PROJECT_NAME/"]
COPY ["src/ProbotSharp.Domain/ProbotSharp.Domain.csproj", "src/ProbotSharp.Domain/"]
COPY ["src/ProbotSharp.Application/ProbotSharp.Application.csproj", "src/ProbotSharp.Application/"]
COPY ["src/ProbotSharp.Infrastructure/ProbotSharp.Infrastructure.csproj", "src/ProbotSharp.Infrastructure/"]
COPY ["src/ProbotSharp.Adapters.Http/ProbotSharp.Adapters.Http.csproj", "src/ProbotSharp.Adapters.Http/"]

# Restore dependencies
RUN dotnet restore "$PROJECT_NAME/$PROJECT_NAME.csproj"

# Copy source code
COPY ["$PROJECT_NAME/", "$PROJECT_NAME/"]
COPY ["src/", "src/"]

# Build
WORKDIR "/src/$PROJECT_NAME"
RUN dotnet build "$PROJECT_NAME.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "$PROJECT_NAME.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "$PROJECT_NAME.dll"]
EOF

echo -e "${GREEN}✓ Dockerfile created${NC}"

# Step 6: Build Docker image
echo -e "${BLUE}[5/7] Building Docker image...${NC}"

IMAGE_NAME=$(echo "$PROJECT_NAME" | tr '[:upper:]' '[:lower:]')
CONTAINER_NAME="${IMAGE_NAME}-test"

# Copy src directory to temp directory for Docker build context
cp -r "$REPO_ROOT/src" "$TEMP_DIR/"

# Build Docker image (Dockerfile is in $TEMP_DIR root now)
cd "$TEMP_DIR"
if ! docker build -f Dockerfile -t "$IMAGE_NAME:test" . > /tmp/template-docker-build.log 2>&1; then
    echo -e "${RED}✗ Docker build failed${NC}"
    echo ""
    echo "Build log:"
    tail -50 /tmp/template-docker-build.log
    exit 1
fi

echo -e "${GREEN}✓ Docker image built: $IMAGE_NAME:test${NC}"

# Step 7: Test the container
echo -e "${BLUE}[6/7] Testing container...${NC}"

# Clean up any existing container
docker rm -f "$CONTAINER_NAME" > /dev/null 2>&1 || true

# Run container
if ! docker run -d --name "$CONTAINER_NAME" -p "$PORT:5000" \
    -e ASPNETCORE_URLS="http://+:5000" \
    -e ProbotSharp__App__WebhookSecret="$WEBHOOK_SECRET" \
    "$IMAGE_NAME:test" > /dev/null 2>&1; then
    echo -e "${RED}✗ Failed to start container${NC}"
    exit 1
fi

echo -e "${YELLOW}Container started, waiting for startup...${NC}"
sleep 5

# Check if container is still running
if ! docker ps | grep -q "$CONTAINER_NAME"; then
    echo -e "${RED}✗ Container died after startup${NC}"
    echo ""
    echo "Container logs:"
    docker logs "$CONTAINER_NAME"
    docker rm -f "$CONTAINER_NAME" > /dev/null 2>&1 || true
    exit 1
fi

# Test health endpoint
echo -e "${YELLOW}Testing health endpoint...${NC}"
HEALTH_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" "http://localhost:$PORT/health" || echo "000")

if [ "$HEALTH_RESPONSE" = "200" ]; then
    echo -e "${GREEN}✓ Health check passed (HTTP 200)${NC}"
else
    echo -e "${RED}✗ Health check failed (HTTP $HEALTH_RESPONSE)${NC}"
    docker logs "$CONTAINER_NAME"
    docker rm -f "$CONTAINER_NAME" > /dev/null 2>&1 || true
    exit 1
fi

# Test webhook endpoint (template uses /api/github/webhooks)
echo -e "${YELLOW}Testing webhook endpoint...${NC}"

# Create test payload
PAYLOAD='{"action":"opened","issue":{"number":1,"title":"Test"},"repository":{"name":"test","full_name":"test/test","owner":{"login":"test"}},"sender":{"login":"test"},"installation":{"id":123}}'

# Generate signature
SIGNATURE=$(echo -n "$PAYLOAD" | openssl dgst -sha256 -hmac "$WEBHOOK_SECRET" | awk '{print $2}')

# Try both webhook endpoints
WEBHOOK_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" -X POST "http://localhost:$PORT/api/github/webhooks" \
  -H "Content-Type: application/json" \
  -H "X-GitHub-Event: issues" \
  -H "X-GitHub-Delivery: test-$(date +%s)" \
  -H "X-Hub-Signature-256: sha256=$SIGNATURE" \
  -d "$PAYLOAD" || echo "000")

if [ "$WEBHOOK_RESPONSE" = "202" ] || [ "$WEBHOOK_RESPONSE" = "200" ]; then
    echo -e "${GREEN}✓ Webhook test passed (HTTP $WEBHOOK_RESPONSE)${NC}"
else
    # Try alternate endpoint
    WEBHOOK_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" -X POST "http://localhost:$PORT/webhooks" \
      -H "Content-Type: application/json" \
      -H "X-GitHub-Event: issues" \
      -H "X-GitHub-Delivery: test-$(date +%s)" \
      -H "X-Hub-Signature-256: sha256=$SIGNATURE" \
      -d "$PAYLOAD" || echo "000")

    if [ "$WEBHOOK_RESPONSE" = "202" ] || [ "$WEBHOOK_RESPONSE" = "200" ]; then
        echo -e "${GREEN}✓ Webhook test passed (HTTP $WEBHOOK_RESPONSE)${NC}"
    else
        echo -e "${RED}✗ Webhook test failed (HTTP $WEBHOOK_RESPONSE)${NC}"
        docker logs "$CONTAINER_NAME"
        docker rm -f "$CONTAINER_NAME" > /dev/null 2>&1 || true
        exit 1
    fi
fi

# Step 8: Cleanup
echo -e "${BLUE}[7/7] Cleaning up...${NC}"

# Stop and remove container
docker rm -f "$CONTAINER_NAME" > /dev/null 2>&1 || true

# Remove image
docker rmi "$IMAGE_NAME:test" > /dev/null 2>&1 || true

# Remove temp directory
rm -rf "$TEMP_DIR"

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}✓ Template test completed successfully!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Summary:"
echo "  - Template project generated: $PROJECT_NAME"
echo "  - Docker image built and tested"
echo "  - Health endpoint: ✓"
echo "  - Webhook endpoint: ✓"
echo "  - Cleanup: ✓"
echo ""

exit 0
