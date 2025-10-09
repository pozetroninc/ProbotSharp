# HttpExtensibilityBot - Custom HTTP Endpoints Example

Demonstrates how to add custom HTTP endpoints to ProbotSharp apps using ASP.NET Core Minimal API patterns. Shows HTTP extensibility achieving feature parity with Node.js Probot's `addHandler`/`getRouter` pattern.

## What This Bot Does

HttpExtensibilityBot demonstrates adding custom REST API endpoints alongside webhook handlers:

- **GET /api/demo/ping** - Simple ping endpoint
- **GET /api/demo/report** - Generate status report (with DI)
- **POST /api/demo/trigger** - Trigger custom action (with request body)
- **GET /api/demo/status/{id}** - Get status by ID (with route parameters)

Perfect for:
- Admin dashboards
- Status/health endpoints beyond basic health checks
- Triggering manual operations
- Exposing bot data via REST API
- Integration with other services

## Comparison with Node.js Probot

### Node.js Probot (Express)

```javascript
export default (app) => {
  app.addHandler("/api/demo/ping", (req, res) => {
    res.json({ message: "pong", timestamp: new Date() });
  });

  // Or using getRouter
  const router = app.getRouter("/api/demo");
  router.get("/report", async (req, res) => {
    const report = await generateReport();
    res.json(report);
  });
};
```

### ProbotSharp (HttpExtensibilityBot)

```text
public class HttpExtensibilityApp : IProbotApp
{
    public Task ConfigureRoutesAsync(IEndpointRouteBuilder endpoints, IServiceProvider serviceProvider)
    {
        var apiGroup = endpoints.MapGroup("/api/demo");

        apiGroup.MapGet("/ping", () => Results.Ok(new
        {
            message = "pong",
            timestamp = DateTime.UtcNow
        }));

        apiGroup.MapGet("/report", async (IReportingService reporting) =>
        {
            var report = await reporting.GenerateReportAsync();
            return Results.Ok(report);
        });

        return Task.CompletedTask;
    }
}
```

## Key Features

### 1. Route Groups

Organize related endpoints under a common prefix:

```text
var apiGroup = endpoints.MapGroup("/api/demo");
apiGroup.MapGet("/ping", ...);     // /api/demo/ping
apiGroup.MapGet("/report", ...);   // /api/demo/report
```

### 2. Dependency Injection

Inject services directly into endpoint handlers:

```text
apiGroup.MapGet("/report", async (IReportingService reporting) =>
{
    var report = await reporting.GenerateReportAsync();
    return Results.Ok(report);
});
```

### 3. Request Body Binding

Automatically deserialize JSON request bodies:

```text
apiGroup.MapPost("/trigger", async (
    [FromBody] TriggerRequest request,
    IReportingService reporting) =>
{
    await reporting.TriggerAsync(request.Action);
    return Results.Accepted();
});

public record TriggerRequest(string Action);
```

### 4. Route Parameters

Extract parameters from URL path:

```text
apiGroup.MapGet("/status/{id}", (string id, IReportingService reporting) =>
{
    var status = reporting.GetStatus(id);
    return status != null ? Results.Ok(status) : Results.NotFound();
});
```

### 5. Metadata and Documentation

Add OpenAPI/Swagger metadata:

```text
apiGroup.MapGet("/ping", () => Results.Ok(...))
    .WithName("DemoPing")
    .WithTags("demo")
    .WithDescription("Simple ping endpoint for testing");
```

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A GitHub account
- A GitHub App (see setup below)

## Quick Start

### 1. Get GitHub App Credentials

