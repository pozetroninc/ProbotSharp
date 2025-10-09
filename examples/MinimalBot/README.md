# MinimalBot - Zero Infrastructure ProbotSharp Example

A minimal ProbotSharp bot that runs with **zero external dependencies** - no Docker, no PostgreSQL, no Redis. Perfect for learning, prototyping, and simple automation.

## What This Bot Does

MinimalBot is an auto-labeler that:
- Automatically labels new issues based on title keywords
- Responds to `/help` commands in issue comments
- Demonstrates ProbotSharp's minimal deployment mode

**Keywords:**
- "bug" → adds "bug" label
- "feature" or "enhancement" → adds "enhancement" label
- "question" → adds "question" label
- "docs" or "documentation" → adds "documentation" label

## Quick Start (5 Minutes)

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A GitHub account

### Step 1: Get GitHub App Credentials

1. Go to [GitHub Apps Settings](https://github.com/settings/apps/new)
2. Create a new GitHub App:
   - **Name**: MinimalBot-YourUsername (must be unique)
   - **Webhook URL**: `http://localhost:5000/webhooks` (for now)
   - **Webhook Secret**: `development`
   - **Permissions**:
     - Repository: Issues (Read & write)
     - Repository: Metadata (Read-only)
   - **Subscribe to events**:
     - Issues
     - Issue comment
3. After creation:
   - Note your **App ID**
   - Generate and download a **private key**
   - Save the private key as `private-key.pem` in this directory

### Step 2: Configure

Edit `appsettings.json` and replace `YOUR_GITHUB_APP_ID` with your actual App ID:

```json
{
  "ProbotSharp": {
    "App": {
      "AppId": "123456",  // Your App ID here
      "WebhookSecret": "development",
      "PrivateKeyPath": "private-key.pem"
    },
    "Adapters": {
      "Cache": {
        "Provider": "InMemory",
        "Options": { "ExpirationMinutes": "60" }
      },
      "Idempotency": {
        "Provider": "InMemory",
        "Options": { "ExpirationHours": "24" }
      },
      "Persistence": {
        "Provider": "InMemory",
        "Options": {}
      },
      "ReplayQueue": {
        "Provider": "InMemory",
        "Options": {
          "MaxRetryAttempts": "3",
          "RetryBaseDelayMs": "1000"
        }
      }
    }
  }
}
```

See [docs/AdapterConfiguration.md](../../docs/AdapterConfiguration.md) for complete configuration guide.

### Step 3: Run

```bash
dotnet run
```

That's it! Your bot is now running at `http://localhost:5000` with zero infrastructure dependencies.

### Step 4: Test Locally with smee.io

To receive webhooks locally:

```bash
# Install smee client (requires Node.js)
npm install -g smee-client

# Create a smee channel at https://smee.io
# Then run:
smee -u https://smee.io/YOUR_CHANNEL -t http://localhost:5000/webhooks
```

Update your GitHub App's webhook URL to your smee.io channel URL.

### Step 5: Install and Test

1. Install your GitHub App on a test repository
2. Create a new issue with "bug" in the title
3. Watch MinimalBot automatically add the "bug" label
4. Comment `/help` on the issue to see the help message

## Project Structure

```
MinimalBot/
├── MinimalBot.csproj          # Minimal dependencies - no database packages
├── Program.cs                 # Simplified startup - no Docker required
├── MinimalApp.cs              # Bot logic - auto-labeler implementation
├── appsettings.json           # Your configuration (edit App ID here)
├── private-key.pem            # Your GitHub App private key (gitignored)
└── README.md                  # This file
```

## Configuration

MinimalBot uses a single `appsettings.json` file configured for zero-infrastructure deployment. All adapters use in-memory implementations - perfect for learning, prototyping, and simple automation.

To scale up to production infrastructure (Redis, PostgreSQL), see the [Adapter Configuration Guide](../../docs/AdapterConfiguration.md).

## In-Memory Mode Details

MinimalBot uses these in-memory implementations:

| Component | Implementation | Details |
|-----------|----------------|---------|
| **Access Token Cache** | `InMemoryAccessTokenCacheAdapter` | ASP.NET Core MemoryCache, automatic expiration |
| **Idempotency** | `InMemoryIdempotencyAdapter` | Prevents duplicate webhook processing using MemoryCache |
| **Replay Queue** | `InMemoryWebhookReplayQueueAdapter` | ConcurrentQueue for retry logic |
| **Persistence** | InMemory | Webhooks not persisted (Provider: "InMemory") |

**Limitations:**
- No webhook history (not stored in database)
- Lost on restart (in-memory cache and queue)
- Single instance only (no distributed caching)
- ~100 webhooks/hour max throughput

**Perfect for:**
- Learning ProbotSharp
- Simple automation (auto-labelers, welcome bots)
- Prototypes and proof-of-concepts
- Low-traffic deployments

## Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/` | GET | Bot info and status |
| `/health` | GET | Health check (shows in-memory mode) |
| `/webhooks` | POST | GitHub webhook receiver |

Test endpoints:

```bash
# Bot info
curl http://localhost:5000/

# Health check
curl http://localhost:5000/health
```

## Deployment Options

### Local Development (Current)

```bash
dotnet run
```

### Docker (Single Container)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
ENV ProbotSharp__Adapters__Cache__Provider=InMemory
ENV ProbotSharp__Adapters__Persistence__Provider=InMemory
EXPOSE 8080
ENTRYPOINT ["dotnet", "MinimalBot.dll"]
```

```bash
docker build -t minimalbot .
docker run -p 8080:8080 \
  -e ProbotSharp__App__AppId=123456 \
  -e ProbotSharp__App__WebhookSecret=development \
  minimalbot
```

### Azure Web App (Free Tier)

```bash
# Create resource group
az group create --name minimalbot-rg --location eastus

# Create App Service plan (Free tier)
az appservice plan create \
  --name minimalbot-plan \
  --resource-group minimalbot-rg \
  --sku F1 \
  --is-linux

# Create web app
az webapp create \
  --resource-group minimalbot-rg \
  --plan minimalbot-plan \
  --name minimalbot-yourname \
  --runtime "DOTNET|8.0"

# Configure app settings
az webapp config appsettings set \
  --resource-group minimalbot-rg \
  --name minimalbot-yourname \
  --settings \
    ProbotSharp__App__AppId=123456 \
    ProbotSharp__Adapters__Cache__Provider=InMemory \
    ProbotSharp__Adapters__Persistence__Provider=InMemory
```

Total cost: **$0/month** (Free tier)

### Railway / Render

Both platforms support minimal deployments with their free tiers. Just push your code and set environment variables in the dashboard.

## Scaling Up

When you need more than in-memory mode:

### Step 1: Add File-Based Queue

```json
{
  "ProbotSharp": {
    "Adapters": {
      "ReplayQueue": {
        "Provider": "FileSystem",
        "Options": {
          "Path": "./replay-queue",
          "MaxRetryAttempts": "5"
        }
      }
    }
  }
}
```

### Step 2: Add SQLite Database

```json
{
  "ProbotSharp": {
    "Adapters": {
      "Persistence": {
        "Provider": "Sqlite",
        "Options": {
          "ConnectionString": "Data Source=minimalbot.db"
        }
      }
    }
  }
}
```

### Step 3: Upgrade to PostgreSQL + Redis

See [Adapter Configuration Guide](../../docs/AdapterConfiguration.md) for complete configuration options and detailed scaling paths.

## Troubleshooting

### Bot doesn't receive webhooks

1. Check smee.io is running and forwarding to `http://localhost:5000/webhooks`
2. Verify GitHub App webhook URL matches your smee.io channel
3. Check logs for webhook signature validation errors

### "Invalid configuration" error

1. Verify `AppId` is set in `appsettings.json`
2. Ensure `private-key.pem` exists in this directory
3. Check private key format (should start with `-----BEGIN RSA PRIVATE KEY-----`)

### Labels not being added

1. Verify GitHub App has "Issues: Read & write" permission
2. Check the bot is installed on the repository
3. Review logs for API errors

### App crashes on startup

1. Check .NET 8.0 SDK is installed: `dotnet --version`
2. Restore dependencies: `dotnet restore`
3. Review logs for missing configuration

## Learn More

- **ProbotSharp Documentation**: [../../docs/](../../docs/)
- **Minimal Deployment Guide**: [../../docs/MinimalDeployment.md](../../docs/MinimalDeployment.md)
- **Local Development Guide**: [../../docs/LocalDevelopment.md](../../docs/LocalDevelopment.md)
- **Best Practices**: [../../docs/BestPractices.md](../../docs/BestPractices.md)

## License

MIT - see [LICENSE](../../LICENSE)
