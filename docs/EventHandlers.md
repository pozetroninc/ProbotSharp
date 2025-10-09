# Event Handlers Guide

This guide explains how to create event handlers in ProbotSharp to respond to GitHub webhook events.

## Table of Contents

- [Overview](#overview)
- [Basic Handler Pattern](#basic-handler-pattern)
- [Wildcard Patterns](#wildcard-patterns)
- [Handler Registration](#handler-registration)
- [Multiple Handlers](#multiple-handlers)
- [Error Handling](#error-handling)
- [Best Practices](#best-practices)
- [Comparison with Node.js Probot](#comparison-with-nodejs-probot)

## Overview

ProbotSharp uses a declarative, attribute-based approach to event handling. Handlers implement the `IEventHandler` interface and use the `[EventHandler]` attribute to specify which events they handle.

**Core Components**:
- **IEventHandler**: Interface that all event handlers must implement
- **EventHandlerAttribute**: Declares which events/actions a handler processes
- **EventRouter**: Routes webhook events to registered handlers
- **ProbotSharpContext**: Provides access to event data, GitHub API, and utilities

## Basic Handler Pattern

### Simple Event Handler

```text
using ProbotSharp.Application.Abstractions;
using ProbotSharp.Domain.Context;

[EventHandler("issues", "opened")]
public class IssueOpenedHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        var (owner, repo, issueNumber) = context.Issue();

        context.Logger.LogInformation(
            "New issue #{IssueNumber} opened in {Owner}/{Repo}",
            issueNumber, owner, repo);

        // Post a greeting comment
        await context.GitHub.Issue.Comment.Create(
            owner, repo, issueNumber,
            "Thanks for opening this issue! 👋");
    }
}
```

### Handler with Dependency Injection

```text
[EventHandler("pull_request", "opened")]
public class PullRequestValidator : IEventHandler
{
    private readonly ICodeAnalysisService _codeAnalysis;
    private readonly ILogger<PullRequestValidator> _logger;

    public PullRequestValidator(
        ICodeAnalysisService codeAnalysis,
        ILogger<PullRequestValidator> logger)
    {
        _codeAnalysis = codeAnalysis;
        _logger = logger;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        var (owner, repo) = context.Repo();
        var prNumber = context.Payload["pull_request"]?["number"]?.ToObject<int>() ?? 0;

        var analysisResult = await _codeAnalysis.AnalyzeAsync(owner, repo, prNumber, cancellationToken);

        if (!analysisResult.IsValid)
        {
            _logger.LogWarning("PR #{PrNumber} failed validation", prNumber);
            // Post validation feedback
        }
    }
}
```

## Wildcard Patterns

ProbotSharp supports wildcard patterns for flexible event subscription.

### Handle All Actions for an Event

```text
[EventHandler("issues", "*")]
public class AllIssueEventsHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        // Handles: issues.opened, issues.closed, issues.edited, issues.labeled, etc.
        var action = context.EventAction;

        context.Logger.LogInformation(
            "Issue event: {Action} in {Repository}",
            action,
            context.GetRepositoryFullName());

        await Task.CompletedTask;
    }
}
```

### Handle All Events (Global Wildcard)

This is the equivalent of Node.js Probot's `app.onAny()`:

```text
[EventHandler("*", null)]
public class AllEventsLogger : IEventHandler
{
    private readonly IMetricsService _metrics;

    public AllEventsLogger(IMetricsService metrics)
    {
        _metrics = metrics;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        // Handles all webhook events
        context.Logger.LogInformation(
            "Event received: {Event}.{Action} from {Repository}",
            context.EventName,
            context.EventAction ?? "null",
            context.GetRepositoryFullName());

        // Record metrics for all events
        _metrics.RecordEvent(context.EventName, context.EventAction);

        await Task.CompletedTask;
    }
}
```

### Multiple Event Handlers

A single handler class can handle multiple event patterns by applying multiple attributes:

```text
[EventHandler("issues", "opened")]
[EventHandler("issues", "reopened")]
[EventHandler("pull_request", "opened")]
public class NewItemGreeter : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        var greeting = context.EventName switch
        {
            "issues" => "Thanks for opening this issue!",
            "pull_request" => "Thanks for your contribution!",
            _ => "Thanks!"
        };

        var (owner, repo, number) = context.Issue(); // Works for both issues and PRs

        await context.GitHub.Issue.Comment.Create(owner, repo, number, greeting);
    }
}
```

## Handler Registration

### Automatic Registration (Recommended)

Use the `[EventHandler]` attribute and register handlers in your app's `InitializeAsync` method:

```text
public class MyApp : IProbotApp
{
    public string Name => "my-app";
    public string Version => "1.0.0";

    public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
    {
        // Register handler as scoped service
        services.AddScoped<IssueOpenedHandler>();
        return Task.CompletedTask;
    }

    public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
    {
        // Register handler with router
        router.RegisterHandler("issues", "opened", typeof(IssueOpenedHandler));

        // Or use reflection to auto-register all handlers in assembly
        var handlerTypes = typeof(MyApp).Assembly.GetTypes()
            .Where(t => typeof(IEventHandler).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var handlerType in handlerTypes)
        {
            var attributes = handlerType.GetCustomAttributes(typeof(EventHandlerAttribute), false)
                .Cast<EventHandlerAttribute>();

            foreach (var attr in attributes)
            {
                router.RegisterHandler(attr.EventName, attr.Action, handlerType);
            }
        }

        return Task.CompletedTask;
    }

    public Task ConfigureRoutesAsync(IEndpointRouteBuilder endpoints, IServiceProvider serviceProvider)
    {
        return Task.CompletedTask;
    }
}
```

### Registration Helper

Consider creating a helper extension method for automatic handler registration:

```text
public static class EventRouterExtensions
{
    public static void RegisterHandlersFromAssembly(this EventRouter router, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => typeof(IEventHandler).IsAssignableFrom(t)
                     && !t.IsAbstract
                     && !t.IsInterface);

        foreach (var handlerType in handlerTypes)
        {
            var attributes = handlerType.GetCustomAttributes<EventHandlerAttribute>();

            foreach (var attr in attributes)
            {
                router.RegisterHandler(attr.EventName, attr.Action, handlerType);
            }
        }
    }
}

// Usage:
public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
{
    router.RegisterHandlersFromAssembly(typeof(MyApp).Assembly);
    return Task.CompletedTask;
}
```

## Multiple Handlers

ProbotSharp executes **all matching handlers** for each event. This allows multiple concerns to respond independently:

```text
// Handler 1: Log all issue events
[EventHandler("issues", "*")]
public class IssueLogger : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        context.Logger.LogInformation("Issue event: {Action}", context.EventAction);
        await Task.CompletedTask;
    }
}

// Handler 2: Greet on issue opened
[EventHandler("issues", "opened")]
public class IssueGreeter : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        var (owner, repo, issueNumber) = context.Issue();
        await context.GitHub.Issue.Comment.Create(owner, repo, issueNumber, "Welcome!");
    }
}

// Handler 3: Auto-label on issue opened
[EventHandler("issues", "opened")]
public class IssueLabeler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        var (owner, repo, issueNumber) = context.Issue();
        var title = context.Payload["issue"]?["title"]?.ToString() ?? "";

        if (title.Contains("bug", StringComparison.OrdinalIgnoreCase))
        {
            await context.GitHub.Issue.Labels.AddToIssue(owner, repo, issueNumber, new[] { "bug" });
        }
    }
}
```

**For `issues.opened` events, all three handlers execute**:
1. `IssueLogger` logs the event (wildcard match)
2. `IssueGreeter` posts greeting comment (exact match)
3. `IssueLabeler` adds labels based on title (exact match)

**Execution Order**:
- Handlers execute in registration order
- Each handler runs independently in its own DI scope
- One handler's failure doesn't prevent others from executing

## Error Handling

### Automatic Error Logging

ProbotSharp automatically catches and logs exceptions from all handlers. You don't need to register a global error handler like Node.js Probot's `app.onError()`.

```text
[EventHandler("issues", "opened")]
public class FlakyHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Something went wrong!");
        // Exception is automatically logged with:
        // - Handler name: FlakyHandler
        // - Event: issues.opened
        // - Installation ID
        // - Repository
        // - Full stack trace
        // - Other handlers continue executing
    }
}
```

### Custom Error Handling

Implement try-catch within your handler for custom error recovery:

```text
[EventHandler("pull_request", "opened")]
public class PullRequestHandler : IEventHandler
{
    private readonly INotificationService _notifications;

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        try
        {
            await ProcessPullRequestAsync(context, cancellationToken);
        }
        catch (RateLimitExceededException ex)
        {
            // Custom handling for rate limits
            context.Logger.LogWarning(ex, "Rate limit exceeded, scheduling retry");
            await _notifications.SendAlertAsync("Rate limit hit", ex);
            throw; // Re-throw to ensure EventRouter logs it
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Gracefully handle missing resources
            context.Logger.LogInformation("Resource not found, skipping processing");
            // Don't re-throw - we handled this error
        }
        catch (Exception ex)
        {
            // Fallback for unexpected errors
            context.Logger.LogError(ex, "Unexpected error, using safe fallback");
            await SafeFallbackAsync(context, cancellationToken);
            // Don't re-throw if fallback succeeded
        }
    }
}
```

### Error Handling Best Practices

1. **Let EventRouter log most errors**: For unexpected exceptions, let them propagate to EventRouter for automatic logging
2. **Handle specific exceptions**: Catch and handle known exception types (rate limits, validation errors, etc.)
3. **Re-throw after custom handling**: If you want both custom logic AND automatic logging, re-throw after handling
4. **Don't catch cancellation**: Never catch `OperationCanceledException` unless you have a specific reason
5. **Use structured logging**: Include context in log messages (issue number, PR number, etc.)

```text
public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
{
    try
    {
        var (owner, repo, issueNumber) = context.Issue();

        // Normal processing
        await ProcessIssueAsync(owner, repo, issueNumber, cancellationToken);
    }
    catch (OperationCanceledException)
    {
        // Don't log cancellation as error - it's expected
        throw;
    }
    catch (ValidationException ex)
    {
        // Handle validation errors gracefully
        context.Logger.LogWarning(ex, "Validation failed for issue #{IssueNumber}", issueNumber);
        return; // Don't re-throw
    }
    catch (Exception ex)
    {
        // Add context to automatic logging
        context.Logger.LogError(ex, "Failed to process issue #{IssueNumber} in {Repository}",
            issueNumber, context.GetRepositoryFullName());
        throw; // Let EventRouter log with full details
    }
}
```

## Best Practices

### 1. Keep Handlers Focused

Each handler should have a single responsibility:

```text
// ✅ Good: Focused responsibility
[EventHandler("issues", "opened")]
public class IssueGreeter : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        var (owner, repo, issueNumber) = context.Issue();
        await context.GitHub.Issue.Comment.Create(owner, repo, issueNumber, "Welcome!");
    }
}

// ❌ Bad: Multiple responsibilities
[EventHandler("issues", "opened")]
public class IssueProcessor : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        // Greeting
        await PostGreeting();
        // Auto-labeling
        await AddLabels();
        // Assign reviewers
        await AssignReviewers();
        // Send notifications
        await SendNotifications();
        // Update project board
        await UpdateProjectBoard();
    }
}
```

### 2. Use Dependency Injection

Leverage DI for testability and maintainability:

```text
[EventHandler("issues", "opened")]
public class IssueAnalyzer : IEventHandler
{
    private readonly IAnalysisService _analysis;
    private readonly INotificationService _notifications;
    private readonly ILogger<IssueAnalyzer> _logger;

    public IssueAnalyzer(
        IAnalysisService analysis,
        INotificationService notifications,
        ILogger<IssueAnalyzer> logger)
    {
        _analysis = analysis;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        var (owner, repo, issueNumber) = context.Issue();
        var result = await _analysis.AnalyzeIssueAsync(owner, repo, issueNumber, cancellationToken);

        if (result.RequiresAttention)
        {
            await _notifications.NotifyTeamAsync(result, cancellationToken);
        }
    }
}
```

### 3. Check for Bot Events

Avoid infinite loops by skipping bot-created events:

```text
[EventHandler("issues", "opened")]
public class IssueHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        if (context.IsBot())
        {
            context.Logger.LogDebug("Skipping bot-created issue");
            return;
        }

        // Process human-created issues
    }
}
```

### 4. Use Structured Logging

Include context in log messages for better observability:

```text
public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
{
    var (owner, repo, issueNumber) = context.Issue();

    context.Logger.LogInformation(
        "Processing issue #{IssueNumber} in {Repository} by {Author}",
        issueNumber,
        context.GetRepositoryFullName(),
        context.Payload["issue"]?["user"]?["login"]?.ToString());
}
```

### 5. Handle Cancellation

Respect cancellation tokens for graceful shutdown:

```text
public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
{
    var (owner, repo, issueNumber) = context.Issue();

    // Pass cancellation token to all async operations
    var comments = await context.GitHub.Issue.Comment.GetAllForIssue(
        owner, repo, issueNumber,
        cancellationToken);

    foreach (var comment in comments)
    {
        // Check cancellation before expensive operations
        cancellationToken.ThrowIfCancellationRequested();

        await ProcessCommentAsync(comment, cancellationToken);
    }
}
```

### 6. Use Context Helpers

Leverage built-in helpers for common operations:

```text
public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
{
    // ✅ Good: Use helpers
    var (owner, repo, issueNumber) = context.Issue();
    var repoFullName = context.GetRepositoryFullName();
    var isBot = context.IsBot();

    // ❌ Bad: Manual payload parsing
    var owner = context.Payload["repository"]?["owner"]?["login"]?.ToString();
    var repo = context.Payload["repository"]?["name"]?.ToString();
    var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>() ?? 0;
}
```

## Comparison with Node.js Probot

| Feature | Node.js Probot | ProbotSharp |
|---------|----------------|-------------|
| **Event subscription** | `app.on("event.action", handler)` | `[EventHandler("event", "action")]` |
| **Wildcard (all events)** | `app.onAny(handler)` | `[EventHandler("*", null)]` |
| **Wildcard (event actions)** | `app.on("event", handler)` | `[EventHandler("event", "*")]` |
| **Multiple handlers** | Sequential registration | Multiple `[EventHandler]` attributes |
| **Error handling** | `app.onError(callback)` | Automatic logging in EventRouter |
| **Dependency injection** | Manual in handler | Constructor injection |
| **Handler registration** | Imperative | Declarative (attribute-based) |
| **Context object** | `context` parameter | `ProbotSharpContext` parameter |
| **Async support** | Native (JavaScript) | Native (C# async/await) |

### Migration Examples

#### Node.js Probot: Basic Handler

```javascript
export default (app) => {
  app.on("issues.opened", async (context) => {
    const issueComment = context.issue({
      body: "Thanks for opening this issue!",
    });
    await context.octokit.issues.createComment(issueComment);
  });
};
```

#### ProbotSharp: Basic Handler

```text
[EventHandler("issues", "opened")]
public class IssueGreeter : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        var (owner, repo, issueNumber) = context.Issue();
        await context.GitHub.Issue.Comment.Create(
            owner, repo, issueNumber,
            "Thanks for opening this issue!");
    }
}
```

#### Node.js Probot: Wildcard Handler

```javascript
export default (app) => {
  app.onAny(async (context) => {
    app.log.info({ event: context.name, action: context.payload.action });
  });
};
```

#### ProbotSharp: Wildcard Handler

```text
[EventHandler("*", null)]
public class AllEventsLogger : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        context.Logger.LogInformation(
            "Event: {Event}, Action: {Action}",
            context.EventName,
            context.EventAction);
        await Task.CompletedTask;
    }
}
```

#### Node.js Probot: Error Handler

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

#### ProbotSharp: Automatic Error Handling

```text
[EventHandler("issues", "opened")]
public class IssueProcessor : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Something went wrong");
        // Automatically logged by EventRouter with full context
        // No explicit error handler registration needed
    }
}
```

## See Also

- [Architecture.md](./Architecture.md) - EventRouter internals and error handling
- [Probot-to-ProbotSharp-Guide.md](./Probot-to-ProbotSharp-Guide.md) - Side-by-side comparison with Node.js Probot
- [ContextHelpers.md](./ContextHelpers.md) - ProbotSharpContext helper methods
- [LocalDevelopment.md](./LocalDevelopment.md) - Testing handlers locally
- [BestPractices.md](./BestPractices.md) - General best practices for ProbotSharp apps