1. Go to [GitHub Apps Settings](https://github.com/settings/apps/new)
2. Create a new GitHub App:
   - **Name**: HttpExtensibilityBot-YourUsername (must be unique)
   - **Webhook URL**: `http://localhost:5000/webhooks`
   - **Webhook Secret**: `development`
   - **Permissions**: None required (this example focuses on HTTP endpoints)
3. After creation:
   - Note your **App ID**
   - Generate and download a **private key**
   - Save the private key as `private-key.pem` in this directory

### 2. Configure

Edit `appsettings.json` and replace `YOUR_GITHUB_APP_ID` with your actual App ID.

### 3. Run

```bash
cd examples/HttpExtensibilityBot
dotnet run
```

The bot will start at `http://localhost:5000`.

### 4. Test Endpoints

```bash
# Ping endpoint
curl http://localhost:5000/api/demo/ping

# Response:
# {"message":"pong","timestamp":"2025-10-07T12:00:00Z"}

# Generate report
curl http://localhost:5000/api/demo/report

# Response:
# {"totalEvents":42,"lastProcessed":"2025-10-07T11:55:00Z","queueDepth":3,"generatedAt":"2025-10-07T12:00:00Z"}

# Trigger action
curl -X POST http://localhost:5000/api/demo/trigger \
  -H "Content-Type: application/json" \
  -d '{"action":"refresh"}'

# Response:
# {"message":"Trigger accepted","action":"refresh"}

# Get status (will return 404 unless you capture the ID from trigger response)
curl http://localhost:5000/api/demo/status/some-id

# Response if found:
# {"action":"refresh","triggeredAt":"2025-10-07T12:00:00Z","status":"pending"}

# Response if not found:
# {"error":"Status not found","id":"some-id"}
```

## Project Structure

```
HttpExtensibilityBot/
├── HttpExtensibilityBot.csproj  # Project file
├── Program.cs                   # Application entry point
├── HttpExtensibilityApp.cs      # IProbotApp with custom routes
├── appsettings.json             # Configuration
└── README.md                    # This file
```

## Architecture

### App Lifecycle

ProbotSharp apps have three lifecycle phases:

1. **ConfigureAsync** (DI Setup): Register services before the DI container is built
2. **InitializeAsync** (Event Routing): Register webhook event handlers
3. **ConfigureRoutesAsync** (HTTP Routes): Register custom HTTP endpoints

```text
public class HttpExtensibilityApp : IProbotApp
{
    public string Name => "http-extensibility-demo";
    public string Version => "1.0.0";

    // Phase 1: Register services
    public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IReportingService, ReportingService>();
        return Task.CompletedTask;
    }

    // Phase 2: Register webhook handlers (none in this example)
    public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
    {
        return Task.CompletedTask;
    }

    // Phase 3: Register custom HTTP routes
    public Task ConfigureRoutesAsync(IEndpointRouteBuilder endpoints, IServiceProvider serviceProvider)
    {
        var apiGroup = endpoints.MapGroup("/api/demo");
        apiGroup.MapGet("/ping", () => Results.Ok(new { message = "pong" }));
        return Task.CompletedTask;
    }
}
```

### Service Registration

Register custom services in `ConfigureAsync`:

```text
public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
{
    // Scoped: New instance per request
    services.AddScoped<IReportingService, ReportingService>();

    // Singleton: Shared across all requests
    services.AddSingleton<IMetricsService, MetricsService>();

    // Transient: New instance every time
    services.AddTransient<IEmailService, EmailService>();

    return Task.CompletedTask;
}
```

### Route Configuration

Configure routes in `ConfigureRoutesAsync`:

```text
public Task ConfigureRoutesAsync(IEndpointRouteBuilder endpoints, IServiceProvider serviceProvider)
{
    // Option 1: Direct registration
    endpoints.MapGet("/custom", () => Results.Ok("Hello"));

    // Option 2: Route groups (recommended)
    var apiGroup = endpoints.MapGroup("/api/mybot");
    apiGroup.MapGet("/status", GetStatusAsync);
    apiGroup.MapPost("/action", PostActionAsync);

    return Task.CompletedTask;
}
```

## Advanced Examples

### Combining Webhook Handlers and HTTP Endpoints

```text
public class MyApp : IProbotApp
{
    public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IssueHandler>();
        services.AddScoped<IReportingService, ReportingService>();
        return Task.CompletedTask;
    }

    public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
    {
        // Register webhook handlers
        router.RegisterHandler("issues", "opened", typeof(IssueHandler));
        return Task.CompletedTask;
    }

    public Task ConfigureRoutesAsync(IEndpointRouteBuilder endpoints, IServiceProvider serviceProvider)
    {
        // Register HTTP endpoints
        var apiGroup = endpoints.MapGroup("/api/mybot");

        apiGroup.MapGet("/issues", async (IReportingService reporting) =>
        {
            var issues = await reporting.GetProcessedIssuesAsync();
            return Results.Ok(issues);
        });

        return Task.CompletedTask;
    }
}
```

### Authentication and Authorization

```text
public Task ConfigureRoutesAsync(IEndpointRouteBuilder endpoints, IServiceProvider serviceProvider)
{
    var apiGroup = endpoints.MapGroup("/api/admin")
        .RequireAuthorization("AdminPolicy");

    apiGroup.MapGet("/settings", GetSettingsAsync);
    apiGroup.MapPost("/settings", UpdateSettingsAsync);

    return Task.CompletedTask;
}
```

### OpenAPI/Swagger Integration

```text
// In Program.cs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Then visit http://localhost:5000/swagger to see API documentation
```

### Validation

```text
using System.ComponentModel.DataAnnotations;

public record CreateIssueRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Title { get; init; }

    public string? Body { get; init; }
}

apiGroup.MapPost("/issues", async (
    [FromBody] CreateIssueRequest request,
    IIssueService issues) =>
{
    // Validation happens automatically
    var issue = await issues.CreateAsync(request);
    return Results.Created($"/api/issues/{issue.Id}", issue);
});
```

## Use Cases

### Admin Dashboard API

```text
var adminGroup = endpoints.MapGroup("/api/admin");

adminGroup.MapGet("/stats", async (IStatsService stats) =>
{
    return Results.Ok(new
    {
        totalWebhooks = await stats.GetTotalWebhooksAsync(),
        activeInstallations = await stats.GetActiveInstallationsAsync(),
        errorRate = await stats.GetErrorRateAsync()
    });
});

adminGroup.MapPost("/clear-cache", async (ICacheService cache) =>
{
    await cache.ClearAsync();
    return Results.NoContent();
});
```

### Webhook Replay API

```text
var webhooksGroup = endpoints.MapGroup("/api/webhooks");

webhooksGroup.MapGet("/history", async (IWebhookHistoryService history) =>
{
    var webhooks = await history.GetRecentAsync(100);
    return Results.Ok(webhooks);
});

webhooksGroup.MapPost("/replay/{id}", async (
    string id,
    IWebhookReplayService replay) =>
{
    await replay.ReplayAsync(id);
    return Results.Accepted();
});
```

### Integration Endpoints

```text
var integrationGroup = endpoints.MapGroup("/api/integrations");

integrationGroup.MapPost("/slack/webhook", async (
    [FromBody] SlackWebhook webhook,
    ISlackIntegration slack) =>
{
    await slack.ProcessWebhookAsync(webhook);
    return Results.Ok();
});

integrationGroup.MapPost("/jira/sync", async (
    [FromBody] JiraSyncRequest request,
    IJiraIntegration jira) =>
{
    var result = await jira.SyncAsync(request);
    return Results.Ok(result);
});
```

## Deployment

HttpExtensibilityBot uses in-memory mode (no external dependencies). Deploy same as MinimalBot:

- **Local**: `dotnet run`
- **Docker**: See [MinimalBot Dockerfile](../MinimalBot/README.md#docker-single-container)
- **Azure Web App**: Free tier, zero infrastructure costs
- **Railway/Render**: Free tier supported

See [MinimalBot deployment docs](../MinimalBot/README.md#deployment-options) for detailed instructions.

## Testing

Test custom endpoints:

```bash
# Using curl
curl http://localhost:5000/api/demo/ping

# Using HTTPie
http GET localhost:5000/api/demo/report

# Using PowerShell
Invoke-RestMethod -Uri http://localhost:5000/api/demo/ping -Method Get
```

## Learn More

- **Architecture**: [../../docs/Architecture.md](../../docs/Architecture.md) (HTTP Extensibility section)
- **Probot Migration**: [../../docs/Probot-to-ProbotSharp-Guide.md](../../docs/Probot-to-ProbotSharp-Guide.md)
- **ASP.NET Core Minimal APIs**: [Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- **Local Development**: [../../docs/LocalDevelopment.md](../../docs/LocalDevelopment.md)

## License

MIT - see [LICENSE](../../LICENSE)
