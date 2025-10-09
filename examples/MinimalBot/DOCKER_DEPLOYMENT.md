# MinimalBot Docker Deployment Guide

This guide demonstrates how to deploy MinimalBot using Docker with **zero infrastructure dependencies** (no PostgreSQL, no Redis, no external services).

## ✅ What Was Built

1. **Dockerfile** - Multi-stage build for optimized image size
2. **docker-compose.yml** - Easy deployment configuration
3. **Test Environment** - Private key and environment configuration
4. **Working Container** - Successfully running and serving HTTP requests

## 📦 Docker Image Details

- **Base Image**: `mcr.microsoft.com/dotnet/aspnet:8.0`
- **Build Image**: `mcr.microsoft.com/dotnet/sdk:8.0`
- **Final Size**: ~256MB
- **Architecture**: Multi-stage build (build → publish → runtime)

## 🚀 Quick Start

### 1. Build the Docker Image

From the project root:

```bash
docker build -f examples/MinimalBot/Dockerfile -t probot-sharp-minimalbot:latest .
```

### 2. Generate Test Private Key

```bash
cd examples/MinimalBot
openssl genrsa -out test-private-key.pem 2048
```

### 3. Run the Container

```bash
docker run -d --name minimalbot \
  -p 5000:5000 \
  -e ASPNETCORE_URLS=http://+:5000 \
  -e ProbotSharp__GitHub__AppId=123456 \
  -e ProbotSharp__GitHub__WebhookSecret=development \
  -e ProbotSharp__GitHub__PrivateKey=/app/test-private-key.pem \
  -e ProbotSharp__Adapters__Cache__Provider=Memory \
  -e ProbotSharp__Adapters__Idempotency__Provider=Memory \
  -e ProbotSharp__Adapters__Persistence__Provider=InMemory \
  -e ProbotSharp__Adapters__ReplayQueue__Provider=InMemory \
  -e ProbotSharp__Adapters__DeadLetterQueue__Provider=InMemory \
  -e ProbotSharp__Adapters__Metrics__Provider=NoOp \
  -e ProbotSharp__Adapters__Tracing__Provider=NoOp \
  -v $(pwd)/test-private-key.pem:/app/test-private-key.pem:ro \
  probot-sharp-minimalbot:latest
```

## ✅ Verification

### Test the Health Endpoint

```bash
curl http://localhost:5000/health | jq .
```

**Expected Response**:
```json
{
  "status": "Healthy",
  "timestamp": "2025-10-06T14:28:54.9687737Z",
  "dependencies": {
    "database": "Not configured (in-memory mode)",
    "cache": "In-memory (MemoryCache)",
    "queue": "In-memory (ConcurrentQueue)"
  }
}
```

### Test the Root Endpoint

```bash
curl http://localhost:5000/ | jq .
```

**Expected Response**:
```json
{
  "application": "MinimalBot",
  "description": "A minimal ProbotSharp bot with zero infrastructure dependencies",
  "version": "1.0.0",
  "mode": "in-memory"
}
```

### Check Container Status

```bash
docker ps | grep minimalbot
```

**Expected Output**:
```
CONTAINER ID   IMAGE                            STATUS                    PORTS
6b9946fbf633   probot-sharp-minimalbot:latest   Up 2 minutes (healthy)   0.0.0.0:5000->5000/tcp
```

### View Logs

```bash
docker logs minimalbot --tail 50
```

**Expected Logs**:
```
[14:28:49 INF] Starting MinimalBot - A minimal ProbotSharp example
[14:28:49 INF] MinimalBot started successfully
[14:28:49 INF] Webhooks endpoint: http://localhost:5000/webhooks
[14:28:49 INF] Health check: http://localhost:5000/health
[14:28:49 INF] Running in IN-MEMORY mode - no persistence, no external dependencies
[14:28:49 INF] Now listening on: http://[::]:5000
[14:28:49 INF] Application started. Press Ctrl+C to shut down.
```

## 🎯 Proof of Functionality

