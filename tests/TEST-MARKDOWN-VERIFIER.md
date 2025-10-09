# TEST FILE - Markdown Code Verifier Test

**⚠️ THIS IS A TEST FILE FOR MARKDOWN CODE VERIFICATION ⚠️**

This file contains intentional errors to test the MarkdownCodeVerifier tool.

## Valid C# Code (Should Pass)

### Example 1: Simple Event Handler

```csharp
[EventHandler("issues", "opened")]
public class ValidHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        var (owner, repo, issueNumber) = context.Issue();

        context.Logger.LogInformation(
            "Processing issue #{Number} in {Owner}/{Repo}",
            issueNumber, owner, repo);

        await Task.CompletedTask;
    }
}
```

### Example 2: Handler with Dependencies

```csharp
[EventHandler("pull_request", "opened")]
public class ValidPullRequestHandler : IEventHandler
{
    private readonly ILogger<ValidPullRequestHandler> _logger;

    public ValidPullRequestHandler(ILogger<ValidPullRequestHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        var (owner, repo) = context.Repo();
        _logger.LogInformation("PR opened in {Owner}/{Repo}", owner, repo);
        await Task.CompletedTask;
    }
}
```

## ❌ INTENTIONAL ERROR #1: Invalid Type Reference

This code block should FAIL because `NonExistentService` doesn't exist:

```csharp
[EventHandler("issues", "closed")]
public class InvalidServiceHandler : IEventHandler
{
    private readonly NonExistentService _service;

    public InvalidServiceHandler(NonExistentService service)
    {
        _service = service;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        await _service.DoSomethingAsync();
    }
}
```

## ❌ INTENTIONAL ERROR #2: Wrong Interface Implementation

This code block should FAIL because the method signature doesn't match `IEventHandler`:

```csharp
[EventHandler("push")]
public class InvalidMethodSignature : IEventHandler
{
    // Wrong return type - should be Task, not void
    public void HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        context.Logger.LogInformation("This won't compile!");
    }
}
```

## ❌ INTENTIONAL ERROR #3: Syntax Error

This code block should FAIL due to syntax errors:

```csharp
[EventHandler("star", "created")]
public class SyntaxErrorHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        // Missing semicolon
        var owner = "test"

        // Undefined variable
        await context.GitHub.Issue.Comment.Create(owner, repo, 1, "Hello");
    }
}
```

## Valid Code Snippet (Should Pass)

```csharp
var (owner, repo) = context.Repo();
var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>() ?? 0;
await context.GitHub.Issue.Comment.Create(owner, repo, issueNumber, "Comment");
```

## Expected Results

When running the MarkdownCodeVerifier:
- ✅ 2 valid class examples should PASS
- ✅ 1 valid snippet should PASS
- ❌ 3 intentional errors should FAIL
- **Total**: 3 passed, 3 failed out of 6 code blocks

---

# TEST FILE - Local Link Verification Tests

**⚠️ THIS SECTION TESTS THE verify-local-links.py TOOL ⚠️**

This section contains both valid and intentionally broken links to test the link verification tool.

## ✅ Valid Links (Should Pass)

### Test 1: Valid File Reference
This should PASS - the README exists in parent directory:
[Project README](../README.md)

### Test 2: Valid Intra-Document Anchor
This should PASS - referencing a header in this document:
[See Valid C# Code section](#valid-c-code-should-pass)

### Test 3: Valid Cross-Document Anchor
This should PASS - README has a quick-start section:
[Quick Start in README](../README.md#quick-start)

### Test 4: Valid Repository-Root Path
This should PASS - using absolute path from repo root:
[Architecture Docs](/docs/Architecture.md)

### Test 5: Valid Image Reference (Hypothetical)
Note: This may fail if image doesn't exist, which is expected.
![Test Image](./test-image.png)

### Test 6: Source Code Line Reference (Should be Skipped)
This should be SKIPPED (not flagged as error) - source code line references are GitHub-only:
[See Program.cs line 45](../src/ProbotSharp.Bootstrap.Api/Program.cs#L45)

### Test 7: External Link (Should be Skipped)
This should be SKIPPED - external links handled by verify-github-links.py:
[GitHub](https://github.com)

### Test 8: Relative Path to Docs
This should PASS - relative path to docs folder:
[Best Practices](../docs/BestPractices.md)

## ❌ Intentional Errors for Testing

### Test Error 1: Non-Existent File
This should FAIL - file doesn't exist:
[Missing File](./this-file-does-not-exist.md)

### Test Error 2: Invalid Anchor in This File
This should FAIL - anchor doesn't exist:
[Non-existent Section](#this-anchor-does-not-exist-in-this-file)

### Test Error 3: Invalid Cross-Document Anchor
This should FAIL - anchor doesn't exist in target file:
[Bad Anchor in README](../README.md#this-section-does-not-exist-in-readme)

### Test Error 4: Non-Existent Image
This should FAIL - image doesn't exist:
![Missing Image](./non-existent-image.png)

### Test Error 5: Broken Relative Path
This should FAIL - wrong path:
[Wrong Path](../../wrong/path/to/file.md)

### Test Error 6: Invalid Repository-Root Path
This should FAIL - file doesn't exist:
[Invalid Absolute Path](/docs/NonExistentFile.md)

## Expected Results for verify-local-links.py

When running verify-local-links.py on this file:

**Should PASS:**
- ✅ Link to ../README.md (Test 1)
- ✅ Anchor #valid-c-code-should-pass (Test 2)
- ✅ Cross-doc ../README.md#quick-start (Test 3)
- ✅ Absolute /docs/Architecture.md (Test 4)
- ✅ Relative ../docs/BestPractices.md (Test 8)

**Should be SKIPPED (not counted as errors):**
- ⊘ Source code line ../src/ProbotSharp.Bootstrap.Api/Program.cs#L45 (Test 6)
- ⊘ External link https://github.com (Test 7)

**Should FAIL:**
- ❌ ./this-file-does-not-exist.md (Test Error 1)
- ❌ #this-anchor-does-not-exist-in-this-file (Test Error 2)
- ❌ ../README.md#this-section-does-not-exist-in-readme (Test Error 3)
- ❌ ./non-existent-image.png (Test Error 4)
- ❌ ../../wrong/path/to/file.md (Test Error 5)
- ❌ /docs/NonExistentFile.md (Test Error 6)

**May FAIL (depends on file existence):**
- 🤷 ./test-image.png (Test 5) - Expected to fail if image not created

**Expected Summary:**
- Valid links: 5 (Tests 1, 2, 3, 4, 8)
- Broken links: 7 (Test 5 + Error Tests 1-6)
- Skipped: 2 (Test 6: source code ref, Test 7: external link)
