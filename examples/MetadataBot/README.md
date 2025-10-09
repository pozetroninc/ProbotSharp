# MetadataBot Example

This example demonstrates how to use **ProbotSharp's Metadata Storage** feature to persist key-value data scoped to specific GitHub issues or pull requests. The metadata survives across multiple webhook events, making it perfect for tracking state and counters.

## What This Bot Does

MetadataBot tracks the number of times an issue or its comments are edited, then posts a summary when the issue is closed:

1. **Tracks edits**: Increments a counter each time an issue or comment is edited
2. **Persists data**: Stores the count in metadata (PostgreSQL database)
3. **Reports summary**: When the issue closes, posts a comment with the total edit count

## Key Components

### 1. EditCountTracker (`EditCountTracker.cs`)

Handles `issues.edited` and `issue_comment.edited` events to track edit activity:

```text
[EventHandler("issues", "edited")]
[EventHandler("issue_comment", "edited")]
public class EditCountTracker : IEventHandler
{
    private readonly MetadataService _metadata;

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct = default)
    {
        // Get current count (or 0 if not set)
        var currentCountString = await _metadata.GetAsync("edit_count", ct);
        var currentCount = int.TryParse(currentCountString, out var count) ? count : 0;

        // Increment and save
        var newCount = currentCount + 1;
        await _metadata.SetAsync("edit_count", newCount.ToString(), ct);
    }
}
```

### 2. EditCountReporter (`EditCountReporter.cs`)

Handles `issues.closed` events to report the final edit count:

```text
[EventHandler("issues", "closed")]
public class EditCountReporter : IEventHandler
{
    private readonly MetadataService _metadata;

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct = default)
    {
        var editCount = await _metadata.GetAsync("edit_count", ct);

        if (!string.IsNullOrEmpty(editCount) && int.TryParse(editCount, out var count))
        {
            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber,
                $"This issue had **{count}** edit(s) before it was closed."
            );
        }
    }
}
```

### 3. MetadataApp (`MetadataApp.cs`)

Registers the event handlers with the Event Router:

```text
public class MetadataApp : IProbotApp
{
    public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
    {
        router.RegisterHandler("issues", "edited", typeof(EditCountTracker));
        router.RegisterHandler("issue_comment", "edited", typeof(EditCountTracker));
        router.RegisterHandler("issues", "closed", typeof(EditCountReporter));
        return Task.CompletedTask;
    }
}
```

## How Metadata Storage Works

### MetadataService API

The `MetadataService` provides a fluent API for metadata operations:

```text
// Injected via constructor
private readonly MetadataService _metadata;

// Get a value (returns null if not found)
string? value = await _metadata.GetAsync("my_key", ct);

// Set a value (creates or updates)
await _metadata.SetAsync("my_key", "my_value", ct);

// Check if exists
bool exists = await _metadata.ExistsAsync("my_key", ct);

// Delete a value
await _metadata.DeleteAsync("my_key", ct);

// Get all metadata for this issue/PR
IDictionary<string, string> all = await _metadata.GetAllAsync(ct);
```

### Scoping

Metadata is automatically scoped to:
- **Repository**: `owner/repo` from the event context
- **Issue/PR**: Issue or pull request number from the payload

This means:
- Each issue has its own isolated metadata store
- Different repositories don't interfere with each other
- Metadata persists across all events for that specific issue

### Storage

- **Database**: PostgreSQL (via Entity Framework Core)
- **Table**: `probot.issue_metadata`
- **Schema**:
  ```
  id (bigint, primary key)
  repository_owner (varchar(255))
  repository_name (varchar(255))
  issue_number (int)
  key (varchar(255))
  value (text)
  created_at (timestamptz)
  updated_at (timestamptz)

  UNIQUE INDEX: (repository_owner, repository_name, issue_number, key)
  ```

### Upsert Behavior

`SetAsync()` uses "upsert" semantics:
- If the key doesn't exist → Creates a new entry
- If the key exists → Updates the value and `updated_at` timestamp

## Running the Example

### Prerequisites

1. **PostgreSQL database** running and accessible
2. **Connection string** configured (see below)
3. **GitHub App** credentials

### Configuration

Set your database connection string in one of these ways:

**Option 1**: Environment variable
```bash
export PROBOTSHARP_STORAGE_CONNECTION="Host=localhost;Database=probotsharp;Username=probotsharp;Password=yourpassword"
```

**Option 2**: appsettings.json
```json
{
  "ConnectionStrings": {
    "ProbotSharp": "Host=localhost;Database=probotsharp;Username=probotsharp;Password=yourpassword"
  }
}
```

**Option 3**: ProbotSharp configuration
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

### Run Database Migrations

Apply the `issue_metadata` table schema:

```bash
cd src/ProbotSharp.Infrastructure
dotnet ef database update
```

This creates the `probot.issue_metadata` table in your PostgreSQL database.

### Install and Run

```bash
# From the ProbotSharp root directory
dotnet build examples/MetadataBot
dotnet run --project src/ProbotSharp.Bootstrap.Api
```

Your bot will now:
- Track edits on any issue in repositories where it's installed
- Post edit count summaries when issues close

## Testing

### Manual Testing

