# Extensions

ProbotSharp includes three powerful built-in extensions that make it easy to build interactive GitHub Apps without writing boilerplate code. These extensions provide the same functionality as the popular Node.js Probot extensions, but with strong typing, better performance, and cleaner integration with C#'s dependency injection.

## Table of Contents

- [Overview](#overview)
- [Slash Commands](#slash-commands)
- [Metadata Storage](#metadata-storage)
- [Comment Attachments](#comment-attachments)
- [Creating Custom Extensions](#creating-custom-extensions)
- [Comparison with Node.js Probot](#comparison-with-nodejs-probot)

## Overview

ProbotSharp provides three batteries-included extensions:

| Extension | Purpose | Node.js Equivalent |
|-----------|---------|-------------------|
| **Slash Commands** | Parse and route `/command` syntax from comments | [probot-commands](https://github.com/probot/commands) |
| **Metadata Storage** | Persist key-value data scoped to issues/PRs | [probot-metadata](https://github.com/probot/metadata) |
| **Comment Attachments** | Add structured content to comments | [probot-attachments](https://github.com/probot/attachments) |

These extensions are:
- **Already integrated** - No separate packages to install
- **Type-safe** - Full C# strong typing with IntelliSense
- **Well-tested** - Comprehensive test coverage
- **Production-ready** - Used in real-world applications

## Slash Commands

Slash commands allow users to interact with your app by typing `/command arguments` in issue or pull request comments.

### Quick Start

**1. Create a command handler:**

```text
using ProbotSharp.Application.Abstractions.Commands;
using ProbotSharp.Domain.Commands;
using ProbotSharp.Domain.Context;

[SlashCommandHandler("label")]
public class LabelCommand : ISlashCommandHandler
{
    public async Task HandleAsync(
        ProbotSharpContext context,
        SlashCommand command,
        CancellationToken cancellationToken = default)
    {
        var labels = command.Arguments.Split(',').Select(l => l.Trim()).ToArray();
        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>() ?? 0;

        await context.GitHub.Issue.Labels.AddToIssue(
            context.Repository.Owner,
            context.Repository.Name,
            issueNumber,
            labels);
    }
}
```

**2. Register the command in your app:**

```text
using ProbotSharp.Application.Extensions;

public class MyApp : IProbotApp
{
    public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
    {
        // Auto-discover all slash command handlers in this assembly
        services.AddSlashCommands(typeof(MyApp).Assembly);

        return Task.CompletedTask;
    }
}
```

**3. Use the command:**

Users can now comment on issues with:
```
/label bug, enhancement
```

### How It Works

1. User posts a comment containing `/label bug, enhancement`
2. `SlashCommandEventHandler` intercepts the `issue_comment.created` event
3. `SlashCommandParser` extracts the command name and arguments
4. `SlashCommandRouter` finds registered handlers for "label"
5. Your `LabelCommand.HandleAsync()` method executes
6. The GitHub API adds the labels

### Features

- **Multiple commands per comment** - Users can type multiple slash commands (one per line)
- **Case-insensitive** - `/Label` and `/label` both work
- **Multiple handlers per command** - Register multiple handlers for the same command name
- **Automatic discovery** - Use `[SlashCommandHandler]` attribute for auto-registration
- **Works everywhere** - Issue comments and PR review comments

### Command Syntax

Commands follow this pattern:
```
/command-name arguments go here
```

- Command names: Alphanumeric with hyphens and underscores (`/label`, `/deploy-prod`, `/auto_merge`)
- Arguments: Everything after the first space (optional)
- Multiple commands: One per line

**Examples:**
```
/label bug
/assign @octocat
/priority high
/deploy staging --force
```

For detailed slash command documentation, see [SlashCommands.md](./SlashCommands.md).

## Metadata Storage

Metadata storage provides persistent key-value storage scoped to specific GitHub issues or pull requests. Perfect for tracking state across multiple webhook events.

### Quick Start

**1. Inject `MetadataService` into your event handler:**

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
        // Get current count
        var currentCount = await _metadata.GetAsync("edit_count", ct);
        var count = int.TryParse(currentCount, out var c) ? c : 0;

        // Increment and save
        await _metadata.SetAsync("edit_count", (count + 1).ToString(), ct);
    }
}
```

**2. Read the metadata later:**

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

### How It Works

Metadata is:
- **Scoped** - Automatically tied to the current repository and issue/PR number
- **Persistent** - Stored in PostgreSQL database
- **Fast** - Direct database queries, no GitHub API calls
- **Reliable** - Survives comment deletions (unlike Node.js probot-metadata)

### API

The `MetadataService` provides a fluent API:

```text
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

### Storage

Metadata is stored in PostgreSQL:

```sql
-- Table: probot.issue_metadata
CREATE TABLE issue_metadata (
    id BIGSERIAL PRIMARY KEY,
    repository_owner VARCHAR(255) NOT NULL,
    repository_name VARCHAR(255) NOT NULL,
    issue_number INT NOT NULL,
    key VARCHAR(255) NOT NULL,
    value TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (repository_owner, repository_name, issue_number, key)
);
```

The `SetAsync()` method uses upsert semantics:
- If key doesn't exist → Creates new entry
- If key exists → Updates value and timestamp

For detailed metadata documentation, see [Metadata.md](./Metadata.md).

## Comment Attachments

Comment attachments allow you to append rich, structured content to GitHub comments without modifying the original user text.

### Quick Start

**1. Inject `CommentAttachmentService` into your event handler:**

```text
using ProbotSharp.Application.Services;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Attachments;
using ProbotSharp.Domain.Context;

[EventHandler("issue_comment", "created")]
public class BuildStatusAttachment : IEventHandler
{
    private readonly CommentAttachmentService _attachments;

    public BuildStatusAttachment(CommentAttachmentService attachments)
    {
        _attachments = attachments;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        var comment = context.Payload["comment"]?["body"]?.ToString();

        if (comment?.Contains("/build-status") == true)
        {
            await _attachments.AddAsync(new CommentAttachment
            {
                Title = "Build Status",
                TitleLink = "https://ci.example.com/builds/123",
                Text = "Latest build completed successfully",
                Color = "green",
                Fields = new List<AttachmentField>
                {
                    new() { Title = "Duration", Value = "2m 34s", Short = true },
                    new() { Title = "Tests", Value = "142 passed", Short = true },
                    new() { Title = "Coverage", Value = "87%", Short = true },
                    new() { Title = "Branch", Value = "main", Short = true },
                }
            }, ct);
        }
    }
}
```

### Rendered Output

The attachment is appended to the comment as markdown:

```markdown
<!-- probot-sharp-attachments -->

---

### [Build Status](https://ci.example.com/builds/123)

Latest build completed successfully

**Duration**: 2m 34s
**Tests**: 142 passed
**Coverage**: 87%
**Branch**: main

---
```

### Features

- **Non-invasive** - Original comment text is preserved
- **Idempotent** - Re-running replaces attachments instead of duplicating
- **Multiple attachments** - Add multiple cards at once
- **Markdown-based** - Renders cleanly in GitHub's UI

### Attachment Model

```text
public class CommentAttachment
{
    public string? Title { get; set; }           // Heading text
    public string? TitleLink { get; set; }       // Makes title clickable
    public string? Text { get; set; }            // Main content
    public string? Color { get; set; }           // Semantic color (not rendered)
    public List<AttachmentField>? Fields { get; set; }  // Key-value pairs
}

public class AttachmentField
{
    public string Title { get; set; }   // Field name
    public string Value { get; set; }   // Field value
    public bool Short { get; set; }     // Display inline (not used in markdown)
}
```

For detailed attachment documentation, see [Attachments.md](./Attachments.md).

## Creating Custom Extensions

You can create your own extensions by following the same patterns used in the built-in extensions.

### Extension Architecture

Extensions in ProbotSharp typically consist of:

1. **Service class** - Core logic (e.g., `CommentAttachmentService`)
2. **Domain models** - Data structures (e.g., `CommentAttachment`)
3. **Event handler** - Automatic processing (e.g., `SlashCommandEventHandler`)
4. **Registration method** - DI setup (e.g., `AddSlashCommands()`)

### Example: Reaction Extension

Let's create a simple extension that adds reactions to comments:

**1. Create the service:**

```text
namespace ProbotSharp.Application.Services;

public class ReactionService
{
    private readonly ProbotSharpContext _context;

    public ReactionService(ProbotSharpContext context)
    {
        _context = context;
    }

    public async Task AddAsync(string reaction, CancellationToken ct = default)
    {
        var commentId = _context.Payload["comment"]?["id"]?.ToObject<long>();
        if (!commentId.HasValue || _context.Repository == null)
        {
            throw new InvalidOperationException("Reactions require a comment context");
        }

        await _context.GitHub.Reaction.CommitComment.Create(
            _context.Repository.Owner,
            _context.Repository.Name,
            (int)commentId.Value,
            new NewReaction(ReactionType.Heart));
    }
}
```

**2. Register in DI:**

```text
public static class ReactionServiceExtensions
{
    public static IServiceCollection AddReactions(this IServiceCollection services)
    {
        services.AddScoped<ReactionService>();
        return services;
    }
}
```

**3. Use in your app:**

```text
[EventHandler("issue_comment", "created")]
public class ThankYouReaction : IEventHandler
{
    private readonly ReactionService _reactions;

    public ThankYouReaction(ReactionService reactions)
    {
        _reactions = reactions;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        var comment = context.Payload["comment"]?["body"]?.ToString();

        if (comment?.Contains("thank you", StringComparison.OrdinalIgnoreCase) == true)
        {
            await _reactions.AddAsync("heart", ct);
        }
    }
}
```

### Extension Best Practices

1. **Inject `ProbotSharpContext`** - Access to event payload and GitHub client
2. **Use scoped lifetime** - Extensions should be scoped to webhook events
3. **Provide extension methods** - `AddXxx()` methods for easy registration
4. **Handle errors gracefully** - Don't crash the entire webhook handler
5. **Document extensively** - Users need to understand the API
6. **Add comprehensive tests** - Unit tests for service logic, integration tests for GitHub API calls

## Comparison with Node.js Probot

### Slash Commands

| Feature | Node.js probot-commands | ProbotSharp |
|---------|------------------------|--------------|
| Parsing | RegEx-based | `SlashCommandParser` |
| Registration | `commands(app, "name", handler)` | `[SlashCommandHandler("name")]` |
| Discovery | Manual | Automatic via attributes |
| Multiple handlers | Not supported | Supported |
| Type safety | TypeScript types | Full C# typing |

**Node.js:**
```javascript
import commands from 'probot-commands';

export default (app) => {
  commands(app, 'label', (context, command) => {
    const labels = command.arguments.split(/, */);
    return context.octokit.issues.addLabels(context.issue({ labels }));
  });
};
```

**ProbotSharp:**
```text
[SlashCommandHandler("label")]
public class LabelCommand : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        var labels = command.Arguments.Split(',').Select(l => l.Trim()).ToArray();
        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>() ?? 0;

        await context.GitHub.Issue.Labels.AddToIssue(
            context.Repository.Owner,
            context.Repository.Name,
            issueNumber,
            labels);
    }
}
```

### Metadata Storage

| Feature | Node.js probot-metadata | ProbotSharp |
|---------|------------------------|--------------|
| Storage | Hidden HTML comment | PostgreSQL database |
| API | `await metadata(context).set('key', value)` | `await _metadata.SetAsync("key", value, ct)` |
| Scoping | Issue/PR | Repository + Issue/PR |
| Performance | Slow (GitHub API) | Fast (direct DB) |
| Reliability | Fails if comment deleted | Durable storage |

**Node.js:**
```javascript
import metadata from 'probot-metadata';

export default (app) => {
  app.on(['issues.edited', 'issue_comment.edited'], async (context) => {
    const kv = await metadata(context);
    await kv.set('edits', (await kv.get('edits')) || 1);
  });
};
```

**ProbotSharp:**
```text
[EventHandler("issues", "edited")]
[EventHandler("issue_comment", "edited")]
public class EditCountTracker : IEventHandler
{
    private readonly MetadataService _metadata;

    public EditCountTracker(MetadataService metadata)
    {
        _metadata = metadata;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        var currentCount = await _metadata.GetAsync("edit_count", ct);
        var count = int.TryParse(currentCount, out var c) ? c : 0;
        await _metadata.SetAsync("edit_count", (count + 1).ToString(), ct);
    }
}
```

### Comment Attachments

| Feature | Node.js probot-attachments | ProbotSharp |
|---------|----------------------------|--------------|
| API | `await attachments(context).add({ title: "..." })` | `await _attachments.AddAsync(new CommentAttachment { ... }, ct)` |
| Type safety | Plain objects | Strongly-typed models |
| Idempotent | Yes | Yes |
| Multiple attachments | Yes | Yes |

**Node.js:**
```javascript
import attachments from 'probot-attachments';

export default (app) => {
  app.on('issue_comment.created', (context) => {
    return attachments(context).add({
      title: 'Build Status',
      title_link: 'https://ci.example.com/builds/123',
    });
  });
};
```

**ProbotSharp:**
```text
[EventHandler("issue_comment", "created")]
public class BuildStatusAttachment : IEventHandler
{
    private readonly CommentAttachmentService _attachments;

    public BuildStatusAttachment(CommentAttachmentService attachments)
    {
        _attachments = attachments;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        await _attachments.AddAsync(new CommentAttachment
        {
            Title = "Build Status",
            TitleLink = "https://ci.example.com/builds/123"
        }, ct);
    }
}
```

## See Also

- [SlashCommands.md](./SlashCommands.md) - Detailed slash command guide
- [Metadata.md](./Metadata.md) - Metadata storage guide
- [Attachments.md](./Attachments.md) - Comment attachment guide
- [Architecture.md](./Architecture.md) - ProbotSharp architecture overview
- [BestPractices.md](./BestPractices.md) - Best practices for building GitHub Apps

## Examples

Complete working examples are available in the `examples/` directory:

- [`examples/SlashCommandsBot/`](../examples/SlashCommandsBot/) - Slash command examples
- [`examples/MetadataBot/`](../examples/MetadataBot/) - Metadata storage examples
- [`examples/AttachmentsBot/`](../examples/AttachmentsBot/) - Comment attachment examples
- [`examples/ExtensionsBot/`](../examples/ExtensionsBot/) - Combined example using all three extensions
