# Extensions Bot

A comprehensive example demonstrating all three built-in ProbotSharp extensions working together:
- **Slash Commands** - Interactive commands from comments
- **Metadata Storage** - Persistent key-value data
- **Comment Attachments** - Rich structured content

## What This Bot Does

ExtensionsBot showcases how the three extensions complement each other to build powerful GitHub automation:

1. **Tracks activity** - Monitors edits, comments, and label changes using metadata
2. **Responds to commands** - Handles slash commands like `/help`, `/status`, `/label`, and `/track`
3. **Displays rich status** - Shows tracked metrics in formatted attachment cards
4. **Generates summaries** - Posts activity summaries when issues close

## Features

### Slash Commands

- `/help` - Display available commands and usage
- `/status` - Show issue status with all tracked metrics
- `/label <labels>` - Add labels and track label changes
- `/track <metric> <value>` - Track custom metrics

### Activity Tracking

- Automatically counts issue edits
- Tracks comment creation
- Monitors label changes
- Records last activity timestamp

### Rich Attachments

- Status cards with all tracked metrics
- Activity summaries on issue close
- Formatted field displays

## Quick Start

### 1. Build the Project

```bash
# From the ProbotSharp root directory
dotnet build examples/ExtensionsBot
```

### 2. Configure Persistence

ExtensionsBot requires PostgreSQL for metadata storage. Configure in `appsettings.json`:

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

### 3. Run Database Migrations

```bash
cd src/ProbotSharp.Infrastructure
dotnet ef database update
```

### 4. Run the Bot

```bash
# From the ProbotSharp root
dotnet run --project src/ProbotSharp.Bootstrap.Api
```

### 5. Install on GitHub

1. Create a GitHub App at https://github.com/settings/apps/new
2. Configure webhook URL: `http://localhost:8080/webhooks`
3. Grant permissions:
   - Issues: Read & Write
   - Pull Requests: Read & Write
4. Subscribe to events:
   - Issues
   - Issue comments
   - Pull requests
5. Install the app on a test repository

## Usage Examples

### Example 1: Track Issue Progress

**Comment on an issue:**
```
/track progress 25%
```

**Bot response:**
```
✅ Tracked progress: 25%

Use /status to see all tracked metrics.
```

**Check status:**
```
/status
```

**Bot adds attachment:**
```markdown
---

### Issue #1 Status

Currently tracking 1 metric(s)

PROGRESS: 25%

---
```

### Example 2: Label and Track Changes

**Comment:**
```
/label bug, priority:high
```

**Bot response:**
```
✅ Added labels: bug, priority:high
```

**Behind the scenes, bot also:**
- Increments `label_changes` counter in metadata
- Records `last_labeler` username

### Example 3: Monitor Activity

**Edit the issue title**
→ Bot increments `edit_count` metadata

**Add a comment**
→ Bot increments `comment_count` metadata

**Check status:**
```
/status
```

**Bot shows:**
```markdown
---

### Issue #1 Status

Currently tracking 5 metric(s)

EDIT COUNT: 2
COMMENT COUNT: 3
LABEL CHANGES: 1
LAST LABELER: alice
PROGRESS: 25%

---
```

### Example 4: Close Issue Summary

**Close the issue**

**Bot automatically posts:**
```markdown
---

### Issue Summary

This issue had 5 tracked metric(s) before being closed.

Total Edits: 2
Total Comments: 3
Label Changes: 1
Last Activity: 2025-10-05 23:15:32 UTC
PROGRESS: 25%

---
```

## Code Structure

### Entry Point

- **ExtensionsApp.cs** - Main app configuration and registration

### Slash Command Handlers

- **HelpCommand.cs** - Display help information
- **StatusCommand.cs** - Show issue status with metadata + attachments
- **LabelCommand.cs** - Add labels and track changes
- **TrackCommand.cs** - Track custom metrics in metadata

### Event Handlers

- **ActivityTracker.cs** - Monitor edits, comments, and labels
- **SummaryGenerator.cs** - Generate summaries on issue close

## How the Extensions Work Together

### Slash Commands → Metadata

Commands like `/label` and `/track` store data in metadata:

```text
// In LabelCommand.cs
await _metadata.SetAsync("label_changes", (count + 1).ToString(), ct);
await _metadata.SetAsync("last_labeler", sender, ct);
```

### Metadata → Attachments

The `/status` command reads metadata and displays it in an attachment:

```text
// In StatusCommand.cs
var allMetadata = await _metadata.GetAllAsync(ct);

var fields = allMetadata.Select(kvp => new AttachmentField
{
    Title = FormatKey(kvp.Key),
    Value = kvp.Value,
    Short = true
}).ToList();

await _attachments.AddAsync(new CommentAttachment
{
    Title = $"Issue #{issueNumber} Status",
    Text = $"Currently tracking {allMetadata.Count} metric(s)",
    Fields = fields
}, ct);
```

### Events → Metadata → Attachments

Issue events update metadata, which is displayed in attachments on close:

```text
// In ActivityTracker.cs (on issue edit)
await _metadata.SetAsync("edit_count", (count + 1).ToString(), ct);

// Later, in SummaryGenerator.cs (on issue close)
var allMetadata = await _metadata.GetAllAsync(ct);
await _attachments.AddAsync(new CommentAttachment { ... }, ct);
```

