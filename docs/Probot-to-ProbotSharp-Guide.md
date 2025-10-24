## Probot (Node.js) to ProbotSharp (.NET) — Side-by-side Guide

This guide maps common Probot docs examples to concrete ProbotSharp equivalents in this repository.

### 1) Webhooks: receiving and handling events

Probot (listen to push):

```18:25:https://github.com/probot/probot/blob/master/docs/webhooks.md
export default (app) => {
  app.on("push", async (context) => {
    // Code was pushed to the repository
    app.log.info("Received push event", context.payload);
  });
};
```

ProbotSharp (greet on issues.opened):

```16:69:examples/HelloWorldBot/IssueGreeter.cs
[EventHandler("issues", "opened")]
public class IssueGreeter : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Received issue opened event for {Repository}",
            context.GetRepositoryFullName());

        if (context.IsBot())
        {
            context.Logger.LogDebug("Issue was created by a bot, skipping greeting");
            return;
        }

        var (owner, repo, issueNumber) = context.Issue();
        var authorLogin = context.Payload["issue"]?["user"]?["login"]?.ToObject<string>() ?? "there";

        try
        {
            var comment = $"Hello @{authorLogin}! 👋\n\nThanks for opening this issue...";
            await context.GitHub.Issue.Comment.Create(owner, repo, issueNumber, comment);
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "Failed to post greeting comment on issue #{IssueNumber}: {ErrorMessage}", issueNumber, ex.Message);
            throw;
        }
    }
}
```

