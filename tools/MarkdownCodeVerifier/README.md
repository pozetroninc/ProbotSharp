# Markdown Code Verifier

A tool that extracts and verifies C# code blocks from Markdown documentation files.

## Purpose

Ensures that C# code examples in documentation:
- Compile successfully
- Use correct types from the ProbotSharp project
- Stay in sync with actual implementation

## Features

✅ **Automatic Extraction** - Finds all `csharp`, `c#`, and `cs` code blocks
✅ **Smart Wrapping** - Automatically wraps code snippets in appropriate context
✅ **Type Checking** - Uses actual project assemblies for accurate verification
✅ **CI/CD Ready** - Exit code 0 on success, 1 on failures
✅ **Fast** - Uses Roslyn in-memory compilation

## Usage

### Basic Usage

```bash
# From repository root
dotnet run --project tools/MarkdownCodeVerifier

# From this directory
dotnet run

# Specify path
dotnet run -- /path/to/docs
```

### Options

```bash
# Verbose output (shows each file being verified)
dotnet run -- . "vendor/" --verbose

# Custom exclude patterns
dotnet run -- . "vendor/,node_modules/,build/"
```

### Arguments

1. **Root Path** (optional) - Directory to search for .md files (default: current directory)
2. **Exclude Patterns** (optional) - Comma-separated patterns to exclude (default: `node_modules/,.git/,.aidocs/,TEST-MARKDOWN-VERIFIER.md`)
3. **--verbose** or **-v** - Show detailed output

## How It Works

### 1. Extraction

Uses Markdig to parse Markdown and extract fenced code blocks:

```markdown
```csharp
public class MyHandler : IEventHandler { ... }
```
```

### 2. Smart Wrapping

The tool automatically wraps code based on what it detects:

**Complete code** (has `namespace` or `using`):
- Uses as-is

**Class/Interface definitions**:
- Wraps in namespace with common using statements

**Method-level code**:
- Wraps in class → namespace with usings

### 3. Compilation

Uses Roslyn to compile with:
- .NET 8.0 runtime assemblies
- Microsoft.Extensions.Logging
- All ProbotSharp.* project assemblies

### 4. Reporting

```
📚 Markdown C# Code Verifier
Root: /home/user/probot-sharp
Exclude: node_modules/, .git/, .aidocs/, TEST-MARKDOWN-VERIFIER.md

Found 28 markdown files
Found 156 C# code blocks

Verifying docs/EventHandlers.md:30... ✓
Verifying docs/EventHandlers.md:55... ✓
...

========================================
Total: 156
✓ Verified: 154
✗ Failed: 2
========================================

Failed code blocks:
  docs/Example.md:42
    The type 'InvalidType' could not be found
```

## Integration

### Pre-commit Hook

Add to `.husky/task-runner.json`:

```json
{
  "name": "verify-markdown-code",
  "group": "pre-commit",
  "command": "dotnet",
  "args": [
    "run",
    "--project",
    "tools/MarkdownCodeVerifier",
    "--",
    ".",
    "vendor/"
  ]
}
```

### GitHub Actions

Add to `.github/workflows/dotnet.yml`:

```yaml
verify-markdown-code:
  name: Verify Markdown Code Examples
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 8.0.x
    - name: Restore dependencies
      run: dotnet restore
    - name: Build project (for assembly references)
      run: dotnet build --configuration Release
    - name: Verify markdown code
      run: dotnet run --project tools/MarkdownCodeVerifier -- . "vendor/"
```

### Makefile

```makefile
verify-docs:
	dotnet run --project tools/MarkdownCodeVerifier
```

## Default Using Statements

Code blocks are automatically wrapped with:

```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Octokit;
using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Application.Abstractions.Commands;
using ProbotSharp.Application.Extensions;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Attachments;
using ProbotSharp.Domain.Commands;
using ProbotSharp.Domain.Context;
```

## Examples

### Example 1: Simple Handler (Wraps in Namespace)

**Input** (from docs):
```csharp
[EventHandler("issues", "opened")]
public class IssueHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        // ...
    }
}
```

**Compiled as**:
```text
using System;
using ProbotSharp.Domain.Context;
// ... other usings

namespace MarkdownCodeVerification
{
    [EventHandler("issues", "opened")]
    public class IssueHandler : IEventHandler
    {
        public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
        {
            // ...
        }
    }
}
```

### Example 2: Complete File (No Wrapping)

**Input**:
```text
using ProbotSharp.Domain.Context;

namespace MyApp;

public class Handler : IEventHandler { ... }
```

**Compiled as**: *(unchanged)*

### Example 3: Code Snippet (Wraps in Class)

**Input**:
```text
var (owner, repo) = context.Repo();
await context.GitHub.Issue.Comment.Create(owner, repo, 1, "Hi!");
```

**Compiled as**:
```text
namespace MarkdownCodeVerification
{
    public class GeneratedCode
    {
        public void Execute()
        {
            var (owner, repo) = context.Repo();
            await context.GitHub.Issue.Comment.Create(owner, repo, 1, "Hi!");
        }
    }
}
```

## Limitations

- **Incomplete snippets**: Some code snippets may require additional context
- **Runtime behavior**: Only verifies compilation, not runtime behavior
- **External dependencies**: Mock/stub types may not compile correctly
- **Complex scenarios**: Some advanced patterns may need manual verification

## Troubleshooting

### "The type 'X' could not be found"

The code references a type not available in the project assemblies. Either:
1. Add the assembly reference to the .csproj
2. Mark the code block as non-compilable (change language to `text` or add `// Skip verification` comment)

### "Project assemblies not found"

Run `dotnet build` first to generate the assemblies:

```bash
dotnet build --configuration Release
dotnet run --project tools/MarkdownCodeVerifier
```

### Too many false positives

Some documentation examples are intentionally incomplete. Options:
1. Use a different language tag (`text` instead of `csharp`)
2. Add a skip marker in the code
3. Adjust the wrapping logic in `WrapCodeIfNeeded()`

## Future Enhancements

- [ ] Support for skip markers (`// SKIP-VERIFICATION`)
- [ ] Custom using statement configuration
- [ ] Roslyn analyzer integration
- [ ] Support for execution/testing (not just compilation)
- [ ] Integration with existing test projects
- [ ] Incremental verification (only changed files)

## See Also

- [tests/TEST-MARKDOWN-VERIFIER.md](../../tests/TEST-MARKDOWN-VERIFIER.md) - Test file with intentional errors (excluded from verification)
- [scripts/verify-github-links.py](../../scripts/verify-github-links.py) - Verifies GitHub links in markdown