## Extension Integration Patterns

### Pattern 1: Command + Metadata

Track state from slash commands:

```text
[SlashCommandHandler("track")]
public class TrackCommand : ISlashCommandHandler
{
    private readonly MetadataService _metadata;

    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        await _metadata.SetAsync(metricName, metricValue, ct);
    }
}
```

### Pattern 2: Metadata + Attachments

Display metadata in rich format:

```text
var allMetadata = await _metadata.GetAllAsync(ct);

await _attachments.AddAsync(new CommentAttachment
{
    Title = "Status",
    Fields = allMetadata.Select(kvp => new AttachmentField
    {
        Title = kvp.Key,
        Value = kvp.Value
    }).ToList()
}, ct);
```

### Pattern 3: Event + Metadata

Track activity automatically:

```text
[EventHandler("issues", "edited")]
public class ActivityTracker : IEventHandler
{
    private readonly MetadataService _metadata;

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        var count = await GetCountAsync("edit_count", ct);
        await _metadata.SetAsync("edit_count", (count + 1).ToString(), ct);
    }
}
```

### Pattern 4: All Three Together

Commands trigger metadata updates displayed via attachments:

```text
[SlashCommandHandler("status")]
public class StatusCommand : ISlashCommandHandler
{
    private readonly MetadataService _metadata;
    private readonly CommentAttachmentService _attachments;

    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        var metadata = await _metadata.GetAllAsync(ct);

        await _attachments.AddAsync(new CommentAttachment
        {
            Title = "Status",
            Fields = BuildFieldsFromMetadata(metadata)
        }, ct);
    }
}
```

## Best Practices Demonstrated

### 1. Ignore Bot Events

```text
if (context.IsBot())
{
    return; // Prevent infinite loops
}
```

### 2. Validate Input

```text
if (string.IsNullOrWhiteSpace(command.Arguments))
{
    await PostUsageMessage(context);
    return;
}
```

### 3. Provide User Feedback

```text
await context.GitHub.Issue.Comment.Create(
    context.Repository.Owner,
    context.Repository.Name,
    issueNumber,
    "✅ Tracked progress: 25%");
```

### 4. Handle Errors Gracefully

```text
try
{
    await _metadata.SetAsync("key", "value", ct);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to set metadata");
    await PostErrorMessage(context);
}
```

### 5. Log Important Actions

```text
_logger.LogInformation(
    "Tracked metric '{Metric}' = '{Value}' for issue #{IssueNumber}",
    metricName,
    metricValue,
    issueNumber);
```

## Customization Ideas

### Track More Metrics

```text
[SlashCommandHandler("track-review")]
public class TrackReviewCommand : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        await _metadata.SetAsync("review_status", command.Arguments, ct);
        await _metadata.SetAsync("reviewed_by", context.Sender.Login, ct);
        await _metadata.SetAsync("reviewed_at", DateTime.UtcNow.ToString("o"), ct);
    }
}
```

### Add More Commands

```text
[SlashCommandHandler("assign")]
public class AssignCommand : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        // Assign users and track in metadata
    }
}
```

### Enhance Attachments

```text
await _attachments.AddAsync(new CommentAttachment
{
    Title = "Detailed Status",
    TitleLink = "https://dashboard.example.com/issue/123",
    Text = "View full details in the dashboard",
    Fields = fields
}, ct);
```

## Testing

### Manual Testing

1. **Install the bot** on a test repository
2. **Create an issue**
3. **Try commands:**
   ```
   /help
   /track progress 25%
   /status
   /label test
   ```
4. **Edit the issue** a few times
5. **Add comments**
6. **Close the issue** to see summary

### Expected Output

After following the steps above, you should see:
- Help message from `/help`
- Confirmation from `/track`
- Status attachment from `/status` showing all metrics
- Label confirmation from `/label`
- Summary attachment when issue closes

## Troubleshooting

### Database Connection Issues

**Error:** `Cannot connect to PostgreSQL`

**Solution:**
1. Verify PostgreSQL is running: `pg_isready -h localhost`
2. Check connection string in configuration
3. Ensure database exists: `createdb probotsharp`
4. Run migrations: `dotnet ef database update`

### Metadata Not Persisting

**Error:** Metadata disappears after restart

**Solution:**
- Verify `Persistence.Provider` is set to `"postgres"`, not in-memory
- Check database connection string
- Confirm migrations were applied

### Commands Not Responding

**Error:** Bot doesn't respond to slash commands

**Solution:**
1. Check logs for errors
2. Verify slash command handlers are registered
3. Ensure `AddSlashCommands` is called in `ConfigureAsync`
4. Check that bot has permissions to post comments

## Related Documentation

- [Extensions Overview](../../docs/Extensions.md) - All three extensions explained
- [Slash Commands Guide](../../docs/SlashCommands.md) - Detailed slash command documentation
- [Metadata Storage Guide](../../docs/Metadata.md) - Metadata storage documentation
- [Attachments Guide](../../docs/Attachments.md) - Comment attachments documentation
- [Best Practices](../../docs/BestPractices.md) - General ProbotSharp best practices

## License

MIT - See [LICENSE](../../LICENSE)
