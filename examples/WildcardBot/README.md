# WildcardBot - Wildcard Event Handler Example

Demonstrates ProbotSharp's wildcard event handler patterns, including global wildcards (`*`), event wildcards (`event.*`), and specific event handlers.

## What This Bot Does

WildcardBot showcases four different handler patterns working together:

1. **AllEventsLogger** (`[EventHandler("*", null)]`) - Logs all webhook events (equivalent to Node.js Probot's `app.onAny()`)
2. **AllIssueEventsHandler** (`[EventHandler("issues", "*")]`) - Handles all issue events (opened, closed, edited, labeled, etc.)
3. **SpecificEventHandler** (`[EventHandler("issues", "opened")]`) - Posts greeting comments on newly opened issues
4. **MetricsCollector** (`[EventHandler("*", null)]`) - Collects metrics from all events for observability

When you open a new issue, **all four handlers execute** in order, demonstrating ProbotSharp's flexible event routing.

## Comparison with Node.js Probot

### Node.js Probot

```javascript
export default (app) => {
  // Handle all events
  app.onAny(async (context) => {
    app.log.info({ event: context.name, action: context.payload.action });
  });

  // Handle all issue events
  app.on("issues", async (context) => {
    app.log.info({ action: context.payload.action });
  });

  // Handle specific event
  app.on("issues.opened", async (context) => {
    await context.octokit.issues.createComment({
      ...context.issue(),
      body: "Thanks for opening this issue!"
    });
  });
};
```

### ProbotSharp (WildcardBot)

```csharp
// Handle all events (app.onAny equivalent)
[EventHandler("*", null)]
public class AllEventsLogger : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        context.Logger.LogInformation("Event: {Event}.{Action}",
            context.EventName, context.EventAction);
        await Task.CompletedTask;
    }
}

// Handle all issue events (app.on("issues") equivalent)
[EventHandler("issues", "*")]
public class AllIssueEventsHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        context.Logger.LogInformation("Issue action: {Action}", context.EventAction);
        await Task.CompletedTask;
    }
}

// Handle specific event (app.on("issues.opened") equivalent)
[EventHandler("issues", "opened")]
public class SpecificEventHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        var (owner, repo, issueNumber) = context.Issue();
        await context.GitHub.Issue.Comment.Create(
            owner, repo, issueNumber,
            "Thanks for opening this issue!");
    }
}
```

## Key Concepts

### Wildcard Patterns

| Pattern | Description | Node.js Equivalent |
|---------|-------------|-------------------|
| `[EventHandler("*", null)]` | Matches all events | `app.onAny()` |
| `[EventHandler("issues", "*")]` | Matches all issue events | `app.on("issues")` |
| `[EventHandler("issues", "opened")]` | Matches specific event | `app.on("issues.opened")` |

### Multiple Handlers

ProbotSharp executes **all matching handlers** for each event. When an issue is opened:

1. `AllEventsLogger` logs it (matches `*`)
2. `AllIssueEventsHandler` processes it (matches `issues.*`)
3. `SpecificEventHandler` posts comment (matches `issues.opened`)
4. `MetricsCollector` records metrics (matches `*`)

**Handlers execute independently**:
- Each handler runs in its own DI scope
- One handler's failure doesn't prevent others from executing
- Exceptions are automatically logged by EventRouter

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A GitHub account
- A GitHub App (see setup below)

## Quick Start

### 1. Get GitHub App Credentials

1. Go to [GitHub Apps Settings](https://github.com/settings/apps/new)
2. Create a new GitHub App:
   - **Name**: WildcardBot-YourUsername (must be unique)
   - **Webhook URL**: `http://localhost:5000/webhooks`
   - **Webhook Secret**: `development`
   - **Permissions**:
     - Repository: Issues (Read & write)
     - Repository: Metadata (Read-only)
   - **Subscribe to events**:
     - Issues
     - Issue comment
     - Pull request (optional, for testing other events)
3. After creation:
   - Note your **App ID**
   - Generate and download a **private key**
   - Save the private key as `private-key.pem` in this directory

### 2. Configure

Edit `appsettings.json` and replace `YOUR_GITHUB_APP_ID` with your actual App ID.

### 3. Run

```bash
cd examples/WildcardBot
dotnet run
```

The bot will start at `http://localhost:5000`.

### 4. Test with smee.io

```bash
# Install smee client (requires Node.js)
npm install -g smee-client

# Create a channel at https://smee.io
# Then forward webhooks:
smee -u https://smee.io/YOUR_CHANNEL -t http://localhost:5000/webhooks
```

Update your GitHub App's webhook URL to your smee.io channel URL.

### 5. Install and Test

1. Install your GitHub App on a test repository
2. Create a new issue
3. Watch the console output - you'll see all four handlers execute:

```
[AllEventsLogger] Event: issues.opened | Repository: owner/repo | Sender: username
[AllIssueEventsHandler] Issue #1 opened in owner/repo
[SpecificEventHandler] New issue #1: "Test Issue" by @username
[SpecificEventHandler] Posted greeting comment on issue #1
[MetricsCollector] Event: issues.opened | Total count: 1
```

4. Check the issue - `SpecificEventHandler` will have posted a greeting comment explaining how multiple handlers processed the event

## Project Structure

```
WildcardBot/
├── WildcardBot.csproj         # Project file
├── Program.cs                 # Application entry point
├── WildcardApp.cs             # IProbotApp implementation (registers handlers)
├── AllEventsLogger.cs         # Wildcard handler: logs all events (*)
├── AllIssueEventsHandler.cs   # Event wildcard: handles all issue events (issues.*)
├── SpecificEventHandler.cs    # Specific handler: greets on issues.opened
├── MetricsCollector.cs        # Wildcard handler: collects metrics from all events (*)
├── appsettings.json           # Configuration
└── README.md                  # This file
```

## Handler Execution Flow

When GitHub sends an `issues.opened` webhook:

```
GitHub Webhook (issues.opened)
        ↓
EventRouter receives webhook
        ↓
EventRouter matches handlers:
  ✓ AllEventsLogger      (matches "*")
  ✓ AllIssueEventsHandler (matches "issues.*")
  ✓ SpecificEventHandler  (matches "issues.opened")
  ✓ MetricsCollector      (matches "*")
        ↓
Execute handlers sequentially:
  1. AllEventsLogger.HandleAsync()      [logs event]
  2. MetricsCollector.HandleAsync()     [records metric]
  3. AllIssueEventsHandler.HandleAsync() [logs issue details]
  4. SpecificEventHandler.HandleAsync()  [posts comment]
        ↓
All handlers complete
```

## Error Handling

WildcardBot demonstrates automatic error handling. Try modifying a handler to throw an exception:

```csharp
[EventHandler("issues", "opened")]
public class SpecificEventHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        throw new InvalidOperationException("Test error!");
        // This exception is automatically caught and logged by EventRouter
        // Other handlers continue executing
    }
}
```

The exception will be logged with full context:
- Handler name: `SpecificEventHandler`
- Event: `issues.opened`
- Repository, installation ID, sender
- Full stack trace

Other handlers (AllEventsLogger, AllIssueEventsHandler, MetricsCollector) will continue executing normally.

## Use Cases for Wildcard Handlers

### Global Wildcard (`*`)

Perfect for cross-cutting concerns:

- **Logging**: Log all webhook events for audit trail
- **Metrics**: Collect event counts, timing, and patterns
- **Observability**: Record events in distributed tracing
- **Security**: Monitor for suspicious patterns
- **Rate limiting**: Track event volume per repository
- **Debugging**: Log all events during development

### Event Wildcard (`event.*`)

Perfect for event-specific cross-cutting concerns:

- **Issue tracking**: Sync all issue events to external system
- **Pull request automation**: Apply policies to all PR events
- **Repository analytics**: Track repository activity patterns
- **Notification routing**: Send different notifications per event type

### Combining Patterns

```text
// Global logger for all events
[EventHandler("*", null)]
public class AllEventsLogger : IEventHandler { }

// Track all issue activity
[EventHandler("issues", "*")]
public class IssueActivityTracker : IEventHandler { }

// Track all PR activity
[EventHandler("pull_request", "*")]
public class PRActivityTracker : IEventHandler { }

// Specific business logic
[EventHandler("issues", "opened")]
public class IssueGreeter : IEventHandler { }

[EventHandler("pull_request", "opened")]
public class PRValidator : IEventHandler { }
```

## Advanced Examples

### Conditional Logic Based on Event Type

```text
[EventHandler("*", null)]
public class SmartRouter : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        switch (context.EventName)
        {
            case "issues":
                await HandleIssueEventAsync(context, ct);
                break;
            case "pull_request":
                await HandlePullRequestEventAsync(context, ct);
                break;
            default:
                context.Logger.LogDebug("Unhandled event type: {Event}", context.EventName);
                break;
        }
    }
}
```

### Metrics Dashboard Integration

```text
[EventHandler("*", null)]
public class PrometheusMetrics : IEventHandler
{
    private readonly IMetricsPort _metrics;

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        // Record event count
        _metrics.IncrementCounter("github_webhooks_total",
            new[] { $"event:{context.EventName}" });

        // Record processing time (use ITracingPort for timing)
        await Task.CompletedTask;
    }
}
```

## Deployment

WildcardBot uses in-memory mode (no external dependencies). Deploy same as MinimalBot:

- **Local**: `dotnet run`
- **Docker**: See [MinimalBot Dockerfile](../MinimalBot/README.md#docker-single-container)
- **Azure Web App**: Free tier, zero infrastructure costs
- **Railway/Render**: Free tier supported

See [MinimalBot deployment docs](../MinimalBot/README.md#deployment-options) for detailed instructions.

## Learn More

- **Event Handlers Guide**: [../../docs/EventHandlers.md](../../docs/EventHandlers.md)
- **Architecture**: [../../docs/Architecture.md](../../docs/Architecture.md) (Event Routing and Error Handling section)
- **Probot Migration**: [../../docs/Probot-to-ProbotSharp-Guide.md](../../docs/Probot-to-ProbotSharp-Guide.md)
- **Local Development**: [../../docs/LocalDevelopment.md](../../docs/LocalDevelopment.md)

## License

MIT - see [LICENSE](../../LICENSE)
