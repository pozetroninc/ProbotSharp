# Attachments Bot Example

This example demonstrates how to use ProbotSharp's comment attachment feature to add structured, rich content to GitHub issue and pull request comments without modifying the original text.

## What It Does

The AttachmentsBot listens for issue comments containing `/build-status` and appends a formatted build status card to the comment. The card includes:

- A title with a clickable link
- Descriptive text
- Structured fields (duration, tests, coverage, branch)

## Key Features

- **Non-invasive**: Attachments are appended to comments, preserving the original user text
- **Idempotent**: Re-running the bot replaces the attachment instead of duplicating it
- **Markdown-based**: Renders cleanly in GitHub's comment UI
- **Structured data**: Fields provide consistent formatting for key-value pairs

## Usage

1. Install the bot on your GitHub repository
2. Comment on any issue with `/build-status`
3. The bot will append a build status card to your comment

## Rendered Output

When you comment with `/build-status`, the bot will append an attachment that looks like this:

```
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

Which GitHub renders as:

---

### [Build Status](https://ci.example.com/builds/123)

Latest build completed successfully

**Duration**: 2m 34s
**Tests**: 142 passed
**Coverage**: 87%
**Branch**: main

---

## Implementation Details

### CommentAttachmentService

The `CommentAttachmentService` is injected into event handlers and provides methods to add attachments:

```csharp
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
            TitleLink = "https://ci.example.com/builds/123",
            Text = "Latest build completed successfully",
            Color = "green",
            Fields = new List<AttachmentField>
            {
                new() { Title = "Duration", Value = "2m 34s", Short = true },
                new() { Title = "Tests", Value = "142 passed", Short = true },
            }
        }, ct);
    }
}
```

### Attachment Model

Attachments support the following properties:

- `Title`: The heading text
- `TitleLink`: Optional URL to make the title clickable
- `Text`: Main content text
- `Color`: Semantic color (for documentation; not rendered in Markdown)
- `Fields`: List of key-value pairs

### Multiple Attachments

You can add multiple attachments at once:

```text
await _attachments.AddAsync(new[]
{
    new CommentAttachment { Title = "Build Status", /* ... */ },
    new CommentAttachment { Title = "Test Results", /* ... */ },
}, ct);
```

### Idempotent Updates

The attachment service uses an HTML marker (`<!-- probot-sharp-attachments -->`) to identify the attachment section. When you call `AddAsync` again on the same comment, it replaces the existing attachments instead of creating duplicates.

## Real-World Use Cases

- **CI/CD Status**: Display build, test, and deployment results
- **Code Review**: Show analysis results (linting, coverage, security scans)
- **Project Management**: Display task status, assigned reviewers, or sprint info
- **Notifications**: Provide structured alerts or reminders
- **Documentation**: Link to relevant docs or wikis based on issue content

## Configuration

This example uses the standard ProbotSharp configuration. The `CommentAttachmentService` is automatically registered in the dependency injection container and available to all event handlers.

## Event Handling

The bot listens to the `issue_comment.created` event:

```text
[EventHandler("issue_comment", "created")]
public class BuildStatusAttachment : IEventHandler
{
    // ...
}
```

This means it will trigger whenever a new comment is posted on an issue or pull request.

## Best Practices

1. **Check for bot loops**: Always verify the sender isn't a bot to avoid infinite loops
2. **Validate context**: Ensure the payload contains the expected comment data
3. **Handle errors gracefully**: Log failures but don't crash on attachment errors
4. **Use semantic colors**: While not rendered in Markdown, colors document intent
5. **Keep fields concise**: Short, scannable key-value pairs work best

## Extending This Example

You could extend this bot to:

- Fetch real build status from a CI service API
- Support different commands (`/deploy-status`, `/test-summary`, etc.)
- Customize attachments based on repository or issue labels
- Add reactions to indicate processing status
- Only show attachments to users with certain permissions
