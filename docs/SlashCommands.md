# Slash Commands

Slash commands provide an intuitive way for users to interact with your ProbotSharp app directly from GitHub issue and pull request comments. By typing `/command arguments`, users can trigger actions without leaving the conversation.

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Creating Command Handlers](#creating-command-handlers)
- [Registration and Discovery](#registration-and-discovery)
- [Command Parsing](#command-parsing)
- [Argument Handling](#argument-handling)
- [Error Handling](#error-handling)
- [Advanced Patterns](#advanced-patterns)
- [Complete Examples](#complete-examples)
- [Best Practices](#best-practices)

## Overview

Slash commands are a user-friendly interface for GitHub Apps. Instead of requiring users to remember complex bot commands or API calls, they can simply type natural commands in comments:

```
/label bug, priority:high
/assign @octocat
/deploy staging
```

### Features

- **Simple syntax** - `/command arguments` format that's easy to remember
- **Multiple commands** - Multiple slash commands in a single comment
- **Case-insensitive** - `/Label` and `/label` both work
- **Auto-discovery** - Commands are registered automatically via attributes
- **Type-safe** - Full C# strong typing with IntelliSense support
- **Composable** - Multiple handlers can respond to the same command
- **Works everywhere** - Issue comments, PR comments, and review comments

## Quick Start

### 1. Create a Command Handler

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
        // Parse arguments
        var labels = command.Arguments.Split(',').Select(l => l.Trim()).ToArray();

        // Get issue number
        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>() ?? 0;

        // Add labels via GitHub API
        await context.GitHub.Issue.Labels.AddToIssue(
            context.Repository.Owner,
            context.Repository.Name,
            issueNumber,
            labels);

        context.Logger.LogInformation(
            "Added labels {Labels} to issue #{IssueNumber}",
            string.Join(", ", labels),
            issueNumber);
    }
}
```

### 2. Register Commands

```text
using ProbotSharp.Application.Extensions;
using Microsoft.Extensions.DependencyInjection;

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

### 3. Use the Command

Users can now comment on issues:

```
/label bug, enhancement
```

The bot will automatically parse the command and execute your handler!

## Creating Command Handlers

### Basic Handler

A command handler implements `ISlashCommandHandler`:

```text
public interface ISlashCommandHandler
{
    Task HandleAsync(
        ProbotSharpContext context,
        SlashCommand command,
        CancellationToken cancellationToken = default);
}
```

Decorate your handler with `[SlashCommandHandler("command-name")]`:

```text
[SlashCommandHandler("greet")]
public class GreetCommand : ISlashCommandHandler
{
    public async Task HandleAsync(
        ProbotSharpContext context,
        SlashCommand command,
        CancellationToken cancellationToken)
    {
        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>() ?? 0;

        await context.GitHub.Issue.Comment.Create(
            context.Repository.Owner,
            context.Repository.Name,
            issueNumber,
            $"Hello! You said: {command.Arguments}");
    }
}
```

**Usage:**
```
/greet world
```

**Result:** Comment posted saying "Hello! You said: world"

### Handler with Dependency Injection

Inject services into your handler via the constructor:

```text
[SlashCommandHandler("assign")]
public class AssignCommand : ISlashCommandHandler
{
    private readonly ILogger<AssignCommand> _logger;
    private readonly MetadataService _metadata;

    public AssignCommand(ILogger<AssignCommand> logger, MetadataService metadata)
    {
        _logger = logger;
        _metadata = metadata;
    }

    public async Task HandleAsync(
        ProbotSharpContext context,
        SlashCommand command,
        CancellationToken ct)
    {
        var assignees = command.Arguments.Split(',').Select(a => a.Trim().TrimStart('@')).ToArray();
        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>() ?? 0;

        await context.GitHub.Issue.Assignee.AddAssignees(
            context.Repository.Owner,
            context.Repository.Name,
            issueNumber,
            assignees.ToArray());

        // Track in metadata
        await _metadata.SetAsync("last_assigned", string.Join(",", assignees), ct);

        _logger.LogInformation("Assigned {Assignees} to issue #{IssueNumber}", assignees, issueNumber);
    }
}
```

### Multiple Commands per Handler

A single handler can respond to multiple command names:

```text
[SlashCommandHandler("label")]
[SlashCommandHandler("tag")]
[SlashCommandHandler("categorize")]
public class LabelCommand : ISlashCommandHandler
{
    public async Task HandleAsync(
        ProbotSharpContext context,
        SlashCommand command,
        CancellationToken ct)
    {
        // command.Name tells you which command was used
        context.Logger.LogInformation("Handling command: {CommandName}", command.Name);

        var labels = command.Arguments.Split(',').Select(l => l.Trim()).ToArray();
        // ... add labels
    }
}
```

**Usage:**
```
/label bug
/tag bug
/categorize bug
```

All three commands trigger the same handler!

## Registration and Discovery

### Automatic Discovery

The recommended approach is automatic discovery:

```text
services.AddSlashCommands(typeof(MyApp).Assembly);
```

This scans the assembly for all types decorated with `[SlashCommandHandler]` and registers them automatically.

### Manual Registration

For more control, register handlers manually:

```text
var router = services.BuildServiceProvider().GetRequiredService<SlashCommandRouter>();
router.RegisterHandler("label", typeof(LabelCommand));
```

### Multiple Handlers per Command

You can register multiple handlers for the same command:

```text
[SlashCommandHandler("deploy")]
public class DeployNotifier : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        // Send notification
    }
}

[SlashCommandHandler("deploy")]
public class DeployExecutor : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        // Actually perform deployment
    }
}
```

Both handlers will execute when `/deploy` is used (in registration order).

## Command Parsing

### Syntax Rules

Commands must follow this pattern:

```
/command-name arguments go here
```

- **Leading slash** - Commands start with `/`
- **Command name** - Alphanumeric characters, hyphens, and underscores (`a-zA-Z0-9_-`)
- **Arguments** - Everything after the first space (optional)
- **One per line** - Each command must be on its own line

### Valid Commands

```
/label bug
/assign @alice
/priority high
/deploy-prod
/auto_merge
/merge_when_green --force
```

### Invalid Commands

```
/ label bug          ❌ Space after slash
/label+bug          ❌ Plus sign not allowed in command name
/label bug /close   ❌ Two commands on same line
```

### The SlashCommand Model

When a command is parsed, you receive a `SlashCommand` object:

```text
public class SlashCommand
{
    public string Name { get; init; }        // e.g., "label"
    public string Arguments { get; init; }   // e.g., "bug, enhancement"
    public string FullText { get; init; }    // e.g., "/label bug, enhancement"
    public int LineNumber { get; init; }     // 1-indexed line number in comment
}
```

**Example:**

Comment:
```
Here's my feedback:
/label bug, priority:high
/assign @johndoe
```

Parsed commands:
```text
// Command 1
Name: "label"
Arguments: "bug, priority:high"
FullText: "/label bug, priority:high"
LineNumber: 2

// Command 2
Name: "assign"
Arguments: "@johndoe"
FullText: "/assign @johndoe"
LineNumber: 3
```

## Argument Handling

### Simple Arguments

```text
[SlashCommandHandler("close")]
public class CloseCommand : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        var reason = string.IsNullOrWhiteSpace(command.Arguments)
            ? "Closed via slash command"
            : command.Arguments;

        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>() ?? 0;

        await context.GitHub.Issue.Update(
            context.Repository.Owner,
            context.Repository.Name,
            issueNumber,
            new IssueUpdate { State = ItemState.Closed });

        await context.GitHub.Issue.Comment.Create(
            context.Repository.Owner,
            context.Repository.Name,
            issueNumber,
            $"Closing: {reason}");
    }
}
```

**Usage:**
```
/close Duplicate of #42
/close
```

### Comma-Separated Lists

```text
[SlashCommandHandler("label")]
public class LabelCommand : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        var labels = command.Arguments
            .Split(',')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        if (labels.Length == 0)
        {
            // Handle missing arguments
            return;
        }

        // Add labels...
    }
}
```

**Usage:**
```
/label bug, enhancement, priority:high
```

### Structured Arguments

For complex arguments, parse structured formats:

```text
[SlashCommandHandler("schedule")]
public class ScheduleCommand : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        // Parse: /schedule action=deploy env=staging time=2024-10-15T14:00:00Z
        var args = ParseKeyValuePairs(command.Arguments);

        var action = args.GetValueOrDefault("action");
        var env = args.GetValueOrDefault("env");
        var time = args.GetValueOrDefault("time");

        // Validate and execute...
    }

    private Dictionary<string, string> ParseKeyValuePairs(string arguments)
    {
        return arguments
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(arg => arg.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1]);
    }
}
```

**Usage:**
```
/schedule action=deploy env=staging time=2024-10-15T14:00:00Z
```

### Flag Arguments

```text
[SlashCommandHandler("merge")]
public class MergeCommand : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        var force = command.Arguments.Contains("--force");
        var squash = command.Arguments.Contains("--squash");

        // Perform merge with options...
    }
}
```

**Usage:**
```
/merge --force --squash
```

## Error Handling

### Validation

Always validate arguments before execution:

```text
[SlashCommandHandler("label")]
public class LabelCommand : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>();
        if (!issueNumber.HasValue || context.Repository == null)
        {
            context.Logger.LogWarning("Could not extract issue number or repository from payload");
            return;
        }

        if (string.IsNullOrWhiteSpace(command.Arguments))
        {
            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                "❌ No labels provided. Usage: `/label label1, label2, ...`");
            return;
        }

        // Process command...
    }
}
```

### Exception Handling

Wrap API calls in try-catch blocks:

```text
[SlashCommandHandler("label")]
public class LabelCommand : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>() ?? 0;
        var labels = command.Arguments.Split(',').Select(l => l.Trim()).ToArray();

        try
        {
            await context.GitHub.Issue.Labels.AddToIssue(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber,
                labels);

            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber,
                $"✅ Added labels: {string.Join(", ", labels.Select(l => $"`{l}`"))}");
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            context.Logger.LogError(ex, "Failed to add labels: some labels may not exist");

            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber,
                "❌ Failed to add labels. Make sure all labels exist in this repository.");
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "Failed to add labels: {ErrorMessage}", ex.Message);

            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber,
                "❌ An error occurred while adding labels. Please try again.");
        }
    }
}
```

## Advanced Patterns

### Help Command

Provide a help command to document available commands:

```text
[SlashCommandHandler("help")]
public class HelpCommand : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        var helpText = @"
## Available Commands

- `/label <labels>` - Add comma-separated labels to this issue
- `/assign <users>` - Assign users to this issue (e.g., `/assign @alice, @bob`)
- `/close [reason]` - Close this issue with an optional reason
- `/deploy <environment>` - Deploy to the specified environment
- `/help` - Show this help message

For more information, visit https://github.com/your-org/your-bot/docs
";

        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>() ?? 0;

        await context.GitHub.Issue.Comment.Create(
            context.Repository.Owner,
            context.Repository.Name,
            issueNumber,
            helpText);
    }
}
```

### Permission Checks

Verify user permissions before executing sensitive commands:

```text
[SlashCommandHandler("deploy")]
public class DeployCommand : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        var sender = context.Payload["sender"]?["login"]?.ToString();
        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>() ?? 0;

        // Check if user is a collaborator
        var permission = await context.GitHub.Repository.Collaborator.ReviewPermission(
            context.Repository.Owner,
            context.Repository.Name,
            sender);

        if (permission.Permission != PermissionLevel.Admin && permission.Permission != PermissionLevel.Write)
        {
            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber,
                "❌ You don't have permission to deploy. Only collaborators with write access can deploy.");
            return;
        }

        // Perform deployment...
    }
}
```

### State Machines with Metadata

Use metadata to implement multi-step workflows:

```text
[SlashCommandHandler("review")]
public class ReviewCommand : ISlashCommandHandler
{
    private readonly MetadataService _metadata;

    public ReviewCommand(MetadataService metadata)
    {
        _metadata = metadata;
    }

    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        var action = command.Arguments.ToLower();

        switch (action)
        {
            case "approve":
                await _metadata.SetAsync("review_status", "approved", ct);
                break;

            case "reject":
                await _metadata.SetAsync("review_status", "rejected", ct);
                break;

            case "status":
                var status = await _metadata.GetAsync("review_status", ct) ?? "pending";
                var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>() ?? 0;

                await context.GitHub.Issue.Comment.Create(
                    context.Repository.Owner,
                    context.Repository.Name,
                    issueNumber,
                    $"Review status: **{status}**");
                break;
        }
    }
}
```

**Usage:**
```
/review approve
/review status
```

## Complete Examples

### Example 1: Label Management

```text
using Octokit;
using ProbotSharp.Application.Abstractions.Commands;
using ProbotSharp.Domain.Commands;
using ProbotSharp.Domain.Context;

[SlashCommandHandler("label")]
public class LabelCommand : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct = default)
    {
        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>();
        if (!issueNumber.HasValue || context.Repository == null)
        {
            context.Logger.LogWarning(
                "Could not extract issue number or repository from payload for /label command");
            return;
        }

        if (string.IsNullOrWhiteSpace(command.Arguments))
        {
            context.Logger.LogWarning(
                "No labels provided for /label command on issue #{IssueNumber}",
                issueNumber.Value);

            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                "❌ No labels provided. Usage: `/label label1, label2, ...`");
            return;
        }

        var labels = command.Arguments
            .Split(',')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        if (labels.Length == 0)
        {
            context.Logger.LogWarning(
                "No valid labels parsed from arguments '{Arguments}' for issue #{IssueNumber}",
                command.Arguments,
                issueNumber.Value);
            return;
        }

        try
        {
            await context.GitHub.Issue.Labels.AddToIssue(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                labels);

            context.Logger.LogInformation(
                "Successfully added labels {Labels} to issue #{IssueNumber} via /label command",
                string.Join(", ", labels),
                issueNumber.Value);

            var labelList = string.Join(", ", labels.Select(l => $"`{l}`"));
            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                $"✅ Added labels: {labelList}");
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            // Specific error for labels that don't exist
            context.Logger.LogError(
                ex,
                "Failed to add labels to issue #{IssueNumber}: Some labels may not exist in the repository",
                issueNumber.Value);

            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                "❌ Failed to add labels. Make sure all labels exist in this repository.");
        }
        catch (Exception ex)
        {
            // Generic error handler
            context.Logger.LogError(
                ex,
                "Failed to add labels to issue #{IssueNumber}: {ErrorMessage}",
                issueNumber.Value,
                ex.Message);

            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                "❌ An error occurred while adding labels. Please try again.");
        }
    }
}
```

**Key improvements in this example:**

1. **Use `context.Logger`** - No need to inject `ILogger<T>` when `ProbotSharpContext` already provides a logger
2. **Specific exception handling** - Use `when` clauses to handle different error scenarios appropriately
3. **Validate filtered results** - Check if any labels remain after filtering whitespace
4. **Better log messages** - Include context like command name and structured parameters

### Example 2: Issue Assignment

```text
[SlashCommandHandler("assign")]
public class AssignCommand : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>() ?? 0;

        var assignees = command.Arguments
            .Split(',')
            .Select(a => a.Trim().TrimStart('@'))
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .ToArray();

        if (assignees.Length == 0)
        {
            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber,
                "❌ No assignees provided. Usage: `/assign @user1, @user2`");
            return;
        }

        await context.GitHub.Issue.Assignee.AddAssignees(
            context.Repository.Owner,
            context.Repository.Name,
            issueNumber,
            assignees.ToArray());

        await context.GitHub.Issue.Comment.Create(
            context.Repository.Owner,
            context.Repository.Name,
            issueNumber,
            $"✅ Assigned to: {string.Join(", ", assignees.Select(a => $"@{a}"))}");
    }
}
```

### Example 3: Status Command with Metadata

```text
[SlashCommandHandler("status")]
public class StatusCommand : ISlashCommandHandler
{
    private readonly MetadataService _metadata;

    public StatusCommand(MetadataService metadata)
    {
        _metadata = metadata;
    }

    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>() ?? 0;

        var allMetadata = await _metadata.GetAllAsync(ct);
        var statusLines = allMetadata.Select(kvp => $"- **{kvp.Key}**: {kvp.Value}");

        var statusMessage = allMetadata.Any()
            ? $"## Issue Status\n\n{string.Join("\n", statusLines)}"
            : "No status information available.";

        await context.GitHub.Issue.Comment.Create(
            context.Repository.Owner,
            context.Repository.Name,
            issueNumber,
            statusMessage);
    }
}
```

## Best Practices

### 1. Validate Input

Always validate command arguments and provide helpful error messages:

```text
if (string.IsNullOrWhiteSpace(command.Arguments))
{
    await PostHelpMessage(context);
    return;
}
```

### 2. Provide User Feedback

Post comments to confirm success or explain errors:

```text
await context.GitHub.Issue.Comment.Create(
    context.Repository.Owner,
    context.Repository.Name,
    issueNumber,
    "✅ Command executed successfully!");
```

### 3. Avoid Bot Loops

The `SlashCommandEventHandler` automatically ignores bot comments, but be careful with other event handlers.

### 4. Use Logging

Log command execution for debugging and monitoring:

```text
_logger.LogInformation(
    "Handling /{CommandName} command with arguments: {Arguments}",
    command.Name,
    command.Arguments);
```

### 5. Handle Cancellation

Respect cancellation tokens for long-running operations:

```text
public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
{
    ct.ThrowIfCancellationRequested();
    // ... async work
}
```

### 6. Document Commands

Provide a `/help` command that lists all available commands and their usage.

### 7. Check Permissions

For sensitive operations, verify user permissions before executing.

### 8. Keep Handlers Focused

Each handler should do one thing well. Use multiple handlers for complex workflows.

## See Also

- [Extensions.md](./Extensions.md) - Overview of all built-in extensions
- [Metadata.md](./Metadata.md) - Metadata storage guide
- [BestPractices.md](./BestPractices.md) - General best practices for ProbotSharp apps
- [../examples/SlashCommandsBot/](../examples/SlashCommandsBot/) - Complete working example