1. **Open an issue** in a repository where your app is installed
2. **Edit the issue** title or body a few times
3. **Edit comments** on the issue
4. **Close the issue**
5. **Check for the summary comment** showing the total edit count

### Example Output

When you close an issue that was edited 5 times:

```
This issue had **5** edit(s) before it was closed.
```

### Debugging

Check logs for metadata operations:

```
[Information] Edit count for issue in owner/repo: 1
[Information] Edit count for issue in owner/repo: 2
[Information] Edit count for issue in owner/repo: 3
[Information] Posted edit count summary on issue #42: 3 edits
```

## Advanced Usage

### Multiple Metadata Keys

Track different metrics for the same issue:

```text
await _metadata.SetAsync("edit_count", "5", ct);
await _metadata.SetAsync("last_editor", "octocat", ct);
await _metadata.SetAsync("first_edit_time", DateTime.UtcNow.ToString("o"), ct);

// Retrieve all at once
var allMetadata = await _metadata.GetAllAsync(ct);
// Returns: { "edit_count": "5", "last_editor": "octocat", "first_edit_time": "..." }
```

### Conditional Logic

Use metadata to implement state machines:

```text
var status = await _metadata.GetAsync("review_status", ct);

if (status == "approved")
{
    // Merge pull request
}
else if (status == "changes_requested")
{
    // Notify author
}
```

### Complex Data

Store JSON for structured data:

```text
var data = new
{
    Edits = 5,
    Editors = new[] { "alice", "bob" },
    Timestamp = DateTime.UtcNow
};

await _metadata.SetAsync("edit_history", JsonSerializer.Serialize(data), ct);

// Later...
var json = await _metadata.GetAsync("edit_history", ct);
var history = JsonSerializer.Deserialize<EditHistory>(json);
```

## Extension Methods (Alternative API)

You can also use the extension methods directly on `ProbotSharpContext`:

```text
using ProbotSharp.Application.Extensions;
using ProbotSharp.Application.Ports.Outbound;

public class MyHandler : IEventHandler
{
    private readonly IMetadataPort _port;

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        // Direct extension method calls
        var value = await context.GetMetadataAsync(_port, "my_key", ct);
        await context.SetMetadataAsync(_port, "my_key", "my_value", ct);
    }
}
```

The `MetadataService` is recommended for cleaner code, but extension methods provide more flexibility.

## Best Practices

### 1. Handle Missing Values

Always check for null when reading metadata:

```text
var value = await _metadata.GetAsync("key", ct);
if (value == null)
{
    // Key doesn't exist - use default
    value = "0";
}
```

### 2. Ignore Bot Events

Prevent infinite loops:

```text
if (context.IsBot())
{
    return;  // Don't process bot-generated events
}
```

### 3. Type Safety

Use typed helpers for non-string data:

```text
// Extension method for int metadata
async Task<int> GetCountAsync(string key, CancellationToken ct)
{
    var value = await _metadata.GetAsync(key, ct);
    return int.TryParse(value, out var count) ? count : 0;
}

// Usage
var count = await GetCountAsync("edit_count", ct);
```

### 4. Error Handling

Wrap metadata operations in try-catch:

```text
try
{
    await _metadata.SetAsync("key", "value", ct);
}
catch (InvalidOperationException ex)
{
    // Context missing repository or issue info
    _logger.LogWarning(ex, "Cannot set metadata: {Message}", ex.Message);
}
```

### 5. Cleanup

Delete metadata when no longer needed:

```text
[EventHandler("issues", "deleted")]
public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
{
    // Clean up all metadata for this issue
    var allMetadata = await _metadata.GetAllAsync(ct);
    foreach (var key in allMetadata.Keys)
    {
        await _metadata.DeleteAsync(key, ct);
    }
}
```

## Comparison with Node.js Probot

| Feature | Node.js probot-metadata | ProbotSharp |
|---------|------------------------|--------------|
| Storage | Hidden comment (HTML) | PostgreSQL database |
| API | `metadata(context).set()` | `MetadataService.SetAsync()` |
| Scoping | Issue/PR | Issue/PR + Repository |
| Persistence | Comment body | Database table |
| Performance | Slow (API calls) | Fast (database queries) |
| Reliability | Can fail if comment deleted | Durable database storage |

## Architecture

```
┌─────────────────┐
│  Event Handler  │
│ (EditCountBot)  │
└────────┬────────┘
         │ injects
         ▼
┌─────────────────┐
│ MetadataService │  ← Fluent API wrapper
└────────┬────────┘
         │ uses
         ▼
┌─────────────────┐
│ IMetadataPort   │  ← Abstraction
└────────┬────────┘
         │ implemented by
         ▼
┌──────────────────────┐
│ PostgresMetadataAdapter │  ← Database adapter
└────────┬─────────────┘
         │ uses
         ▼
┌─────────────────┐
│ ProbotDbContext │  ← Entity Framework
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   PostgreSQL    │  ← Actual storage
│ issue_metadata  │
└─────────────────┘
```

## Related Documentation

- [ProbotSharp Architecture](../../docs/Architecture.md)
- [Best Practices](../../docs/BestPractices.md)

## License

MIT - See [LICENSE](../../LICENSE)
