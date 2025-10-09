# Comment Attachments

Comment attachments allow you to append rich, structured content to GitHub issue and pull request comments without modifying the original user text. Perfect for adding build status cards, CI/CD results, code analysis summaries, and other structured information.

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Attachment Structure](#attachment-structure)
- [Rendering and Templates](#rendering-and-templates)
- [API Reference](#api-reference)
- [Idempotent Updates](#idempotent-updates)
- [Advanced Usage](#advanced-usage)
- [Best Practices](#best-practices)

## Overview

Comment attachments enable you to:

- **Add structured content** to comments (build status, test results, metrics)
- **Preserve user text** - Original comment remains unchanged
- **Update idempotently** - Re-running replaces attachments instead of duplicating
- **Render as Markdown** - Clean, native GitHub rendering
- **Provide rich information** - Titles, links, descriptions, and key-value fields

### Key Features

- **Non-invasive** - Original comment text is never modified
- **Idempotent** - Multiple runs replace rather than duplicate
- **Markdown-based** - Renders cleanly in GitHub's UI
- **Structured fields** - Key-value pairs for consistent formatting
- **Multiple attachments** - Add multiple cards at once
- **HTML marker** - Hidden marker ensures clean updates

## Quick Start

### 1. Inject CommentAttachmentService

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

### 2. Use the Command

User comments on an issue:

```
/build-status
```

### 3. Result

The bot appends this to the comment:

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

Which GitHub renders as:

---

### [Build Status](https://ci.example.com/builds/123)

Latest build completed successfully

**Duration**: 2m 34s
**Tests**: 142 passed
**Coverage**: 87%
**Branch**: main

---

## Attachment Structure

### CommentAttachment Model

```text
public class CommentAttachment
{
    // Heading text for the attachment
    public string? Title { get; set; }

    // Optional URL to make the title clickable
    public string? TitleLink { get; set; }

    // Main content/description
    public string? Text { get; set; }

    // Semantic color (for documentation; not rendered in Markdown)
    public string? Color { get; set; }

    // List of key-value pairs
    public List<AttachmentField>? Fields { get; set; }
}
```

### AttachmentField Model

```text
public class AttachmentField
{
    // Field name/label
    public string Title { get; set; }

    // Field value
    public string Value { get; set; }

    // Display hint (not used in Markdown rendering)
    public bool Short { get; set; }
}
```

### Property Details

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `Title` | string? | Heading text | "Build Status" |
| `TitleLink` | string? | Makes title clickable | "https://ci.example.com/builds/123" |
| `Text` | string? | Main descriptive text | "Latest build completed successfully" |
| `Color` | string? | Semantic color (not rendered) | "green", "red", "yellow" |
| `Fields` | List&lt;AttachmentField&gt;? | Key-value pairs | See examples below |

**Note:** The `Color` property is for semantic purposes and documentation. GitHub's Markdown renderer doesn't display colors, but the property is useful for logging and debugging.

## Rendering and Templates

### Basic Attachment

```text
await _attachments.AddAsync(new CommentAttachment
{
    Title = "Deployment Status",
    Text = "Successfully deployed to production"
}, ct);
```

**Renders as:**

```markdown
---

### Deployment Status

Successfully deployed to production

---
```

### Attachment with Link

```text
await _attachments.AddAsync(new CommentAttachment
{
    Title = "CI Build #123",
    TitleLink = "https://ci.example.com/builds/123",
    Text = "All checks passed"
}, ct);
```

**Renders as:**

```markdown
---

### [CI Build #123](https://ci.example.com/builds/123)

All checks passed

---
```

### Attachment with Fields

```text
await _attachments.AddAsync(new CommentAttachment
{
    Title = "Test Results",
    Fields = new List<AttachmentField>
    {
        new() { Title = "Total", Value = "150 tests" },
        new() { Title = "Passed", Value = "142 tests" },
        new() { Title = "Failed", Value = "8 tests" },
        new() { Title = "Duration", Value = "3m 45s" },
    }
}, ct);
```

**Renders as:**

```markdown
---

### Test Results

**Total**: 150 tests
**Passed**: 142 tests
**Failed**: 8 tests
**Duration**: 3m 45s

---
```

### Complete Attachment

```text
await _attachments.AddAsync(new CommentAttachment
{
    Title = "Code Quality Report",
    TitleLink = "https://sonarqube.example.com/project/123",
    Text = "SonarQube analysis completed successfully",
    Color = "green",
    Fields = new List<AttachmentField>
    {
        new() { Title = "Coverage", Value = "87.3%", Short = true },
        new() { Title = "Bugs", Value = "0", Short = true },
        new() { Title = "Code Smells", Value = "12", Short = true },
        new() { Title = "Vulnerabilities", Value = "1 (Low)", Short = true },
    }
}, ct);
```

**Renders as:**

```markdown
---

### [Code Quality Report](https://sonarqube.example.com/project/123)

SonarQube analysis completed successfully

**Coverage**: 87.3%
**Bugs**: 0
**Code Smells**: 12
**Vulnerabilities**: 1 (Low)

---
```

## API Reference

### CommentAttachmentService

```text
public class CommentAttachmentService
{
    // Add a single attachment to the comment
    Task AddAsync(CommentAttachment attachment, CancellationToken ct = default);

    // Add multiple attachments to the comment
    Task AddAsync(IEnumerable<CommentAttachment> attachments, CancellationToken ct = default);
}
```

### Adding Single Attachment

```text
await _attachments.AddAsync(new CommentAttachment
{
    Title = "Status",
    Text = "Operation completed"
}, ct);
```

### Adding Multiple Attachments

```text
await _attachments.AddAsync(new[]
{
    new CommentAttachment
    {
        Title = "Build Status",
        Text = "Build succeeded"
    },
    new CommentAttachment
    {
        Title = "Test Results",
        Text = "All tests passed"
    }
}, ct);
```

**Renders as:**

```markdown
---

### Build Status

Build succeeded

---

### Test Results

All tests passed

---
```

## Idempotent Updates

Attachments use an HTML marker (`<!-- probot-sharp-attachments -->`) to identify the attachment section in comments. This enables idempotent updates.

### How It Works

**First run:**
```
User comment text here

<!-- probot-sharp-attachments -->

---

### Build Status

Build succeeded

---
```

**Second run with new attachment:**
```
User comment text here

<!-- probot-sharp-attachments -->

---

### Build Status

Build failed - 3 tests failing

---
```

The old attachment is **replaced**, not duplicated.

### Benefits

- **Safe to re-run** - Multiple webhook deliveries won't create duplicates
- **Always up-to-date** - Latest information replaces old information
- **Clean comments** - No attachment spam

### Implementation

```text
// Internal implementation (you don't need to do this)
var attachmentSection = AttachmentMarker + "\n" + AttachmentRenderer.RenderAttachments(attachments);

if (currentBody.Contains(AttachmentMarker))
{
    // Replace existing attachments
    var markerIndex = currentBody.IndexOf(AttachmentMarker);
    newBody = currentBody.Substring(0, markerIndex) + attachmentSection;
}
else
{
    // Append new attachments
    newBody = currentBody + "\n\n" + attachmentSection;
}
```

## Advanced Usage

### Dynamic Attachments Based on CI Results

```text
[EventHandler("check_run", "completed")]
public class CIResultsAttachment : IEventHandler
{
    private readonly CommentAttachmentService _attachments;

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        var conclusion = context.Payload["check_run"]?["conclusion"]?.ToString();
        var name = context.Payload["check_run"]?["name"]?.ToString();
        var url = context.Payload["check_run"]?["html_url"]?.ToString();

        var attachment = conclusion switch
        {
            "success" => new CommentAttachment
            {
                Title = $"✅ {name}",
                TitleLink = url,
                Text = "All checks passed",
                Color = "green"
            },
            "failure" => new CommentAttachment
            {
                Title = $"❌ {name}",
                TitleLink = url,
                Text = "Some checks failed - click for details",
                Color = "red"
            },
            _ => new CommentAttachment
            {
                Title = $"⚠️ {name}",
                TitleLink = url,
                Text = $"Status: {conclusion}",
                Color = "yellow"
            }
        };

        await _attachments.AddAsync(attachment, ct);
    }
}
```

### Attachments with Conditional Fields

```text
var fields = new List<AttachmentField>
{
    new() { Title = "Status", Value = "Deployed" }
};

if (deploymentTime != null)
{
    fields.Add(new AttachmentField { Title = "Time", Value = deploymentTime });
}

if (deployedBy != null)
{
    fields.Add(new AttachmentField { Title = "Deployed By", Value = deployedBy });
}

await _attachments.AddAsync(new CommentAttachment
{
    Title = "Deployment",
    Fields = fields
}, ct);
```

### Attachments from External APIs

```text
[EventHandler("issue_comment", "created")]
public class JenkinsStatusAttachment : IEventHandler
{
    private readonly CommentAttachmentService _attachments;
    private readonly HttpClient _httpClient;

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        var comment = context.Payload["comment"]?["body"]?.ToString();

        if (comment?.Contains("/jenkins-status") == true)
        {
            // Fetch from Jenkins API
            var buildInfo = await FetchJenkinsStatus(ct);

            await _attachments.AddAsync(new CommentAttachment
            {
                Title = $"Jenkins Build #{buildInfo.Number}",
                TitleLink = buildInfo.Url,
                Text = buildInfo.Status,
                Fields = new List<AttachmentField>
                {
                    new() { Title = "Duration", Value = buildInfo.Duration },
                    new() { Title = "Branch", Value = buildInfo.Branch },
                }
            }, ct);
        }
    }

    private async Task<BuildInfo> FetchJenkinsStatus(CancellationToken ct)
    {
        var response = await _httpClient.GetAsync("https://jenkins.example.com/api/json", ct);
        // Parse response...
        return new BuildInfo();
    }
}
```

### Multiple Attachments for Different Services

```text
await _attachments.AddAsync(new[]
{
    new CommentAttachment
    {
        Title = "CI Build",
        TitleLink = "https://ci.example.com/builds/123",
        Text = "✅ Build succeeded",
        Fields = new List<AttachmentField>
        {
            new() { Title = "Duration", Value = "2m 30s" }
        }
    },
    new CommentAttachment
    {
        Title = "Code Coverage",
        TitleLink = "https://codecov.io/gh/owner/repo",
        Text = "Coverage increased by 2.3%",
        Fields = new List<AttachmentField>
        {
            new() { Title = "Total", Value = "87.3%" },
            new() { Title = "Diff", Value = "+2.3%" }
        }
    },
    new CommentAttachment
    {
        Title = "Security Scan",
        TitleLink = "https://snyk.io/test/gh/owner/repo",
        Text = "No vulnerabilities found",
        Fields = new List<AttachmentField>
        {
            new() { Title = "Dependencies", Value = "142 scanned" }
        }
    }
}, ct);
```

## Best Practices

### 1. Check for Bot Loops

Always verify the sender isn't a bot:

```text
if (context.IsBot())
{
    return; // Don't process bot-generated events
}
```

### 2. Validate Context

Ensure the payload contains expected comment data:

```text
var commentId = context.Payload["comment"]?["id"]?.ToObject<long>();
if (!commentId.HasValue || context.Repository == null)
{
    context.Logger.LogWarning("Cannot add attachment: missing comment or repository context");
    return;
}
```

### 3. Handle Errors Gracefully

```text
try
{
    await _attachments.AddAsync(attachment, ct);
}
catch (InvalidOperationException ex)
{
    context.Logger.LogWarning(ex, "Failed to add attachment: {Message}", ex.Message);
}
```

### 4. Use Semantic Colors

While not rendered in Markdown, colors document intent:

```text
Color = "green"  // Success states
Color = "red"    // Failure/error states
Color = "yellow" // Warning states
Color = "blue"   // Informational states
```

### 5. Keep Fields Concise

Short, scannable key-value pairs work best:

```text
// Good
new AttachmentField { Title = "Tests", Value = "142 passed" }

// Bad (too verbose)
new AttachmentField { Title = "Total Test Count", Value = "One hundred and forty-two tests passed" }
```

### 6. Provide Clickable Links

Always include `TitleLink` when possible:

```text
new CommentAttachment
{
    Title = "Build Status",
    TitleLink = buildUrl,  // ← Users can click to see details
    Text = "Build completed"
}
```

### 7. Update Rather Than Append

Since attachments are idempotent, you can safely update them:

```text
// First call
await _attachments.AddAsync(new CommentAttachment
{
    Title = "Deployment",
    Text = "Deploying..."
}, ct);

// Later... (replaces previous attachment)
await _attachments.AddAsync(new CommentAttachment
{
    Title = "Deployment",
    Text = "Deployment completed successfully"
}, ct);
```

### 8. Don't Overuse Attachments

Attachments are great for structured data, but:
- **Do use** for build status, test results, metrics, deployment info
- **Don't use** for simple text responses (use regular comments instead)

### 9. Consider Mobile Users

Keep attachments reasonably sized for mobile viewing:
- Limit to 5-8 fields per attachment
- Use concise titles and values
- Avoid overly long text

## Real-World Use Cases

### CI/CD Status

```text
new CommentAttachment
{
    Title = "Deploy to Production",
    TitleLink = "https://deploy.example.com/12345",
    Text = "Deployment completed successfully",
    Fields = new List<AttachmentField>
    {
        new() { Title = "Environment", Value = "production" },
        new() { Title = "Version", Value = "v2.3.1" },
        new() { Title = "Duration", Value = "5m 12s" },
        new() { Title = "Deployed By", Value = "@alice" }
    }
}
```

### Code Review Results

```text
new CommentAttachment
{
    Title = "Code Review Summary",
    TitleLink = "https://reviewdog.example.com/pr/42",
    Text = "Automated review found 3 suggestions",
    Fields = new List<AttachmentField>
    {
        new() { Title = "Linting", Value = "2 issues" },
        new() { Title = "Security", Value = "0 issues" },
        new() { Title = "Best Practices", Value = "1 suggestion" }
    }
}
```

### Performance Metrics

```text
new CommentAttachment
{
    Title = "Performance Benchmark",
    TitleLink = "https://benchmark.example.com/run/567",
    Text = "Performance regression detected",
    Color = "red",
    Fields = new List<AttachmentField>
    {
        new() { Title = "Response Time", Value = "450ms (+120ms)" },
        new() { Title = "Memory Usage", Value = "256MB (+45MB)" },
        new() { Title = "Throughput", Value = "850 req/s (-120 req/s)" }
    }
}
```

## Comparison with Node.js probot-attachments

| Feature | Node.js probot-attachments | ProbotSharp |
|---------|----------------------------|--------------|
| API | `await attachments(context).add({ title: "..." })` | `await _attachments.AddAsync(new CommentAttachment { ... }, ct)` |
| Type safety | Plain JavaScript objects | Strongly-typed C# models |
| Idempotent | Yes (HTML marker) | Yes (HTML marker) |
| Multiple attachments | Yes | Yes |
| Rendering | Markdown | Markdown |
| Color support | Documented but not rendered | Documented but not rendered |

**Node.js:**
```javascript
await attachments(context).add({
  title: 'Build Status',
  title_link: 'https://ci.example.com/builds/123',
  text: 'Build succeeded',
  color: 'green'
});
```

**ProbotSharp:**
```text
await _attachments.AddAsync(new CommentAttachment
{
    Title = "Build Status",
    TitleLink = "https://ci.example.com/builds/123",
    Text = "Build succeeded",
    Color = "green"
}, ct);
```

## See Also

- [Extensions.md](./Extensions.md) - Overview of all built-in extensions
- [SlashCommands.md](./SlashCommands.md) - Slash command guide
- [BestPractices.md](./BestPractices.md) - General best practices for ProbotSharp apps
- [../examples/AttachmentsBot/](../examples/AttachmentsBot/) - Complete working example
