# Local Development Guide

This guide will help you set up ProbotSharp for local development.

## Quick Start (No Docker Required)

The fastest way to get started - no databases, no Docker, just run:

### Step 1: Install Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A GitHub account
- Your preferred IDE (Visual Studio 2022, VS Code, or Rider)

### Step 2: Create Your Bot

```bash
# Clone the repository
git clone https://github.com/yourusername/probotsharp.git
cd probotsharp

# Create a new bot from template
dotnet new install ./templates
dotnet new probotsharp-app -n MyBot -o mybot
cd mybot
```

### Step 3: Configure (Minimal)

Create `appsettings.json` with in-memory configuration:

```json
{
  "ProbotSharp": {
    "GitHub": {
      "AppId": "YOUR_APP_ID",
      "WebhookSecret": "development",
      "PrivateKey": "private-key.pem"
    },
    "Adapters": {
      "Cache": { "Provider": "InMemory" },
      "Idempotency": { "Provider": "InMemory" },
      "Persistence": { "Provider": "InMemory" },
      "ReplayQueue": { "Provider": "InMemory" },
      "DeadLetterQueue": { "Provider": "InMemory" },
      "Metrics": { "Provider": "NoOp" },
      "Tracing": { "Provider": "NoOp" }
    }
  }
}
```

### Step 4: Get GitHub App Credentials