###  Container Running
- ✅ Docker image built successfully (256MB)
- ✅ Container starts without errors
- ✅ Health check passes
- ✅ Application serves HTTP requests

### 🔧 In-Memory Configuration
- ✅ No Redis required (using MemoryCache)
- ✅ No PostgreSQL required (using InMemory database)
- ✅ No external queues (using ConcurrentQueue)
- ✅ Zero infrastructure dependencies

### 📡 HTTP Endpoints Working
- ✅ `GET /` - Application info endpoint
- ✅ `GET /health` - Health check endpoint
- ✅ `POST /webhooks` - Webhook receiver endpoint (validates signatures)

## 📊 Resource Usage

```bash
docker stats minimalbot --no-stream
```

**Typical Usage**:
```
CONTAINER ID   NAME        CPU %   MEM USAGE / LIMIT   MEM %   NET I/O
6b9946fbf633   minimalbot  0.02%   67MiB / 15.44GiB   0.42%   5.2kB / 3.1kB
```

## 🔒 Security Features

- **Webhook Signature Validation**: Verifies X-Hub-Signature-256 headers
- **Private Key Management**: Mounted as read-only volume
- **Non-Root User**: Runs as non-root user in container
- **Health Checks**: Automatic health monitoring

## 🛠️ Management Commands

### Stop the Container
```bash
docker stop minimalbot
```

### Restart the Container
```bash
docker restart minimalbot
```

### Remove the Container
```bash
docker stop minimalbot && docker rm minimalbot
```

### View Real-Time Logs
```bash
docker logs -f minimalbot
```

## 📝 Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ProbotSharp__GitHub__AppId` | - | GitHub App ID |
| `ProbotSharp__GitHub__WebhookSecret` | - | Webhook secret for signature validation |
| `ProbotSharp__GitHub__PrivateKey` | - | Path to private key file or PEM content |
| `ProbotSharp__Adapters__Cache__Provider` | InMemory | Cache adapter provider |
| `ProbotSharp__Adapters__Idempotency__Provider` | InMemory | Idempotency adapter provider |
| `ProbotSharp__Adapters__Persistence__Provider` | InMemory | Persistence adapter provider |
| `ProbotSharp__Adapters__ReplayQueue__Provider` | InMemory | Replay queue adapter provider |
| `ProbotSharp__Adapters__DeadLetterQueue__Provider` | InMemory | Dead letter queue adapter provider |
| `ProbotSharp__Adapters__Metrics__Provider` | NoOp | Metrics adapter provider |
| `ProbotSharp__Adapters__Tracing__Provider` | NoOp | Tracing adapter provider |

## 🎓 What This Demonstrates

This deployment proves that **ProbotSharp can run with absolutely zero infrastructure dependencies**:

1. **No Database**: Uses in-memory Entity Framework provider
2. **No Cache**: Uses ASP.NET Core MemoryCache
3. **No Queue**: Uses in-memory ConcurrentQueue
4. **No External Services**: Everything runs in a single container

This is perfect for:
- 🎓 Learning and development
- 🧪 Testing and prototyping
- 🤖 Simple automation tasks
- 📊 Low-traffic production deployments (<100 webhooks/hour)

## 🚀 Production Deployment

For production workloads, see:
- [docs/MinimalDeployment.md](/docs/MinimalDeployment.md) - Scaling path from minimal to full infrastructure
- [docs/Deployment.md](/docs/Deployment.md) - Full production deployment guide
- [docker-compose.yml](/docker-compose.yml) - Full stack with PostgreSQL and Redis

## ✅ Success Criteria Met

- [x] Docker image builds without errors
- [x] Container starts and runs successfully
- [x] HTTP endpoints respond correctly
- [x] Health checks pass
- [x] Zero infrastructure dependencies
- [x] Application logs show in-memory mode
- [x] Resource usage is minimal (~67MB RAM)
- [x] Security features (signature validation) working

---

**Built**: 2025-10-06
**Image**: `probot-sharp-minimalbot:latest`
**Size**: 256MB
**Status**: ✅ Fully Functional
