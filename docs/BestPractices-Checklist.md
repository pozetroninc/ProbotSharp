# ProbotSharp App Pre-Release Checklist

Use this checklist to ensure your ProbotSharp GitHub App follows best practices before publishing or deploying to production.

**📖 For detailed explanations and code examples, see [BestPractices.md](BestPractices.md)**

## 📋 Table of Contents

- [Empathy](#empathy)
- [Autonomy](#autonomy)
- [Configuration](#configuration)
- [Error Handling](#error-handling)
- [Performance](#performance)
- [Security](#security)
- [Testing](#testing)
- [Deployment](#deployment)
- [Documentation](#documentation)
- [C#-Specific](#c-specific)

---

## Empathy

Bot communication should be clear, concise, and professional. **[→ See BestPractices.md#empathy](BestPractices.md#empathy)**

- [ ] Bot responses are factual and concise (not overly friendly)
- [ ] Bot clearly identifies itself in comments and messages
- [ ] Bot avoids excessive emojis and exclamation marks
- [ ] Bot doesn't pretend to be human or use first-person emotions
- [ ] Error messages are helpful and actionable
- [ ] Bot provides clear next steps when actions are needed

---

## Autonomy

Users should control bulk and destructive actions. **[→ See BestPractices.md#autonomy](BestPractices.md#autonomy)**

- [ ] No bulk actions performed without explicit configuration
- [ ] Installation doesn't trigger automatic bulk operations
- [ ] Dry-run mode implemented for all destructive actions
- [ ] Dry-run is enabled by default (users must explicitly disable)
- [ ] Dry-run logging clearly shows what would happen
- [ ] Users can preview actions before enabling them

**Quick Check:** `DryRun = true` as default, use `context.IsDryRun` or `context.ExecuteOrLogAsync()`

---

## Configuration

Configuration should be flexible with sensible defaults. **[→ See BestPractices.md#configuration](BestPractices.md#configuration)**

- [ ] All settings have sensible defaults
- [ ] App works without configuration file
- [ ] All defaults are customizable per installation
- [ ] Configuration stored in `.github/{appname}.yml`
- [ ] Configuration schema documented in README
- [ ] Invalid configuration falls back to defaults gracefully
- [ ] Configuration loading errors are logged but don't crash app

**Quick Check:** All config properties have `= defaultValue`, `NotFoundException` returns `new AppConfig()`

---

## Error Handling

Errors should be handled gracefully without crashing. **[→ See BestPractices.md#error-handling](BestPractices.md#error-handling)**

- [ ] All event handlers have try-catch blocks
- [ ] GitHub API errors are caught and logged with context
- [ ] No unhandled exceptions that crash the app
- [ ] Null checks on all payload values
- [ ] Repository context validation before use
- [ ] Specific exception types handled appropriately
- [ ] Cancellation is handled gracefully

**Quick Check:** Catch `NotFoundException`, `ApiException`, `RateLimitExceededException` specifically

---

## Performance

App should be efficient and minimize API calls. **[→ See BestPractices.md#performance](BestPractices.md#performance)**

- [ ] Use payload data instead of additional API calls when possible
- [ ] API calls are batched when appropriate
- [ ] No blocking operations (Thread.Sleep, .Wait(), .Result)
- [ ] Proper async/await usage throughout
- [ ] ConfigureAwait(false) used in library/handler code
- [ ] CancellationToken passed to all async operations
- [ ] Long-running operations support cancellation

**Quick Check:** Use `context.Payload` first, batch array operations, `await...ConfigureAwait(false)`

---

## Security

App should be secure and protect sensitive data. **[→ See BestPractices.md#security](BestPractices.md#security)**

- [ ] Bot checks implemented to prevent infinite loops
- [ ] IsBot() called at start of handlers that create events
- [ ] No secrets or tokens logged anywhere
- [ ] User input validated before use
- [ ] Input length limits enforced
- [ ] Regex patterns have timeouts (prevent ReDoS)
- [ ] Sensitive configuration from environment variables only
- [ ] No credentials in repository files
- [ ] Webhook signature verification enabled

**Critical Checks:**
```text
if (context.IsBot()) return;  // Prevent loops
Regex.IsMatch(input, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100))  // ReDoS protection
```

---

## Testing

Comprehensive tests ensure reliability. **[→ See BestPractices.md#testing](BestPractices.md#testing)**

- [ ] All event handlers have unit tests
- [ ] Tests cover successful scenarios
- [ ] Tests cover error scenarios (permissions, not found, etc.)
- [ ] Tests cover edge cases (null values, bot senders, etc.)
- [ ] Configuration loading tested with and without files
- [ ] API error handling tested with mocked exceptions
- [ ] Bot detection tested
- [ ] Cancellation tested

**Quick Check:** Test success case, permission denied, bot sender, cancellation for each handler

---

## Deployment

Production deployment should be secure and observable. **[→ See BestPractices.md#deployment](BestPractices.md#deployment)**

- [ ] All secrets from environment variables
- [ ] .env file in .gitignore (never committed)
- [ ] .env.example provided as template
- [ ] Structured logging implemented
- [ ] Metrics/telemetry configured
- [ ] Health checks implemented
- [ ] Graceful shutdown implemented
- [ ] Docker support (if applicable)
- [ ] Deployment documentation provided

**Quick Check:** All secrets use `Configuration["KEY"]`, `.AddHealthChecks()`, `ApplicationStopping.Register()`

---

## Documentation

Clear documentation helps users and contributors. **[→ See BestPractices.md#documentation](BestPractices.md#documentation)**

- [ ] README includes clear description of what app does
- [ ] Installation instructions provided
- [ ] Configuration options documented with defaults
- [ ] Required GitHub App permissions listed
- [ ] Required webhook events listed
- [ ] Usage examples provided
- [ ] Contributing guidelines provided
- [ ] License file included
- [ ] Changelog maintained with semantic versioning

**README.md Must Include:**
- [ ] Description and features
- [ ] Installation steps
- [ ] Configuration table with all options
- [ ] Permission requirements
- [ ] Usage examples
- [ ] Development setup
- [ ] License information

---

## C#-Specific

Follow C# and .NET best practices. **[→ See BestPractices.md#c-specific-best-practices](BestPractices.md#c-specific-best-practices)**

- [ ] Dependency injection used for all services
- [ ] ILogger<T> used for all logging
- [ ] Structured logging with named parameters
- [ ] IDisposable implemented where needed
- [ ] HttpClient injected via IHttpClientFactory (not created directly)
- [ ] Async methods return Task (never async void)
- [ ] CancellationToken accepted in all async methods
- [ ] Resources properly disposed
- [ ] No string interpolation in log messages (use templates)

**Critical Checks:**
```text
// Constructor injection for all dependencies
public MyHandler(ILogger<MyHandler> logger, IConfigService config) { ... }

// Structured logging - template with parameters, NOT string interpolation
_logger.LogInformation("Processing {Number} in {Repo}", num, repo);

// CancellationToken in all async methods
public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
```

---

## Final Verification

Before deploying to production:

### Code Quality
- [ ] All checklist items above are complete
- [ ] Code reviewed by at least one other person
- [ ] All tests passing
- [ ] No compiler warnings
- [ ] Code follows project style guidelines

### Security
- [ ] Security scan completed (e.g., Snyk, SonarQube)
- [ ] No secrets in code or config files
- [ ] All dependencies up to date
- [ ] Webhook signature verification tested

### Documentation
- [ ] README complete and accurate
- [ ] Configuration examples provided
- [ ] API permissions documented
- [ ] Known issues/limitations documented

### Testing
- [ ] Unit tests cover critical paths
- [ ] Integration tests completed
- [ ] Manual testing in staging environment
- [ ] Error scenarios tested

### Deployment
- [ ] Staging deployment successful
- [ ] Monitoring/alerting configured
- [ ] Rollback plan documented
- [ ] Team notified of deployment

---

## Quick Reference Card

Print this quick reference for your desk:

### ✅ Always Do
1. Check IsBot() to prevent loops
2. Default DryRun = true
3. Validate all payload values
4. Use structured logging
5. Handle errors gracefully
6. Batch API operations
7. Pass CancellationToken
8. Use dependency injection

### ❌ Never Do
1. Log secrets or tokens
2. Bulk actions without permission
3. Block with .Wait() or .Result
4. Swallow exceptions silently
5. Skip input validation
6. Create HttpClient directly
7. Use async void
8. Pretend bot is human

---

## Resources

- **[Best Practices Guide](BestPractices.md)** - Comprehensive documentation with detailed code examples
- [Good Examples](../examples/BestPracticesExamples/GoodExample.cs) - Code examples
- [Bad Examples](../examples/BestPracticesExamples/BadExample.cs) - Anti-patterns to avoid
- [ProbotSharp Architecture](Architecture.md) - System architecture

---

## Version

**Checklist Version:** 1.0.0
**Last Updated:** 2025-10-05
**Compatible with:** ProbotSharp 1.0+

## Contributing

Found an issue with this checklist or have suggestions? Please open an issue or pull request in the ProbotSharp repository.
