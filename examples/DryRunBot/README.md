# Dry-Run Bot Example

This example demonstrates ProbotSharp's dry-run mode feature for safely testing bulk operations before executing them.

## Overview

Dry-run mode allows you to test your bot's logic without making any actual changes to GitHub. This is especially useful for:

- Testing bulk operations (creating many issues, comments, etc.)
- Validating complex workflows before deployment
- Debugging event handlers without side effects
- Safely experimenting with new features

## How to Enable Dry-Run Mode

Set the `PROBOT_DRY_RUN` environment variable to `true`:

```bash
export PROBOT_DRY_RUN=true
# or on Windows
set PROBOT_DRY_RUN=true
```

When enabled, all operations that check `context.IsDryRun` will log what they would do instead of actually executing.

## Three Patterns Demonstrated

### Pattern 1: Manual if/else Checks

**File:** `BulkIssueCreator.cs`

The most explicit pattern - manually check `context.IsDryRun` and branch your logic:

```text
if (context.IsDryRun)
{
    context.Logger.LogInformation("[DRY-RUN] Would create {Count} issues", issueCount);
    // Log details...
}
else
{
    // Actually create issues
    await context.GitHub.Issue.Create(owner, repo, newIssue);
}
```

**Pros:**
- Full control over dry-run behavior
- Easy to understand
- Can customize logging per operation

**Cons:**
- More verbose
- Duplicate structure for each operation

### Pattern 2: ExecuteOrLogAsync Helper

**File:** `BulkCommentProcessor.cs`

Use the `ExecuteOrLogAsync` extension method for cleaner, more maintainable code:

```text
await context.ExecuteOrLogAsync(
    actionDescription: "Add comment to PR #123",
    action: async () =>
    {
        await context.GitHub.Issue.Comment.Create(owner, repo, number, body);
    },
    parameters: new { owner, repo, number, body }
);
```

**Pros:**
- Cleaner, less repetitive code
- Automatic logging with structured parameters
- Consistent logging format across your app

**Cons:**
- Less flexible than manual checks
- Wraps operations in lambda

### Pattern 3: ThrowIfNotDryRun for Dangerous Operations

**File:** `LabelManager.cs`

For operations that are too dangerous to ever run automatically:

```text
// Ensure this can ONLY be logged, never executed
context.ThrowIfNotDryRun("Bulk deletion is too dangerous to run automatically");

// Rest of your code only runs in dry-run mode
context.LogDryRun("Delete all unused labels", new { labelsToDelete });
```

**Pros:**
- Prevents accidental execution of dangerous operations
- Clear intent in code
- Safety guarantee

**Cons:**
- Can't be used for normal operations
- Requires dry-run mode to be enabled

## Testing the Example

1. Build the solution:
   ```bash
   dotnet build
   ```

2. Enable dry-run mode:
   ```bash
   export PROBOT_DRY_RUN=true
   ```

3. Run your ProbotSharp application with this bot registered

4. Trigger events and watch the logs - you'll see `[DRY-RUN]` messages showing what would be done

5. Disable dry-run mode to actually execute the operations:
   ```bash
   export PROBOT_DRY_RUN=false
   ```

## Best Practices

1. **Always test in dry-run mode first** before running bulk operations
2. **Use structured logging** with the `parameters` argument so you can review exactly what will be done
3. **Start with Pattern 3** (ThrowIfNotDryRun) for any dangerous operations, then relax to Pattern 1 or 2 once tested
4. **Log enough detail** to understand the full scope of changes before executing
5. **Test both modes** - ensure your code works with dry-run enabled AND disabled

## What Gets Logged

In dry-run mode, the framework logs:

```
[DRY-RUN] Would execute: Create issue comment with parameters: {"owner":"myorg","repo":"myrepo","number":42,"body":"Hello!"}
```

You'll see:
- What operation would be performed
- All parameters that would be passed
- Structured data you can parse/analyze

## Limitations

Dry-run mode does not:
- Mock GitHub API responses (read operations still execute)
- Prevent all side effects (your custom code still runs)
- Automatically wrap all Octokit calls (you must implement the checks)

You must explicitly check `context.IsDryRun` or use the extension methods for dry-run to work.

## See Also

- [Best Practices Documentation](../../docs/BestPractices.md)
- [Context Helpers Documentation](../../docs/ContextHelpers.md)