1. Go to [GitHub Apps Settings](https://github.com/settings/apps/new)
2. Create a new GitHub App:
   - **Name**: MyBot (or your choice)
   - **Webhook URL**: http://localhost:5000/webhooks (for now)
   - **Webhook Secret**: development
   - **Permissions**: Repository issues (Read & write)
   - **Subscribe to events**: Issues
3. After creation, note your **App ID**
4. Generate and download a **private key** (save as `private-key.pem` in your bot directory)

### Step 5: Run Your Bot

```bash
# Run without any infrastructure
dotnet run
```

Your bot is now running at http://localhost:5000 with zero external dependencies!

### Step 6: Test Locally

Use [smee.io](https://smee.io) to receive webhooks locally:

```bash
# Install smee client
npm install -g smee-client

# Start smee channel (create one at https://smee.io)
smee -u https://smee.io/YOUR_CHANNEL -t http://localhost:5000/webhooks
```

Update your GitHub App's webhook URL to your smee.io channel URL.

That's it! You're ready to build. See [Minimal Deployment Guide](MinimalDeployment.md) for more details on running without infrastructure.

---

## Advanced Setup (With Docker)

If you want to test with full infrastructure (PostgreSQL, Redis), use Docker Compose:

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) or [Docker Engine](https://docs.docker.com/engine/install/)
- [Docker Compose](https://docs.docker.com/compose/install/) (included with Docker Desktop)
- A GitHub account
- Your preferred IDE:
  - [Visual Studio 2022](https://visualstudio.microsoft.com/) (17.8 or later)
  - [Visual Studio Code](https://code.visualstudio.com/) with C# extension
  - [JetBrains Rider](https://www.jetbrains.com/rider/)

### Setup with Docker Compose

ProbotSharp offers two ways to set up your GitHub App:
1. **Web-Based Setup** (Recommended) - Interactive wizard at `/setup`
2. **Manual Setup** - Configure using GitHub's web interface

#### Option 1: Web-Based Setup (Recommended)

The easiest way to get started is using the built-in web-based setup wizard:

1. Clone and start the application:

```bash
git clone https://github.com/yourusername/probotsharp.git
cd probotsharp

# Start dependencies
docker-compose up -d postgres redis

# Run the application
cd src/ProbotSharp.Bootstrap.Api
dotnet run
```

2. Open your browser to http://localhost:3000/setup (or your configured port)

3. Follow the interactive wizard:
   - **Configure**: Enter your app name, description, and optional settings
   - **Generate Manifest**: Review the generated GitHub App manifest
   - **Register on GitHub**: Click to register your app on GitHub
   - **Complete**: Your app credentials are automatically saved

4. Install your app on a test repository and start building!

**Note**: If you're running in production or don't want the automatic redirect to `/setup`, set the environment variable:
```bash
PROBOT_SKIP_SETUP=true
```

### Option 2: Manual Setup

If you prefer to configure your GitHub App manually:

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/probotsharp.git
cd probotsharp
```

### 2. Create a GitHub App

1. Go to [GitHub App settings](https://github.com/settings/apps) (or organization settings for org apps)
2. Click **New GitHub App**
3. Fill in the required fields:
   - **GitHub App name**: Choose a unique name (e.g., `my-probotsharp-dev`)
   - **Homepage URL**: `http://localhost:8080` (for local development)
   - **Webhook URL**: You'll configure this in step 3
   - **Webhook secret**: Generate a random string or use `development` for local testing
4. Set the required permissions (depends on your app's needs):
   - Repository permissions: Issues (Read & Write), Pull Requests (Read & Write), etc.
   - Subscribe to events: Issues, Pull Request, Push, etc.
5. Click **Create GitHub App**
6. On the app page, note down:
   - **App ID** (at the top of the page)
   - **Webhook Secret** (if you set one)
7. Scroll down and click **Generate a private key**
   - Download the `.pem` file - you'll need this

### 3. Configure Webhook Proxy (Smee.io)

Since GitHub webhooks need a public URL but you're developing locally, use [Smee.io](https://smee.io/) as a webhook proxy:

1. Go to https://smee.io/
2. Click **Start a new channel**
3. Copy the webhook proxy URL (e.g., `https://smee.io/abc123`)
4. Go back to your GitHub App settings
5. Update the **Webhook URL** to your Smee.io URL
6. Save changes

### 4. Set Up Environment Variables

1. Copy the example environment file:

```bash
cp .env.example .env
```

2. Edit `.env` and fill in your GitHub App credentials:

```bash
GITHUB_APP_ID=123456
GITHUB_WEBHOOK_SECRET=development
GITHUB_PRIVATE_KEY="-----BEGIN RSA PRIVATE KEY-----
...your private key content...
-----END RSA PRIVATE KEY-----"
SMEE_URL=https://smee.io/abc123
```

**Note**: For `GITHUB_PRIVATE_KEY`, you can either:
- Paste the entire content from the `.pem` file (including the BEGIN/END lines)
- Or provide a file path in `appsettings.json` instead

### 5. Start Dependencies with Docker Compose

Start PostgreSQL and Redis:

```bash
docker-compose up -d postgres redis
```

Wait for services to be healthy:

```bash
docker-compose ps
```

You should see both `postgres` and `redis` with status `healthy`.

### 6. Run Database Migrations

Install the EF Core CLI tool (if not already installed):

```bash
dotnet tool install --global dotnet-ef
```

Run migrations to create the database schema:

```bash
dotnet ef database update --project src/ProbotSharp.Infrastructure --startup-project src/ProbotSharp.Bootstrap.Api
```

### 7. Run the Application

#### Option A: Using Docker Compose (Recommended)

Start all services including the application and Smee proxy:

```bash
docker-compose up
```

The application will be available at http://localhost:8080

#### Option B: Using .NET CLI

If you prefer to run the application outside Docker:

1. Stop the Docker app service if running:
```bash
docker-compose stop app smee
```

2. Run the application:
```bash
cd src/ProbotSharp.Bootstrap.Api
dotnet run
```

3. In a separate terminal, run the Smee client:
```bash
npm install -g smee-client
smee --url https://smee.io/abc123 --target http://localhost:8080/webhooks
```

#### Option C: Using Visual Studio

1. Open `ProbotSharp.sln` in Visual Studio
2. Set `ProbotSharp.Bootstrap.Api` as the startup project
3. Press F5 to run with debugging
4. Set up Smee client separately (see Option B, step 3)

#### Option D: Using Visual Studio Code

1. Open the repository folder in VS Code
2. Install recommended extensions (C# Dev Kit, C#)
3. Press F5 to run with debugging (uses `.vscode/launch.json`)
4. Set up Smee client separately (see Option B, step 3)

### 8. Test the Setup

1. Open your browser and navigate to http://localhost:8080
2. You should see: `{"application":"ProbotSharp","version":"0.0.0"}`
3. Check the health endpoint: http://localhost:8080/health
4. Install your GitHub App on a test repository:
   - Go to your GitHub App settings
   - Click **Install App**
   - Select a test repository
5. Create an issue or pull request in the test repository
6. Check your application logs to see the webhook being received

## Development Workflow

### Replaying Webhooks Locally

The `receive` command allows you to simulate webhook events without triggering them from GitHub. This is useful for rapid local development and debugging.

#### Basic Usage

Replay a webhook from a fixture file:

```bash
dotnet run --project src/ProbotSharp.Bootstrap.Cli -- receive ./your-app -e issues.opened -f fixtures/issues-opened.json
```

#### Command Options

- `-e, --event <name>` - Webhook event name (e.g., `issues.opened`, `pull_request`, `push`)
- `-f, --file <path>` - Path to JSON payload file (if not provided, reads from stdin)
- `--app-id <id>` - GitHub App ID (optional, for testing with real API)
- `--private-key <file>` - Path to private key PEM file (optional, for testing with real API)
- `--base-url <url>` - GitHub API base URL (optional, for GitHub Enterprise)
- `-t, --token <token>` - GitHub personal access token (optional, for testing)
- `--log-level <level>` - Log level (default: info)
- `--log-format <format>` - Log format: json or pretty (default: pretty)

#### Example Fixtures

The repository includes example webhook payloads in the `fixtures/` directory:

- `fixtures/issues-opened.json` - Issue opened event
- `fixtures/pull-request-opened.json` - Pull request opened event
- `fixtures/push.json` - Push event

#### Examples

Replay an issues event:
```bash
dotnet run --project src/ProbotSharp.Bootstrap.Cli -- receive ./your-app -e issues.opened -f fixtures/issues-opened.json
```

Replay a pull request event with debug logging:
```bash
dotnet run --project src/ProbotSharp.Bootstrap.Cli -- receive ./your-app -e pull_request.opened -f fixtures/pull-request-opened.json --log-level debug
```

Replay with inline payload from stdin:
```bash
cat fixtures/push.json | dotnet run --project src/ProbotSharp.Bootstrap.Cli -- receive ./your-app -e push
```

Use with actual GitHub credentials to test API calls:
```bash
dotnet run --project src/ProbotSharp.Bootstrap.Cli -- receive ./your-app -e issues.opened -f fixtures/issues-opened.json --app-id 123456 --private-key path/to/key.pem
```

#### Creating Custom Fixtures

To create your own webhook fixtures:

1. Trigger a real webhook from GitHub
2. Copy the payload from your application logs or GitHub's webhook delivery page
3. Save it as a JSON file in the `fixtures/` directory
4. Replay it using the `receive` command

Alternatively, refer to the [GitHub webhook event documentation](https://docs.github.com/en/webhooks/webhook-events-and-payloads) for payload schemas.

### Running Tests

Run all tests:

```bash
dotnet test
```

Run tests with coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Generate coverage report:

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

Open `coverage-report/index.html` in your browser.

### Building the Solution

```bash
dotnet build
```

Build in Release mode:

```bash
dotnet build --configuration Release
```

### Code Style and Linting

The project uses StyleCop analyzers and .editorconfig for code style enforcement. Warnings will appear during build if your code doesn't follow the conventions.

To fix common formatting issues:

```bash
dotnet format
```

### Database Migrations

Create a new migration:

```bash
dotnet ef migrations add MigrationName --project src/ProbotSharp.Infrastructure --startup-project src/ProbotSharp.Bootstrap.Api
```

Remove the last migration (if not applied):

```bash
dotnet ef migrations remove --project src/ProbotSharp.Infrastructure --startup-project src/ProbotSharp.Bootstrap.Api
```

### Logs and Data

When running with Docker Compose, logs and data are stored in local directories:

- `./logs/` - Application logs
- `./replay-queue/` - Webhook replay queue (if using file system queue)
- `./dead-letter-queue/` - Failed webhooks that exceeded retry attempts

### Accessing Services

- **Application**: http://localhost:8080
- **PostgreSQL**: `localhost:5432` (username: `probotsharp`, password: `dev-password`)
- **Redis**: `localhost:6379`

Connect to PostgreSQL:

```bash
docker-compose exec postgres psql -U probotsharp -d probotsharp
```

Connect to Redis:

```bash
docker-compose exec redis redis-cli
```

## IDE Configuration

### Visual Studio 2022

1. Open `ProbotSharp.sln`
2. Set environment variables in launch profile:
   - Right-click `ProbotSharp.Bootstrap.Api` → Properties
   - Debug → Open debug launch profiles UI
   - Add environment variables in the Environment Variables section
3. Enable nullable reference types warnings:
   - Tools → Options → Text Editor → C# → Advanced
   - Check "Enable nullable reference types"

### Visual Studio Code

Recommended extensions (should prompt on first open):

- **C# Dev Kit** - Microsoft's official C# extension
- **C#** - IntelliSense and debugging
- **EditorConfig** - Code style enforcement
- **.NET Core Test Explorer** - Run and debug tests

Launch configuration (`.vscode/launch.json`):

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/ProbotSharp.Bootstrap.Api/bin/Debug/net8.0/ProbotSharp.Bootstrap.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/ProbotSharp.Bootstrap.Api",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ProbotSharp__GitHub__AppId": "${env:GITHUB_APP_ID}",
        "ProbotSharp__GitHub__WebhookSecret": "${env:GITHUB_WEBHOOK_SECRET}",
        "ProbotSharp__GitHub__PrivateKey": "${env:GITHUB_PRIVATE_KEY}"
      }
    }
  ]
}
```

### JetBrains Rider

1. Open `ProbotSharp.sln`
2. Configure environment variables:
   - Run → Edit Configurations
   - Select `ProbotSharp.Bootstrap.Api`
   - Add environment variables in Environment Variables section
3. Enable solution-wide analysis:
   - File → Settings → Editor → Inspection Settings
   - Check "Enable solution-wide analysis"

## Troubleshooting

### Database Connection Issues

**Problem**: Can't connect to PostgreSQL

**Solutions**:
- Ensure Docker containers are running: `docker-compose ps`
- Check PostgreSQL logs: `docker-compose logs postgres`
- Verify connection string in `appsettings.json`
- Test connection: `docker-compose exec postgres pg_isready -U probotsharp`

### Webhook Not Received

**Problem**: GitHub sends webhook but application doesn't receive it

**Solutions**:
- Check Smee.io dashboard to see if webhook was received
- Verify Smee client is running and pointing to correct URL
- Check application logs for errors
- Ensure webhook URL in GitHub App settings matches your Smee.io URL
- Verify webhook secret matches in both GitHub App and application config

### Migration Errors

**Problem**: `dotnet ef database update` fails

**Solutions**:
- Ensure database is running: `docker-compose up -d postgres`
- Drop and recreate database: `docker-compose exec postgres psql -U probotsharp -c "DROP DATABASE probotsharp; CREATE DATABASE probotsharp;"`
- Delete `src/ProbotSharp.Infrastructure/Migrations/` and recreate: `dotnet ef migrations add Initial --project src/ProbotSharp.Infrastructure --startup-project src/ProbotSharp.Bootstrap.Api`

### Port Already in Use

**Problem**: Port 8080, 5432, or 6379 is already in use

**Solutions**:
- Change ports in `docker-compose.yml`
- Stop conflicting services: `docker ps` and `docker stop <container>`
- Update `appsettings.json` if you changed ports

### Private Key Format Issues

**Problem**: Can't parse GitHub App private key

**Solutions**:
- Ensure the private key includes the BEGIN/END lines
- Check for extra whitespace or line breaks
- Store the key in a file and reference it via configuration instead of environment variable
- On Windows, ensure line endings are LF not CRLF

## Next Steps

- Read the [Architecture documentation](Architecture.md) to understand the codebase structure
- Check the [Operations guide](Operations.md) for deployment and monitoring

## Getting Help

- Open an issue on [GitHub](https://github.com/yourusername/probotsharp/issues)
- Check [GitHub Docs - Creating GitHub Apps](https://docs.github.com/en/developers/apps/building-github-apps/creating-a-github-app)
- Review [Probot documentation](https://probot.github.io/docs/) for conceptual guidance
