# Metadata Storage

Metadata storage provides persistent key-value storage scoped to specific GitHub issues or pull requests. It's perfect for tracking state, counters, and custom data across multiple webhook events without cluttering your code with database boilerplate.

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Storage Architecture](#storage-architecture)
- [API Reference](#api-reference)
- [CRUD Operations](#crud-operations)
- [Configuration](#configuration)
- [Migration Patterns](#migration-patterns)
- [Advanced Usage](#advanced-usage)
- [Best Practices](#best-practices)

## Overview

Metadata storage allows you to:

- **Persist state** across webhook events for the same issue/PR
- **Track counters** (edit counts, comment counts, review rounds, etc.)
- **Store custom data** (timestamps, user preferences, workflow states)
- **Implement state machines** with persistent transitions
- **Cache expensive computations** tied to specific issues

### Key Features

- **Automatic scoping** - Metadata is automatically tied to the current repository and issue/PR
- **PostgreSQL storage** - Durable, reliable database storage
- **Simple API** - Get, set, delete, and list operations
- **Upsert semantics** - Set creates or updates automatically
- **Type-safe** - Full C# strong typing with IntelliSense

## Quick Start

### 1. Inject MetadataService

```text
using ProbotSharp.Application.Services;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Context;

[EventHandler("issues", "edited")]
public class EditCountTracker : IEventHandler
{
    private readonly MetadataService _metadata;

    public EditCountTracker(MetadataService metadata)
    {
        _metadata = metadata;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct = default)
    {
        // Get current count (returns null if not found)
        var currentCount = await _metadata.GetAsync("edit_count", ct);
        var count = int.TryParse(currentCount, out var c) ? c : 0;

        // Increment and save
        await _metadata.SetAsync("edit_count", (count + 1).ToString(), ct);

        context.Logger.LogInformation(
            "Issue edit count: {Count}",
            count + 1);
    }
}
```

### 2. Read Metadata Later

```text
[EventHandler("issues", "closed")]
public class EditCountReporter : IEventHandler
{
    private readonly MetadataService _metadata;

    public EditCountReporter(MetadataService metadata)
    {
        _metadata = metadata;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct = default)
    {
        var editCount = await _metadata.GetAsync("edit_count", ct);

        if (int.TryParse(editCount, out var count))
        {
            var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>() ?? 0;

            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber,
                $"This issue had **{count}** edit(s) before it was closed.");
        }
    }
}
```

**Usage:** Edit an issue a few times, then close it. The bot will post a summary comment!

## Storage Architecture

### Database Schema

Metadata is stored in PostgreSQL:

```sql
CREATE TABLE probot.issue_metadata (
    id BIGSERIAL PRIMARY KEY,
    repository_owner VARCHAR(255) NOT NULL,
    repository_name VARCHAR(255) NOT NULL,
    issue_number INT NOT NULL,
    key VARCHAR(255) NOT NULL,
    value TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_issue_metadata UNIQUE (repository_owner, repository_name, issue_number, key)
);

CREATE INDEX idx_issue_metadata_lookup
    ON probot.issue_metadata (repository_owner, repository_name, issue_number);
```

### Scoping

Metadata is automatically scoped to:

1. **Repository** - `owner/repo` from the current event context
2. **Issue/PR** - Issue or pull request number from the payload

This means:
- Each issue has its own isolated metadata store
- Different repositories don't interfere with each other
- Metadata persists across all events for that specific issue/PR
- Pull requests and issues are separate (PR #1 and Issue #1 have different metadata)

### Architecture Layers

```
┌─────────────────┐
│  Event Handler  │
└────────┬────────┘
         │ injects
         ▼
┌─────────────────┐
│ MetadataService │  ← Fluent API wrapper
└────────┬────────┘
         │ uses
         ▼
┌─────────────────┐
│ IMetadataPort   │  ← Port interface (abstraction)
└────────┬────────┘
         │ implemented by
         ▼
┌──────────────────────┐
│ PostgresMetadataAdapter │  ← Database adapter
└────────┬─────────────┘
         │ uses
         ▼
┌─────────────────┐
│ ProbotDbContext │  ← Entity Framework Core
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   PostgreSQL    │  ← Actual storage
└─────────────────┘
```

## API Reference

### MetadataService

The `MetadataService` provides a fluent API for metadata operations:

```text
public class MetadataService
{
    // Get a value (returns null if not found)
    Task<string?> GetAsync(string key, CancellationToken ct = default);

    // Set a value (creates or updates)
    Task SetAsync(string key, string value, CancellationToken ct = default);

    // Check if exists
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    // Delete a value
    Task DeleteAsync(string key, CancellationToken ct = default);

    // Get all metadata for this issue/PR
    Task<IDictionary<string, string>> GetAllAsync(CancellationToken ct = default);
}
```

### IMetadataPort

For more advanced scenarios, you can inject `IMetadataPort` directly:

```text
public interface IMetadataPort
{
    Task<string?> GetAsync(string owner, string repo, int issueNumber, string key, CancellationToken ct);
    Task SetAsync(string owner, string repo, int issueNumber, string key, string value, CancellationToken ct);
    Task<bool> ExistsAsync(string owner, string repo, int issueNumber, string key, CancellationToken ct);
    Task DeleteAsync(string owner, string repo, int issueNumber, string key, CancellationToken ct);
    Task<IDictionary<string, string>> GetAllAsync(string owner, string repo, int issueNumber, CancellationToken ct);
}
```

## CRUD Operations

### Create/Update (Set)

```text
await _metadata.SetAsync("status", "pending", ct);
await _metadata.SetAsync("assigned_to", "alice", ct);
await _metadata.SetAsync("priority", "high", ct);
```

**Upsert Behavior:**
- If key doesn't exist → Creates new entry with `created_at` timestamp
- If key exists → Updates value and `updated_at` timestamp

### Read (Get)

```text
string? status = await _metadata.GetAsync("status", ct);

if (status == null)
{
    // Key doesn't exist - use default
    status = "unknown";
}
```

**Always check for null** - `GetAsync` returns `null` if the key doesn't exist.

### Check Existence (Exists)

```text
bool hasStatus = await _metadata.ExistsAsync("status", ct);

if (!hasStatus)
{
    await _metadata.SetAsync("status", "pending", ct);
}
```

### Delete

```text
await _metadata.DeleteAsync("status", ct);

// Verify deletion
bool stillExists = await _metadata.ExistsAsync("status", ct); // false
```

### List All (GetAll)

```text
var allMetadata = await _metadata.GetAllAsync(ct);

foreach (var kvp in allMetadata)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
```

Returns a dictionary of all metadata key-value pairs for the current issue/PR.

## Configuration

### PostgreSQL Connection

Configure your database connection in `appsettings.json`:

```json
{
  "ProbotSharp": {
    "Persistence": {
      "Provider": "postgres",
      "ConnectionString": "Host=localhost;Database=probotsharp;Username=probotsharp;Password=yourpassword"
    }
  }
}
```

Or via environment variable:

```bash
export PROBOTSHARP_STORAGE_CONNECTION="Host=localhost;Database=probotsharp;Username=probotsharp;Password=yourpassword"
```

### Database Migrations

Run EF Core migrations to create the schema:

```bash
cd src/ProbotSharp.Infrastructure
dotnet ef database update
```

This creates the `probot.issue_metadata` table and indexes.

## Migration Patterns

### Migrating from Node.js probot-metadata

Node.js probot-metadata stores metadata in hidden HTML comments. To migrate:

**1. Export data from Node.js app:**

```javascript
// Node.js export script
app.on('issues.opened', async (context) => {
  const meta = await metadata(context);
  const data = await meta.get();
  console.log(JSON.stringify({
    owner: context.repo().owner,
    repo: context.repo().repo,
    issue: context.issue().number,
    metadata: data
  }));
});
```

**2. Import into ProbotSharp:**

```text
[EventHandler("issues", "opened")]
public class MetadataImporter : IEventHandler
{
    private readonly MetadataService _metadata;

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        // Read your exported JSON
        var exportedData = await ReadExportedData(context);

        // Import into metadata
        foreach (var kvp in exportedData)
        {
            await _metadata.SetAsync(kvp.Key, kvp.Value, ct);
        }
    }
}
```

### Migrating Between Storage Backends

To migrate from in-memory to PostgreSQL:

```text
public async Task MigrateMetadata(
    IMetadataPort sourcePort,
    IMetadataPort targetPort,
    string owner,
    string repo,
    int issueNumber,
    CancellationToken ct)
{
    var allMetadata = await sourcePort.GetAllAsync(owner, repo, issueNumber, ct);

    foreach (var kvp in allMetadata)
    {
        await targetPort.SetAsync(owner, repo, issueNumber, kvp.Key, kvp.Value, ct);
    }
}
```

## Advanced Usage

### Storing Complex Data (JSON)

Metadata values are strings, but you can serialize objects:

```text
using System.Text.Json;

public class ReviewHistory
{
    public List<string> Reviewers { get; set; }
    public int ReviewRound { get; set; }
    public DateTime LastReviewDate { get; set; }
}

// Store
var history = new ReviewHistory
{
    Reviewers = new List<string> { "alice", "bob" },
    ReviewRound = 2,
    LastReviewDate = DateTime.UtcNow
};

await _metadata.SetAsync("review_history", JsonSerializer.Serialize(history), ct);

// Retrieve
var json = await _metadata.GetAsync("review_history", ct);
if (json != null)
{
    var history = JsonSerializer.Deserialize<ReviewHistory>(json);
}
```

### Typed Metadata Helpers

Create typed wrappers for better ergonomics:

```text
public class TypedMetadataService
{
    private readonly MetadataService _metadata;

    public TypedMetadataService(MetadataService metadata)
    {
        _metadata = metadata;
    }

    public async Task<int> GetCountAsync(string key, CancellationToken ct)
    {
        var value = await _metadata.GetAsync(key, ct);
        return int.TryParse(value, out var count) ? count : 0;
    }

    public async Task SetCountAsync(string key, int value, CancellationToken ct)
    {
        await _metadata.SetAsync(key, value.ToString(), ct);
    }

    public async Task IncrementAsync(string key, CancellationToken ct)
    {
        var current = await GetCountAsync(key, ct);
        await SetCountAsync(key, current + 1, ct);
    }
}

// Usage
await typedMetadata.IncrementAsync("edit_count", ct);
var count = await typedMetadata.GetCountAsync("edit_count", ct);
```

### State Machines

Implement workflow states with metadata:

```text
public enum ReviewState
{
    Pending,
    InReview,
    Approved,
    ChangesRequested,
    Merged
}

[EventHandler("pull_request", "opened")]
public class ReviewStateMachine : IEventHandler
{
    private readonly MetadataService _metadata;

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        await _metadata.SetAsync("review_state", ReviewState.Pending.ToString(), ct);
    }
}

[EventHandler("pull_request_review", "submitted")]
public class ReviewStateTransition : IEventHandler
{
    private readonly MetadataService _metadata;

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        var reviewState = await _metadata.GetAsync("review_state", ct);
        var action = context.Payload["review"]?["state"]?.ToString();

        var newState = (reviewState, action) switch
        {
            ("Pending", "approved") => ReviewState.Approved,
            ("Pending", "changes_requested") => ReviewState.ChangesRequested,
            ("InReview", "approved") => ReviewState.Approved,
            _ => ReviewState.InReview
        };

        await _metadata.SetAsync("review_state", newState.ToString(), ct);
    }
}
```

### Batch Operations

For efficiency, use `GetAllAsync` and batch operations:

```text
public async Task UpdateMultipleMetadata(
    Dictionary<string, string> updates,
    CancellationToken ct)
{
    foreach (var kvp in updates)
    {
        await _metadata.SetAsync(kvp.Key, kvp.Value, ct);
    }
}

// Usage
await UpdateMultipleMetadata(new Dictionary<string, string>
{
    ["status"] = "approved",
    ["reviewer"] = "alice",
    ["approved_at"] = DateTime.UtcNow.ToString("o")
}, ct);
```

### Cleanup on Issue Deletion

Delete metadata when issues are deleted:

```text
[EventHandler("issues", "deleted")]
public class MetadataCleanup : IEventHandler
{
    private readonly MetadataService _metadata;

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        var allMetadata = await _metadata.GetAllAsync(ct);

        foreach (var key in allMetadata.Keys)
        {
            await _metadata.DeleteAsync(key, ct);
        }

        context.Logger.LogInformation("Cleaned up {Count} metadata entries", allMetadata.Count);
    }
}
```

## Best Practices

### 1. Always Check for Null

```text
var value = await _metadata.GetAsync("key", ct);
if (value == null)
{
    // Handle missing key - use default or set initial value
    value = "default";
}
```

### 2. Use Meaningful Key Names

```text
// Good
await _metadata.SetAsync("review_status", "approved", ct);
await _metadata.SetAsync("last_deployment_timestamp", timestamp, ct);

// Bad
await _metadata.SetAsync("rs", "a", ct);
await _metadata.SetAsync("ldt", timestamp, ct);
```

### 3. Handle Errors Gracefully

```text
try
{
    await _metadata.SetAsync("key", "value", ct);
}
catch (InvalidOperationException ex)
{
    // Context missing repository or issue info
    context.Logger.LogWarning(ex, "Cannot set metadata: {Message}", ex.Message);
}
```

### 4. Don't Store Secrets

Metadata is stored in plaintext. Never store:
- API tokens
- Passwords
- Private keys
- Sensitive user data

### 5. Consider Data Size

While `TEXT` columns can store large values, keep metadata small:
- **Good:** Status flags, counters, timestamps, user IDs
- **Bad:** Full file contents, large JSON objects, binary data

For large data, use blob storage and store references in metadata.

### 6. Use Transactions for Related Updates

When updating multiple related keys, use transactions:

```text
// In a real implementation, wrap in a transaction
await _metadata.SetAsync("status", "approved", ct);
await _metadata.SetAsync("approved_by", "alice", ct);
await _metadata.SetAsync("approved_at", DateTime.UtcNow.ToString("o"), ct);
```

### 7. Document Metadata Schema

Document what keys your app uses:

```text
/// <summary>
/// Metadata keys used by this app:
/// - "edit_count" (int): Number of times issue was edited
/// - "last_editor" (string): Username of last editor
/// - "review_status" (string): Current review status (pending/approved/rejected)
/// </summary>
public class MyApp : IProbotApp
{
    // ...
}
```

## Comparison with Node.js probot-metadata

| Feature | Node.js probot-metadata | ProbotSharp |
|---------|------------------------|--------------|
| Storage | Hidden HTML comment | PostgreSQL database |
| API | `await metadata(context).set('key', value)` | `await _metadata.SetAsync("key", value, ct)` |
| Scoping | Issue/PR | Repository + Issue/PR |
| Performance | Slow (GitHub API) | Fast (direct DB) |
| Reliability | Fails if comment deleted | Durable storage |
| Queries | Fetch all comments | SQL queries with indexes |
| Type safety | Plain objects | Strongly-typed C# |

**Node.js:**
```javascript
const meta = await metadata(context);
await meta.set('count', 5);
const count = await meta.get('count');
```

**ProbotSharp:**
```text
await _metadata.SetAsync("count", "5", ct);
var count = await _metadata.GetAsync("count", ct);
```

## See Also

- [Extensions.md](./Extensions.md) - Overview of all built-in extensions
- [SlashCommands.md](./SlashCommands.md) - Slash command guide
- [Architecture.md](./Architecture.md) - ProbotSharp architecture overview
- [../examples/MetadataBot/](../examples/MetadataBot/) - Complete working example
