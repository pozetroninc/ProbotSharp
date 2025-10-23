# MyBot

BOT_DESCRIPTION

A GitHub App built with [Probot Sharp](https://github.com/your-repo/probot-sharp), a C# port of [Probot](https://probot.github.io/).

## Quick Start

### Prerequisites

- .NET 8.0 SDK or later
- A GitHub App (create one at https://github.com/settings/apps/new)
- GitHub App credentials (App ID, Private Key, Webhook Secret)

### Setup

1. **Configure your bot**

   Copy the environment template and fill in your GitHub App credentials:

   ```bash
   cp .env.example .env
   ```

   Edit `.env` and set:
   - `PROBOTSHARP_GITHUB_APPID` - Your GitHub App ID
   - `PROBOTSHARP_WEBHOOK_SECRET` - Your webhook secret
   - `PROBOTSHARP_GITHUB_PRIVATEKEY` - Path to your private key file or base64-encoded key

2. **Run the bot**

   ```bash
   dotnet run
   ```

   The bot will start and listen for webhooks at `http://localhost:5000/api/github/webhooks`

3. **Test locally with webhook replay**

   You can test your bot locally by replaying webhook fixtures:

   ```bash
   dotnet run --project ../../src/ProbotSharp.Bootstrap.Cli -- \
     receive . \
     -e issues.opened \
     -f ../../fixtures/issues-opened.json \
     --app-id YOUR_APP_ID \
     --private-key path/to/your-key.pem
   ```

## Project Structure

```
MyBot/
├── MyBot.csproj              # Project file with dependencies
├── Program.cs                # Application entry point
├── MyBotApp.cs              # Main app class implementing IProbotApp
├── Handlers/
│   └── ExampleHandler.cs    # Event handler for issues.opened
├── .env.example             # Environment variable template
└── README.md                # This file
```

## How It Works

### The App Class (`MyBotApp.cs`)

The `MyBotApp` class implements `IProbotApp` and defines:

- **`ConfigureAsync`**: Registers your event handlers and services in the dependency injection container
- **`InitializeAsync`**: Registers handlers with the event router after DI is built

### Event Handlers (`Handlers/`)

Event handlers:
- Implement `IEventHandler`
- Are decorated with `[EventHandler("event", "action")]` to subscribe to specific events
- Receive a `ProbotSharpContext` with the webhook payload and authenticated GitHub API client

### The Context

The `ProbotSharpContext` provides:

- `context.Payload` - The webhook payload as JSON
- `context.GitHub` - Authenticated Octokit client for GitHub API calls
- `context.Logger` - Scoped logger with event context
- `context.Repository` - Repository information (owner/name)
- `context.Installation` - GitHub App installation details
- `context.IsBot()` - Check if the sender is a bot
- `context.GetRepositoryFullName()` - Get "owner/repo" string

## Adding New Event Handlers

1. **Create a handler class** in the `Handlers/` directory:

   ```csharp
   using System.Threading;
   using System.Threading.Tasks;
   using ProbotSharp.Application.Abstractions.Events;
   using ProbotSharp.Domain.Context;

   namespace MyBot.Handlers;

   [EventHandler("pull_request", "opened")]
   public class PullRequestHandler : IEventHandler
   {
       public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
       {
           // Extract data from payload
           var prNumber = context.Payload["pull_request"]?["number"]?.ToObject<int>();

           if (!prNumber.HasValue || context.Repository == null)
               return;

           // Use GitHub API
           await context.GitHub.Issue.Comment.Create(
               context.Repository.Owner,
               context.Repository.Name,
               prNumber.Value,
               "Thanks for the PR! 🎉");
       }
   }
   ```

2. **Register the handler** in `MyBotApp.cs`:

   ```text
   public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
   {
       services.AddScoped<ExampleHandler>();
       services.AddScoped<PullRequestHandler>(); // Add this line
       ...
   }

   public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
   {
       router.RegisterHandler("issues", "opened", typeof(ExampleHandler));
       router.RegisterHandler("pull_request", "opened", typeof(PullRequestHandler)); // Add this line
       ...
   }
   ```

## Wildcard Event Handlers

You can create handlers that respond to multiple events:

```csharp
// Handle all issue events (opened, closed, edited, etc.)
[EventHandler("issues", "*")]
public class AllIssuesHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        var action = context.Payload["action"]?.ToString();
        context.Logger.LogInformation("Issue event: {Action}", action);
        await Task.CompletedTask;
    }
}

// Handle ALL webhook events
[EventHandler("*", null)]
public class AllEventsHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        context.Logger.LogInformation("Event received: {EventName}", context.EventName);
        await Task.CompletedTask;
    }
}
```

## Configuration

All configuration is done through environment variables (or `.env` file). See `.env.example` for all available options.

### Required Configuration

- `PROBOTSHARP_GITHUB_APPID` - Your GitHub App ID
- `PROBOTSHARP_WEBHOOK_SECRET` - Webhook secret for signature verification
- `PROBOTSHARP_GITHUB_PRIVATEKEY` - Private key (file path or base64-encoded)

### Optional Configuration

- `LOG_LEVEL_DEFAULT` - Logging level (default: Information)
- `ASPNETCORE_URLS` - Port to listen on (default: http://localhost:5000)
- Database, Redis, metrics - see `.env.example`

## Local Development

### Using Smee.io for Webhook Forwarding

For local development, use [smee.io](https://smee.io/) to forward webhooks from GitHub to your local machine:

1. Go to https://smee.io/ and click "Start a new channel"
2. Install the smee client: `npm install -g smee-client`
3. Start forwarding: `smee -u YOUR_SMEE_URL -t http://localhost:5000/api/github/webhooks`
4. Configure your GitHub App to send webhooks to the Smee URL

### Testing with Fixtures

You can test handlers without GitHub by replaying webhook fixtures:

```bash
dotnet run --project ../../src/ProbotSharp.Bootstrap.Cli -- \
  receive . \
  -e issues.opened \
  -f ../../fixtures/issues-opened.json
```

## GitHub App Permissions

Make sure your GitHub App has the necessary permissions:

For the example handler:
- **Repository permissions**: Issues (Read & Write)
- **Subscribe to events**: Issues

## Deployment

### Using Docker

Build a Docker image:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "MyBot.dll"]
```

### Environment Variables

In production, set environment variables through your hosting platform (Azure, AWS, Heroku, etc.) instead of using a `.env` file.

### Database (Optional)

If you need to persist data, configure a PostgreSQL database:

```bash
DATABASE_CONNECTION_STRING=Host=your-db-host;Port=5432;Database=mybot;Username=user;Password=pass
```

## Resources

- [Probot Sharp Documentation](../../docs/)
- [GitHub Webhook Events](https://docs.github.com/en/webhooks/webhook-events-and-payloads)
- [Octokit.NET Documentation](https://octokitnet.readthedocs.io/)
- [Creating a GitHub App](https://docs.github.com/en/apps/creating-github-apps)

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Author

BOT_AUTHOR