Key differences:
- Probot uses `app.on("event[.action]")`; ProbotSharp uses `[EventHandler("event", "action")]` with DI + strong typing.
- Parameter extraction helpers: `context.issue()` vs `context.Issue()` (tuple in C#).

#### Handling All Events (onAny equivalent)

Probot (Node.js):

```javascript
export default (app) => {
  app.onAny(async (context) => {
    app.log.info({ event: context.name, action: context.payload.action });
  });
};
```

ProbotSharp (wildcard handler):

```csharp
[EventHandler("*", null)]
public class AllEventsLogger : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Event: {Event}, Action: {Action}",
            context.EventName,
            context.EventAction);

        await Task.CompletedTask;
    }
}
```

Key differences:
- Probot uses `app.onAny()` for imperative registration; ProbotSharp uses wildcard pattern `[EventHandler("*", null)]` for declarative registration.
- You can also use partial wildcards: `[EventHandler("issues", "*")]` handles all issue events.

#### Error Handling (onError equivalent)

Probot (Node.js):

```javascript
export default (app) => {
  app.on("issues.opened", async (context) => {
    throw new Error("Something went wrong");
  });

  app.onError(async (error) => {
    app.log.error(error);
  });
};
```

ProbotSharp (automatic error logging):

```csharp
[EventHandler("issues", "opened")]
public class IssueProcessor : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Something went wrong");
        // Error is automatically caught and logged by EventRouter
        // No global error handler registration needed
    }
}
```

Key differences:
- Probot provides `app.onError()` for global error handling; ProbotSharp automatically catches and logs all handler exceptions in EventRouter.
- Errors are logged with full context (handler name, event, installation) without requiring explicit registration.
- If you need custom error handling, implement try-catch within your handler or use middleware/DI interceptors.

### 2) HTTP routes: adding custom endpoints

Probot (Express v5):

```14:46:https://github.com/probot/probot/blob/master/docs/http.md
import Express from "express";
import { createNodeMiddleware, createProbot } from "probot";

const express = Express();

const app = (probot) => {
  probot.on("push", async () => {
    probot.log.info("Push event received");
  });
};

const middleware = await createNodeMiddleware(app, {
  webhooksPath: "/api/github/webhooks",
  probot: createProbot({ env: { APP_ID, PRIVATE_KEY, WEBHOOK_SECRET } }),
});

express.use(middleware);
express.get("/custom-route", (req, res) => res.json({ status: "ok" }));
express.listen(3000);
```

ProbotSharp (Minimal API + DI):

```38:101:examples/HttpExtensibilityBot/HttpExtensibilityApp.cs
public Task ConfigureRoutesAsync(IEndpointRouteBuilder endpoints, IServiceProvider serviceProvider)
{
    var apiGroup = endpoints.MapGroup("/api/demo");
    apiGroup.MapGet("/ping", () => Results.Ok(new { message = "pong", timestamp = DateTime.UtcNow }));
    apiGroup.MapGet("/report", async (IReportingService reporting) => Results.Ok(await reporting.GenerateReportAsync()));
    apiGroup.MapPost("/trigger", async ([FromBody] TriggerRequest request, IReportingService reporting, ILogger<HttpExtensibilityApp> logger) => { /* ... */ });
    apiGroup.MapGet("/status/{id}", (string id, IReportingService reporting) => { /* ... */ });
    return Task.CompletedTask;
}
```

Key differences:
- Probot relies on `createNodeMiddleware` and Express/Fastify; ProbotSharp wires endpoints in `ConfigureRoutesAsync` using Minimal API + DI.

### 3) Testing and simulating webhooks

Probot (Jest + `probot.receive`):

```12:56:https://github.com/probot/probot/blob/master/docs/testing.md
await probot.receive({ name: "issues", payload });
```

ProbotSharp (CLI receive to replay fixtures):

```304:347:docs/LocalDevelopment.md
dotnet run --project src/ProbotSharp.Bootstrap.Cli -- receive ./your-app -e issues.opened -f fixtures/issues-opened.json
```

Also see dry-run helpers:

```27:81:src/ProbotSharp.Domain/Context/ProbotSharpContextDryRunExtensions.cs
public static void LogDryRun(this ProbotSharpContext context, string action, object? parameters = null) { /* ... */ }
public static void ThrowIfNotDryRun(this ProbotSharpContext context, string message) { /* ... */ }
public static Task ExecuteOrLogAsync(this ProbotSharpContext context, string actionDescription, Func<Task> action, object? parameters = null) { /* ... */ }
```

### 4) Persistence/state: metadata and attachments

Probot (metadata/attachments):

```42:59:https://github.com/probot/probot/blob/master/docs/extensions.md
const kv = await metadata(context);
await kv.set("edits", (await kv.get("edits")) || 1);
```

ProbotSharp (durable metadata via PostgreSQL):

```16:75:examples/MetadataBot/EditCountTracker.cs
[EventHandler("issues", "edited")]
[EventHandler("issue_comment", "edited")]
public class EditCountTracker : IEventHandler
{
    private readonly MetadataService _metadata;
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct = default)
    {
        var current = await _metadata.GetAsync("edit_count", ct);
        var count = int.TryParse(current, out var c) ? c : 0;
        await _metadata.SetAsync("edit_count", (count + 1).ToString(), ct);
    }
}
```

Attachments:

```17:63:examples/AttachmentsBot/BuildStatusAttachment.cs
[EventHandler("issue_comment", "created")]
public class BuildStatusAttachment : IEventHandler
{
    private readonly CommentAttachmentService _attachments;
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        var payload = context.GetPayload<IssueCommentPayload>();
        if (payload.Comment.Body.Contains("/build-status"))
        {
            await _attachments.AddAsync(new CommentAttachment { Title = "Build Status", /* ... */ }, ct);
        }
    }
}
```

### 5) GraphQL (REST parity and GraphQL usage)

Probot (GraphQL): see `github-api.md` in Probot docs.

ProbotSharp (GraphQL query + mutation):

```15:186:examples/GraphQLBot/IssueGraphQLHandler.cs
[EventHandler("issues", "opened")]
public class IssueGraphQLHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        var (owner, repo, issueNumber) = context.Issue();
        var query = @"query($owner: String!, $name: String!, $number: Int!) { /* ... */ }";
        var result = await context.GraphQL.ExecuteAsync<IssueQueryResponse>(query, new { owner, name = repo, number = issueNumber }, cancellationToken);
        if (result.IsSuccess && result.Value != null)
        {
            await AddCommentViaGraphQL(context, result.Value.Repository.Issue.Id, cancellationToken);
        }
    }
    private async Task AddCommentViaGraphQL(ProbotSharpContext context, string issueId, CancellationToken ct) { /* mutation */ }
}
```

### 6) OAuth/install tokens (ports and adapters)

Port contract:

```10:13:src/ProbotSharp.Application/Ports/Outbound/IGitHubOAuthPort.cs
public interface IGitHubOAuthPort
{
    Task<Result<InstallationAccessToken>> CreateInstallationTokenAsync(InstallationId installationId, CancellationToken cancellationToken = default);
}
```

Adapter implementation and DI registration:

```21:99:src/ProbotSharp.Infrastructure/Adapters/GitHub/GitHubOAuthClient.cs
public sealed partial class GitHubOAuthClient : IGitHubOAuthPort
{
    public async Task<Result<InstallationAccessToken>> CreateInstallationTokenAsync(InstallationId installationId, CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync(installationId, cancellationToken);
        if (cached is not null && !cached.IsExpired(DateTimeOffset.UtcNow))
            return Result<InstallationAccessToken>.Success(cached);

        var client = _httpClientFactory.CreateClient("GitHubOAuth");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"app/installations/{installationId.Value}/access_tokens");
        var response = await client.SendAsync(request, cancellationToken);
        // parse JSON -> InstallationAccessToken, cache, return Result.Success or Failure
    }
}
```

```144:147:src/ProbotSharp.Infrastructure/Extensions/InfrastructureServiceCollectionExtensions.cs
services.AddTransient<IGitHubOAuthPort, GitHubOAuthClient>();
```

---

### 7) Webhook Deduplication (Architectural Difference)

**This is a critical behavioral difference between Probot and ProbotSharp.**

#### Probot (Node.js) Behavior

```javascript
// Probot does NOT deduplicate webhooks automatically
export default (app) => {
  app.on("push", async (context) => {
    // This will be called for EVERY webhook delivery
    // Even if GitHub sends the same delivery ID multiple times
    app.log.info("Processing push", context.id);
  });
};
```

- Does NOT automatically deduplicate webhooks by delivery ID
- All webhook deliveries are processed, including duplicates
- Application is responsible for implementing deduplication if needed
- Common pattern: Store processed delivery IDs in Redis/database

#### ProbotSharp (.NET) Behavior

```text
// ProbotSharp deduplicates by default (production safety)
var app = builder.Build();
app.UseProbotSharpMiddleware();
app.UseIdempotency(); // Prevents duplicate processing
```

- Automatic deduplication via `UseIdempotency()` middleware (enabled by default in all examples)
- Prevents duplicate processing for horizontal scaling and reliability
- Uses dual-layer strategy: database-level + distributed lock (Redis/in-memory)
- Configured via `Adapters.Idempotency` in appsettings.json

#### How to Disable (Probot-Compatible Behavior)

```text
var app = builder.Build();
app.UseProbotSharpMiddleware();
// Comment out or remove this line for Probot-compatible behavior:
// app.UseIdempotency();
```

#### When to Disable Idempotency

**Disable for:**
- ✅ Integration testing against Probot behavior
- ✅ Apps that implement custom deduplication logic
- ✅ Single-instance deployments where duplicates are acceptable
- ✅ Testing scenarios that verify duplicate handling

**Enable for (RECOMMENDED for production):**
- ✅ Production deployments
- ✅ Horizontal scaling (multiple instances)
- ✅ High-reliability requirements
- ✅ Preventing accidental duplicate actions

#### Configuration

**In-Memory (Development/Testing):**
```json
{
  "ProbotSharp": {
    "Adapters": {
      "Idempotency": {
        "Provider": "InMemory",
        "Options": {
          "ExpirationHours": "24"
        }
      }
    }
  }
}
```

**Redis (Production):**
```json
{
  "ProbotSharp": {
    "Adapters": {
      "Idempotency": {
        "Provider": "Redis",
        "Options": {
          "ConnectionString": "localhost:6379",
          "ExpirationHours": "24"
        }
      }
    }
  }
}
```

**Database (Enterprise):**
```json
{
  "ProbotSharp": {
    "Adapters": {
      "Idempotency": {
        "Provider": "Database",
        "Options": {
          "ExpirationHours": "24"
        }
      }
    }
  }
}
```

**See Also:**
- [Architecture docs - Idempotency Strategy](Architecture.md#idempotency-strategy)
- [Architecture docs - ADR-002: Dual-Layer Idempotency](Architecture.md#adr-002-dual-layer-idempotency-strategy)
- [Adapter Configuration Guide](AdapterConfiguration.md)

---

Refer to `docs/Architecture.md`, `docs/Extensions.md`, and `docs/LocalDevelopment.md` for deeper dives and runnable examples.


