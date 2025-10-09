# Best Practices Examples

This directory contains code examples demonstrating best practices and anti-patterns for building ProbotSharp GitHub Apps.

## Files

- **[GoodExample.cs](GoodExample.cs)** - Demonstrates all best practices with well-implemented handlers
- **[BadExample.cs](BadExample.cs)** - Shows common anti-patterns and mistakes to avoid (annotated)

## Quick Reference

### Good Examples Include

1. **Empathy** ([GoodCommandHandler](GoodExample.cs#L212))
   - Concise, factual bot responses
   - No excessive emojis or friendliness
   - Clear, professional communication

2. **Autonomy** ([GoodStaleIssueHandler](GoodExample.cs#L17))
   - Dry-run mode with safe defaults
   - No bulk actions without permission
   - Clear logging of what would happen

3. **Configuration** ([ConfigService](GoodExample.cs#L168))
   - Sensible defaults for all settings
   - Graceful handling of missing config files
   - Structured configuration with YamlDotNet

4. **Error Handling** ([ProcessStaleIssueAsync](GoodExample.cs#L103))
   - Specific exception handling with pattern matching
   - Graceful degradation on errors
   - Actionable error messages
   - Proper cancellation support

5. **Security** ([GoodCommandHandler](GoodExample.cs#L235))
   - Bot identity checks to prevent loops
   - Input validation and length limits
   - Regex timeout to prevent ReDoS
   - No secret logging

6. **Performance**
   - Batch API operations
   - Use payload data instead of extra API calls
   - Async/await best practices with ConfigureAwait
   - Cancellation token propagation

7. **C#-Specific Patterns**
   - Dependency injection
   - Structured logging with ILogger
   - Proper async/await usage
   - Resource management

### Bad Examples Include

The [BadExample.cs](BadExample.cs) file demonstrates common mistakes:

1. **Empathy Violations** ([BadStaleIssueHandler](BadExample.cs#L45))
   - Overly friendly, pretends to be human
   - Excessive emojis: "Oh no! 😢 I'm so sorry..."

2. **Autonomy Violations** ([BadInstallationHandler](BadExample.cs#L18))
   - Bulk actions without permission on install
   - No dry-run mode
   - Immediate destructive actions

3. **Configuration Issues** ([BadConfigService](BadExample.cs#L142))
   - No default values
   - Poor error handling
   - Hard-coded values

4. **Error Handling Issues** ([BadErrorHandlingHandler](BadExample.cs#L174))
   - Swallowing exceptions
   - No null checks
   - Silent failures

5. **Performance Issues** ([BadPerformanceHandler](BadExample.cs#L196))
   - Unnecessary API calls
   - No batching
   - Blocking with Thread.Sleep()
   - Using .Result instead of await

6. **Security Issues** ([BadSecurityHandler](BadExample.cs#L232))
   - No bot checks (infinite loop risk)
   - Logging secrets and tokens
   - No input validation
   - ReDoS vulnerabilities

7. **C# Specific Issues** ([BadAsyncHandler](BadExample.cs#L271))
   - Async void methods
   - Blocking async code
   - Poor resource management
   - No cancellation tokens

## Pattern Quick Links

### Empathy

| Pattern | Good Example | Bad Example |
|---------|--------------|-------------|
| Bot messaging | [GoodCommandHandler](GoodExample.cs#L212) - "Adding label 'X' to this issue" | [BadStaleIssueHandler](BadExample.cs#L58) - "Oh no! 😢 I'm so sorry..." |

### Autonomy

| Pattern | Good Example | Bad Example |
|---------|--------------|-------------|
| Dry-run mode | [GoodStaleIssueHandler](GoodExample.cs#L75) - Default dry-run enabled | [BadStaleIssueHandler](BadExample.cs#L56) - No dry-run, immediate action |
| Bulk actions | [GoodStaleIssueHandler](GoodExample.cs#L31) - Config required | [BadInstallationHandler](BadExample.cs#L27) - No permission check |

### Configuration

| Pattern | Good Example | Bad Example |
|---------|--------------|-------------|
| Defaults | [StaleConfig](GoodExample.cs#L223) - All properties have defaults | [BadConfig](BadExample.cs#L161) - No defaults |
| Loading | [ConfigService](GoodExample.cs#L175) - Graceful fallback | [BadConfigService](BadExample.cs#L145) - Crashes if missing |

### Error Handling

| Pattern | Good Example | Bad Example |
|---------|--------------|-------------|
| API errors | [ProcessStaleIssueAsync](GoodExample.cs#L119) - Specific catch blocks | [BadErrorHandlingHandler](BadExample.cs#L189) - Silent catch-all |
| Null checks | [GoodCommandHandler](GoodExample.cs#L240) - Validates all inputs | [BadErrorHandlingHandler](BadExample.cs#L181) - No null checks |

### Security

| Pattern | Good Example | Bad Example |
|---------|--------------|-------------|
| Bot checks | [GoodCommandHandler](GoodExample.cs#L235) - Checks IsBot() | [BadCommentHandler](BadExample.cs#L90) - No bot check |
| Logging | [GoodStaleIssueHandler](GoodExample.cs#L41) - Structured, safe | [BadSecurityHandler](BadExample.cs#L246) - Logs secrets |
| Input validation | [GoodCommandHandler](GoodExample.cs#L248) - Length limits, regex timeout | [BadSecurityHandler](BadExample.cs#L251) - No validation |

### Performance

| Pattern | Good Example | Bad Example |
|---------|--------------|-------------|
| API batching | [FetchStaleIssuesAsync](GoodExample.cs#L76) - Single request | [BadPerformanceHandler](BadExample.cs#L209) - Multiple requests |
| Async/await | [GoodStaleIssueHandler](GoodExample.cs#L23) - Proper async | [BadPerformanceHandler](BadExample.cs#L224) - Blocking with .Result |

### C#-Specific

| Pattern | Good Example | Bad Example |
|---------|--------------|-------------|
| Dependency Injection | [GoodStaleIssueHandler constructor](GoodExample.cs#L19) | [BadInstallationHandler](BadExample.cs#L16) - Creates logger directly |
| Cancellation Tokens | [GoodStaleIssueHandler](GoodExample.cs#L23) - Accepts and propagates | [BadInstallationHandler](BadExample.cs#L22) - Ignores token |
| Resource Management | Uses DI for HttpClient | [BadResourceHandler](BadExample.cs#L295) - Creates per instance |

## Using These Examples

### For Learning

1. **Start with GoodExample.cs** - Study the implementations to understand best practices
2. **Review BadExample.cs** - Learn what to avoid and why
3. **Compare patterns** - See the good and bad side-by-side for each concept

### For Your Projects

1. **Copy patterns from GoodExample.cs** as starting points for your handlers
2. **Use as a checklist** - Make sure your code follows the good patterns
3. **Review before publishing** - Check against anti-patterns in BadExample.cs

### For Code Reviews

1. **Reference specific examples** when reviewing pull requests
2. **Link to patterns** when suggesting improvements
3. **Share with team** to establish coding standards

## Additional Resources

- [Best Practices Guide](../../docs/BestPractices.md) - Comprehensive documentation
- [Best Practices Checklist](../../docs/BestPractices-Checklist.md) - Pre-release review checklist
- [ProbotSharp Architecture](../../docs/Architecture.md) - System architecture guide

## Running the Examples

These examples are meant for reference and learning. They are not executable projects but code snippets showing patterns.

To use these patterns in your own ProbotSharp app:

1. Create a new bot using the template:
   ```bash
   dotnet new probotsharp-app -n MyBot
   ```

2. Copy patterns from GoodExample.cs to your handlers

3. Avoid patterns shown in BadExample.cs

4. Review with the [Best Practices Checklist](../../docs/BestPractices-Checklist.md) before deployment
