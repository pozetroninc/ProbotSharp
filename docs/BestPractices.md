# Best Practices for ProbotSharp Apps

This guide provides recommendations for building effective, user-friendly GitHub Apps with ProbotSharp. These best practices are adapted from the [Node.js Probot framework](https://probot.github.io/docs/best-practices/) and enhanced with C#-specific guidance.

**Contents:**

- [The Three Laws of Robotics](#the-three-laws-of-robotics)
- [Empathy](#empathy)
  - [Avoid the Uncanny Valley](#avoid-the-uncanny-valley)
- [Autonomy](#autonomy)
  - [Never Take Bulk Actions Without Explicit Permission](#never-take-bulk-actions-without-explicit-permission)
  - [Include Dry-Run Functionality](#include-dry-run-functionality)
- [Configuration](#configuration)
  - [Require Minimal Configuration](#require-minimal-configuration)
  - [Provide Full Configuration](#provide-full-configuration)
  - [Store Configuration in the Repository](#store-configuration-in-the-repository)
- [Error Handling](#error-handling)
  - [Handle GitHub API Errors Gracefully](#handle-github-api-errors-gracefully)
  - [Don't Crash on Invalid Input](#dont-crash-on-invalid-input)
  - [Use Structured Exception Handling](#use-structured-exception-handling)
- [Performance](#performance)
  - [Avoid Unnecessary API Calls](#avoid-unnecessary-api-calls)
  - [Use Conditional Requests](#use-conditional-requests)
  - [Batch Operations](#batch-operations)
  - [Implement Async/Await Properly](#implement-asyncawait-properly)
- [Security](#security)
  - [Never Log Secrets](#never-log-secrets)
  - [Validate User Input](#validate-user-input)
  - [Check Bot Identity](#check-bot-identity)
  - [Respect Rate Limits](#respect-rate-limits)
  - [Use Secure Configuration](#use-secure-configuration)
- [Testing](#testing)
  - [Write Tests for Event Handlers](#write-tests-for-event-handlers)
  - [Test Configuration Loading](#test-configuration-loading)
  - [Test Edge Cases](#test-edge-cases)
  - [Use Property-Based Testing](#use-property-based-testing)
- [Deployment](#deployment)
  - [Use Environment Variables for Secrets](#use-environment-variables-for-secrets)
  - [Monitor Your App](#monitor-your-app)
  - [Provide Health Checks](#provide-health-checks)
  - [Provide Root Metadata Endpoint](#provide-root-metadata-endpoint)
  - [Implement Graceful Shutdown](#implement-graceful-shutdown)
- [Documentation](#documentation)
  - [Provide Clear README](#provide-clear-readme)
  - [Document Required Permissions](#document-required-permissions)
  - [Include Configuration Schema](#include-configuration-schema)
- [Community](#community)
  - [Be Responsive to Issues](#be-responsive-to-issues)
  - [Accept Contributions](#accept-contributions)
  - [Follow Semantic Versioning](#follow-semantic-versioning)
- [C#-Specific Best Practices](#c-specific-best-practices)
  - [Use Dependency Injection](#use-dependency-injection)
  - [Leverage ILogger for Structured Logging](#leverage-ilogger-for-structured-logging)
  - [Implement IDisposable When Needed](#implement-idisposable-when-needed)
  - [Use CancellationToken](#use-cancellationtoken)

---

## The Three Laws of Robotics

First and foremost, your app must obey the [The Three Laws of Robotics](https://en.wikipedia.org/wiki/Three_Laws_of_Robotics):

> 0. A robot may not harm humanity, or through inaction allow humanity to come to harm.
> 1. A robot may not injure a human being or, through inaction, allow a human being to come to harm.
> 2. A robot must obey the orders given to it by human beings except where such orders would conflict with the First Law.
> 3. A robot must protect its own existence as long as such protection does not conflict with the First or Second Laws.

Now that we agree that nobody will get hurt, here are some tips to make your app more effective.

---

## Empathy

Understanding and being aware of what another person is thinking or feeling is critical to healthy relationships. This is true for interactions with humans as well as apps, and it works both ways. Empathy enhances our ability to receive and process information, and it helps us communicate more effectively.

Think about how people will experience the interactions with your app.

### Avoid the Uncanny Valley

The [uncanny valley](https://en.wikipedia.org/wiki/Uncanny_valley) is the hypothesis that our emotional response to a robot becomes increasingly positive as it appears to be more human, until it becomes eerie and empathy quickly turns to revulsion. This area between a "barely human" and "fully human" is the uncanny valley.

Your app should be empathetic, but it shouldn't pretend to be human. It is an app and everyone that interacts with it knows that.

**Examples:**

```text
// ✅ GOOD: Clear, factual, professional
var comment = "Build failed with exit code 1:\n" +
              $"```\n{errorOutput}\n```\n\n" +
              "Please review the errors and push a fix.";

await context.GitHub.Issue.Comment.Create(
    context.Repository.Owner,
    context.Repository.Name,
    prNumber,
    comment);

// ❌ BAD: Overly friendly, pretends to be human
var comment = "Oh no! 😱 I'm so sorry, but it looks like your build failed! " +
              "I feel terrible about this, but don't worry, I know you'll fix it! " +
              "You're doing great! Have a fantastic day! 🌟✨";
```

**Key Guidelines:**
- Be concise and factual
- Avoid excessive emojis and exclamation marks
- Don't use first-person language that implies human emotions
- Focus on providing useful information, not personality

---

## Autonomy

### Never Take Bulk Actions Without Explicit Permission

Being installed on a repository is sufficient permission for responding to individual events, like replying to a single issue. However, an app **must** have explicit permission before performing bulk actions, like labeling all open issues or closing all stale pull requests.

**C# Example:**

```text
[EventHandler("installation", "created")]
public class InstallationHandler : IEventHandler
{
    private readonly IConfigService _configService;

    public InstallationHandler(IConfigService configService)
    {
        _configService = configService;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        // ❌ BAD: Don't automatically perform bulk actions on install
        // await LabelAllOpenIssues(context, ct);

        // ✅ GOOD: Wait for explicit configuration
        var config = await _configService.LoadConfigAsync(context, ct);

        if (config?.EnableInitialLabeling == true)
        {
            context.Logger.LogInformation(
                "Initial labeling enabled in config, processing {Count} issues",
                config.MaxIssuesToLabel);

            await LabelOpenIssues(context, config.MaxIssuesToLabel, ct);
        }
        else
        {
            context.Logger.LogInformation(
                "Skipping initial labeling - not enabled in .github/{ConfigFile}",
                config?.ConfigFileName ?? "app.yml");
        }
    }
}
```

**For example:** The [stale](https://github.com/probot/stale) app will only scan a target repository for stale issues and pull requests if `.github/stale.yml` exists in that repository.

### Include Dry-Run Functionality

Apps that perform destructive actions **must** offer a dry-run mode. Apps performing any automated actions **should** offer a dry-run mode. A dry run logs what actions would be taken without actually executing them.

**ProbotSharp** provides framework-level dry-run support via the `PROBOT_DRY_RUN` environment variable and `context.IsDryRun` property.

**Enable Dry-Run Mode:**

```bash
# Set environment variable
export PROBOT_DRY_RUN=true
```

When enabled, `context.IsDryRun` will be `true` and you can use the provided extension methods for automatic logging.

**Pattern 1: Manual Checks (Full Control)**

```text
public class StaleIssueHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        var staleIssues = await FindStaleIssues(context, ct);

        context.Logger.LogInformation(
            "Found {Count} stale issues. Dry run: {DryRun}",
            staleIssues.Count,
            context.IsDryRun);

        foreach (var issue in staleIssues)
        {
            if (context.IsDryRun)
            {
                context.Logger.LogInformation(
                    "[DRY-RUN] Would close issue #{Number}: {Title}",
                    issue.Number,
                    issue.Title);
            }
            else
            {
                context.Logger.LogInformation(
                    "Closing stale issue #{Number}: {Title}",
                    issue.Number,
                    issue.Title);

                // Post close comment
                await context.GitHub.Issue.Comment.Create(
                    context.Repository.Owner,
                    context.Repository.Name,
                    issue.Number,
                    "This issue has been closed due to inactivity");

                // Close the issue
                await context.GitHub.Issue.Update(
                    context.Repository.Owner,
                    context.Repository.Name,
                    issue.Number,
                    new IssueUpdate { State = ItemState.Closed });
            }
        }
    }
}
```

**Pattern 2: ExecuteOrLogAsync Helper (Cleaner)**

```text
public class StaleIssueHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        var staleIssues = await FindStaleIssues(context, ct);

        foreach (var issue in staleIssues)
        {
            await context.ExecuteOrLogAsync(
                $"Close stale issue #{issue.Number}",
                async () =>
                {
                    await context.GitHub.Issue.Comment.Create(
                        context.Repository.Owner,
                        context.Repository.Name,
                        issue.Number,
                        "This issue has been closed due to inactivity");

                    await context.GitHub.Issue.Update(
                        context.Repository.Owner,
                        context.Repository.Name,
                        issue.Number,
                        new IssueUpdate { State = ItemState.Closed });
                },
                new { issueNumber = issue.Number, title = issue.Title });
        }
    }
}
```

**Pattern 3: ThrowIfNotDryRun (Maximum Safety)**

For extremely dangerous operations:

```text
public class DangerousHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        // Ensure this can ONLY run in dry-run mode
        context.ThrowIfNotDryRun("Bulk deletion must only be tested in dry-run mode");

        // This code only executes in dry-run mode
        context.LogDryRun("Delete all old labels", new { count = 50 });
    }
}
```

**See Also:** [DryRunBot Example](../examples/DryRunBot/) for complete demonstrations of all three patterns.

**Configuration Example:**

```yaml
# .github/stale.yml
dryRun: true  # Start with dry run enabled, users must explicitly disable
daysUntilStale: 60
daysUntilClose: 7
closeComment: >
  This issue has been automatically closed due to inactivity.
  Please reopen if you believe this was closed in error.
```

---

## Configuration

### Require Minimal Configuration

Apps **should** provide sensible defaults for all settings. Users should be able to install your app and get value immediately without reading documentation or creating configuration files.

**C# Example:**

```text
public class AppConfig
{
    // Provide sensible defaults for all settings
    public bool EnableAutoLabeling { get; set; } = true;
    public int StaleIssueDays { get; set; } = 60;
    public string[] DefaultLabels { get; set; } = new[] { "needs-triage" };
    public bool DryRun { get; set; } = true; // Safe default
    public int MaxBulkOperations { get; set; } = 50;
    public string CloseComment { get; set; } =
        "This issue has been automatically closed. Please reopen if needed.";
}
```

### Provide Full Configuration

Apps **should** allow all settings to be customized per installation. What works for one repository may not work for another.

**C# Example:**

```yaml
# .github/mybot.yml - Full configuration options
# All settings are optional and have defaults

# Enable/disable features
enableAutoLabeling: true
enableStaleIssueClosing: false

# Behavior settings
dryRun: false  # Set to true to preview actions without executing
maxBulkOperations: 100

# Stale issue configuration
staleIssueDays: 90
staleIssueLabel: "stale"
closeComment: |
  This issue has been automatically closed after 90 days of inactivity.
  If you believe this was closed in error, please reopen or create a new issue.

# Label configuration
defaultLabels:
  - "needs-triage"
  - "awaiting-review"

# Exclude certain labels from automation
exemptLabels:
  - "pinned"
  - "security"
  - "do-not-close"
```

### Store Configuration in the Repository

Configuration **should** be stored in the `.github` directory of the target repository. This keeps configuration close to the code and allows teams to version and review changes.

**C# Example:**

```text
public class ConfigService
{
    private readonly string _configFileName;

    public ConfigService(string configFileName = "mybot.yml")
    {
        _configFileName = configFileName;
    }

    public async Task<AppConfig> LoadConfigAsync(
        ProbotSharpContext context,
        CancellationToken ct)
    {
        try
        {
            // Try loading from .github/mybot.yml
            var configPath = $".github/{_configFileName}";

            var configContent = await context.GitHub.Repository.Content.GetAllContentsByRef(
                context.Repository.Owner,
                context.Repository.Name,
                configPath,
                context.Payload["repository"]?["default_branch"]?.ToString() ?? "main");

            var yaml = configContent.First().Content;
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var config = deserializer.Deserialize<AppConfig>(yaml);

            context.Logger.LogInformation(
                "Loaded configuration from {Path}",
                configPath);

            return config ?? new AppConfig();
        }
        catch (NotFoundException)
        {
            // Fall back to defaults
            context.Logger.LogInformation(
                "No configuration file found at .github/{FileName}, using defaults",
                _configFileName);

            return new AppConfig();
        }
        catch (Exception ex)
        {
            context.Logger.LogError(
                ex,
                "Failed to load configuration from .github/{FileName}, using defaults",
                _configFileName);

            return new AppConfig();
        }
    }
}
```

**Support for Shared Configuration:**

You can also support the `_extends` pattern to share configuration across repositories:

```yaml
# .github/mybot.yml
_extends: .github  # Load from organization's .github repository
# Override specific values
dryRun: false
staleIssueDays: 120
```

---

## Error Handling

### Handle GitHub API Errors Gracefully

GitHub API calls can fail for many reasons: rate limits, permissions, network issues, or invalid data. Your app should handle these gracefully and provide useful feedback.

**C# Example:**

```text
public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
{
    var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>();

    if (!issueNumber.HasValue)
    {
        context.Logger.LogWarning("Issue number not found in payload");
        return;
    }

    try
    {
        await context.GitHub.Issue.Labels.AddToIssue(
            context.Repository.Owner,
            context.Repository.Name,
            issueNumber.Value,
            new[] { "bug" });

        context.Logger.LogInformation(
            "Added 'bug' label to issue #{Number}",
            issueNumber.Value);
    }
    catch (NotFoundException ex)
    {
        context.Logger.LogWarning(
            ex,
            "Issue #{Number} not found or was deleted",
            issueNumber.Value);
    }
    catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
        context.Logger.LogError(
            ex,
            "Insufficient permissions to add labels to issue #{Number}. " +
            "Check app permissions (requires 'issues: write')",
            issueNumber.Value);
    }
    catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
    {
        context.Logger.LogWarning(
            ex,
            "Label 'bug' does not exist in repository {Repo}. " +
            "Create the label or update configuration",
            context.GetRepositoryFullName());
    }
    catch (RateLimitExceededException ex)
    {
        context.Logger.LogError(
            ex,
            "Rate limit exceeded. Resets at {ResetTime}. " +
            "Consider implementing exponential backoff",
            ex.Reset.ToLocalTime());

        // Optionally queue for retry
        throw;
    }
    catch (Exception ex)
    {
        context.Logger.LogError(
            ex,
            "Unexpected error adding label to issue #{Number}: {Message}",
            issueNumber.Value,
            ex.Message);

        throw;
    }
}
```

### Don't Crash on Invalid Input

Event handlers should **never** throw unhandled exceptions that crash the app. Validate input and handle errors gracefully.

**C# Example:**

```text
public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
{
    try
    {
        // Validate required context
        if (context.Repository == null)
        {
            context.Logger.LogWarning(
                "No repository information in webhook payload for event {EventName}",
                context.EventName);
            return;
        }

        // Safely extract payload data with null checks
        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>();
        var issueTitle = context.Payload["issue"]?["title"]?.ToObject<string>();
        var authorLogin = context.Payload["issue"]?["user"]?["login"]?.ToObject<string>();

        if (!issueNumber.HasValue)
        {
            context.Logger.LogWarning(
                "Could not extract issue number from {EventName} payload",
                context.EventName);
            return;
        }

        // Proceed with processing...
        await ProcessIssue(context, issueNumber.Value, issueTitle, authorLogin, ct);
    }
    catch (Exception ex)
    {
        // Log the error but don't crash the app
        context.Logger.LogError(
            ex,
            "Failed to handle {EventName} event {EventId}: {Message}",
            context.EventName,
            context.Id,
            ex.Message);

        // Return gracefully instead of throwing
    }
}
```

### Use Structured Exception Handling

Use C#'s pattern matching and exception filters for cleaner error handling:

```text
try
{
    await PerformGitHubOperation(context, ct);
}
catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    context.Logger.LogWarning("Resource not found: {Message}", ex.Message);
}
catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
{
    context.Logger.LogError("Permission denied: {Message}", ex.Message);
}
catch (OperationCanceledException) when (ct.IsCancellationRequested)
{
    context.Logger.LogInformation("Operation cancelled");
    throw; // Re-throw cancellation to propagate properly
}
catch (Exception ex)
{
    context.Logger.LogError(ex, "Unexpected error");
    throw; // Re-throw unexpected exceptions
}
```

---

## Performance

### Avoid Unnecessary API Calls

Use the data in the webhook payload instead of making additional API calls when possible. The payload often contains all the information you need.

**C# Example:**

```text
public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
{
    // ❌ BAD: Making unnecessary API call
    var issue = await context.GitHub.Issue.Get(
        context.Repository.Owner,
        context.Repository.Name,
        issueNumber);
    var title = issue.Title;

    // ✅ GOOD: Use data from payload
    var title = context.Payload["issue"]?["title"]?.ToObject<string>();
    var author = context.Payload["issue"]?["user"]?["login"]?.ToObject<string>();
    var labels = context.Payload["issue"]?["labels"]?
        .Select(l => l["name"]?.ToObject<string>())
        .Where(l => l != null)
        .ToList();
}
```

### Use Conditional Requests

Cache resources and use ETags for conditional requests when fetching data that doesn't change often:

```text
public class ConfigCacheService
{
    private readonly Dictionary<string, (string ETag, AppConfig Config)> _cache = new();

    public async Task<AppConfig> GetConfigAsync(
        ProbotSharpContext context,
        CancellationToken ct)
    {
        var cacheKey = $"{context.Repository.Owner}/{context.Repository.Name}";

        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            // Use conditional request with ETag
            var request = new Request
            {
                Headers = { { "If-None-Match", cached.ETag } }
            };

            try
            {
                var response = await context.GitHub.Connection.Get<AppConfig>(
                    new Uri($"repos/{cacheKey}/contents/.github/mybot.yml"),
                    null,
                    request.Headers);

                // Update cache if modified
                if (response.HttpResponse.StatusCode == HttpStatusCode.OK)
                {
                    var etag = response.HttpResponse.Headers["ETag"].FirstOrDefault();
                    _cache[cacheKey] = (etag, response.Body);
                    return response.Body;
                }
            }
            catch (NotFoundException)
            {
                // File was deleted, remove from cache
                _cache.Remove(cacheKey);
                return new AppConfig();
            }

            // 304 Not Modified - use cached version
            return cached.Config;
        }

        // First request - fetch and cache
        // ... (fetch logic)

        return new AppConfig();
    }
}
```

### Batch Operations

Group API calls when possible to reduce network overhead:

```text
// ❌ BAD: Multiple separate API calls
foreach (var label in labelsToAdd)
{
    await context.GitHub.Issue.Labels.AddToIssue(owner, repo, issueNumber, new[] { label });
}

// ✅ GOOD: Single batched API call
await context.GitHub.Issue.Labels.AddToIssue(
    context.Repository.Owner,
    context.Repository.Name,
    issueNumber,
    labelsToAdd.ToArray());
```

### Implement Async/Await Properly

Follow C# async best practices:

```text
// ✅ GOOD: Proper async implementation
public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
{
    // Use ConfigureAwait(false) in library code to avoid deadlocks
    var config = await LoadConfigAsync(context, ct).ConfigureAwait(false);

    // Pass cancellation token to all async operations
    var issues = await FetchIssuesAsync(context, ct).ConfigureAwait(false);

    // Use Task.WhenAll for parallel operations
    var tasks = issues.Select(issue =>
        ProcessIssueAsync(context, issue, ct));
    await Task.WhenAll(tasks).ConfigureAwait(false);
}

// ❌ BAD: Blocking async code
public void HandleSync(ProbotSharpContext context)
{
    var result = LoadConfigAsync(context, CancellationToken.None).Result; // Deadlock risk!
}
```

---

## Security

### Never Log Secrets

Be extremely careful not to log sensitive information like tokens, private keys, or webhook secrets:

```text
// ✅ GOOD: Safe logging
context.Logger.LogInformation(
    "Processing webhook {EventName} for repository {Repo}",
    context.EventName,
    context.GetRepositoryFullName());

// ❌ BAD: Logs entire payload which may contain secrets
context.Logger.LogInformation(
    "Received payload: {Payload}",
    context.Payload.ToString());

// ✅ GOOD: Log specific safe fields
context.Logger.LogInformation(
    "Issue #{Number} opened by @{Author}",
    context.Payload["issue"]?["number"],
    context.Payload["issue"]?["user"]?["login"]);

// ❌ BAD: Logs authorization headers
context.Logger.LogDebug(
    "Request headers: {Headers}",
    httpContext.Request.Headers); // Contains authorization tokens!
```

**Use structured logging to prevent accidental secret exposure:**

```text
// Use log message templates with specific parameters
context.Logger.LogInformation(
    "Configuration loaded from {Path} with {SettingCount} settings",
    configPath,
    config.SettingCount);
```

### Validate User Input

Treat all user-provided content (comments, issue bodies, configuration) as untrusted input:

```text
public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
{
    var comment = context.Payload["comment"]?["body"]?.ToObject<string>();

    if (string.IsNullOrWhiteSpace(comment))
    {
        context.Logger.LogDebug("Ignoring empty comment");
        return;
    }

    // Validate length to prevent abuse
    if (comment.Length > 10_000)
    {
        context.Logger.LogWarning(
            "Comment exceeds maximum length ({Length} > 10000), truncating",
            comment.Length);
        comment = comment.Substring(0, 10_000);
    }

    // Sanitize input before using in API calls
    var sanitized = SanitizeInput(comment);

    // Be careful with regex on user input to prevent ReDoS attacks
    if (Regex.IsMatch(sanitized, @"^/label\s+(\w+)$", RegexOptions.None, TimeSpan.FromMilliseconds(100)))
    {
        // Process command...
    }
}

private static string SanitizeInput(string input)
{
    // Remove potentially dangerous characters
    return Regex.Replace(input, @"[^\w\s\-.,!?@]", string.Empty);
}
```

### Check Bot Identity

Prevent infinite loops by checking if the sender is a bot:

```text
public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
{
    // Always check at the beginning of handlers that create events
    if (context.IsBot())
    {
        context.Logger.LogDebug(
            "Ignoring {EventName} event from bot {BotName}",
            context.EventName,
            context.Payload["sender"]?["login"]?.ToObject<string>());
        return;
    }

    // Process event from human user...
}
```

**Additional bot checks:**

```text
// Check if the current app is the sender to prevent self-loops
public bool IsCurrentApp(ProbotSharpContext context, long appId)
{
    var senderAppId = context.Payload["sender"]?["id"]?.ToObject<long>();
    return senderAppId == appId;
}

// Check for specific bot types
public bool IsGitHubActionsBot(ProbotSharpContext context)
{
    var login = context.Payload["sender"]?["login"]?.ToObject<string>();
    return login == "github-actions[bot]";
}
```

### Respect Rate Limits

Monitor rate limit headers and implement backoff strategies:

```text
public class RateLimitService
{
    private readonly ILogger<RateLimitService> _logger;

    public async Task<T> ExecuteWithRateLimitAsync<T>(
        ProbotSharpContext context,
        Func<Task<T>> operation,
        CancellationToken ct)
    {
        try
        {
            return await operation();
        }
        catch (RateLimitExceededException ex)
        {
            _logger.LogWarning(
                "Rate limit exceeded. Limit: {Limit}, Remaining: {Remaining}, Resets: {Reset}",
                ex.Limit,
                ex.Remaining,
                ex.Reset);

            var waitTime = ex.Reset - DateTimeOffset.UtcNow;

            if (waitTime > TimeSpan.Zero && waitTime < TimeSpan.FromMinutes(10))
            {
                _logger.LogInformation(
                    "Waiting {Seconds}s for rate limit reset",
                    waitTime.TotalSeconds);

                await Task.Delay(waitTime, ct);
                return await operation();
            }

            throw; // Wait time too long, re-throw
        }
    }
}
```

### Use Secure Configuration

Store sensitive configuration in environment variables, not in repository files:

```text
// ✅ GOOD: Secrets from environment variables
var appId = Environment.GetEnvironmentVariable("GITHUB_APP_ID");
var privateKey = Environment.GetEnvironmentVariable("GITHUB_PRIVATE_KEY");
var webhookSecret = Environment.GetEnvironmentVariable("GITHUB_WEBHOOK_SECRET");

// ❌ BAD: Secrets in config file
// .github/mybot.yml:
// appId: 123456
// privateKey: "-----BEGIN RSA PRIVATE KEY-----\n..."
```

---

## Testing

### Write Tests for Event Handlers

Every event handler should have comprehensive tests:

```text
using NSubstitute;
using Xunit;

public class IssueOpenedHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenIssueOpened_AddsTriageLabel()
    {
        // Arrange
        var mockGitHub = Substitute.For<IGitHubClient>();
        var mockIssues = Substitute.For<IIssuesClient>();
        var mockLabels = Substitute.For<IIssuesLabelsClient>();

        mockGitHub.Issue.Returns(mockIssues);
        mockIssues.Labels.Returns(mockLabels);

        var context = CreateTestContext(
            eventName: "issues",
            action: "opened",
            payload: new JObject
            {
                ["issue"] = new JObject
                {
                    ["number"] = 42,
                    ["title"] = "Test issue"
                },
                ["repository"] = new JObject
                {
                    ["owner"] = new JObject { ["login"] = "testowner" },
                    ["name"] = "testrepo"
                }
            },
            gitHub: mockGitHub);

        var handler = new IssueOpenedHandler();

        // Act
        await handler.HandleAsync(context, CancellationToken.None);

        // Assert
        await mockLabels.Received(1).AddToIssue(
            "testowner",
            "testrepo",
            42,
            Arg.Is<string[]>(labels => labels.Contains("needs-triage")));
    }

    [Fact]
    public async Task HandleAsync_WhenSenderIsBot_DoesNotAddLabel()
    {
        // Arrange
        var mockGitHub = Substitute.For<IGitHubClient>();
        var context = CreateTestContext(
            eventName: "issues",
            action: "opened",
            payload: new JObject
            {
                ["sender"] = new JObject
                {
                    ["type"] = "Bot",
                    ["login"] = "github-actions[bot]"
                },
                ["issue"] = new JObject { ["number"] = 42 }
            },
            gitHub: mockGitHub);

        var handler = new IssueOpenedHandler();

        // Act
        await handler.HandleAsync(context, CancellationToken.None);

        // Assert
        await mockGitHub.Issue.Labels.DidNotReceive()
            .AddToIssue(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string[]>());
    }

    [Fact]
    public async Task HandleAsync_WhenPermissionDenied_LogsError()
    {
        // Arrange
        var mockGitHub = Substitute.For<IGitHubClient>();
        mockGitHub.Issue.Labels
            .AddToIssue(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string[]>())
            .ThrowsAsync(new ApiException("Forbidden", HttpStatusCode.Forbidden));

        var mockLogger = Substitute.For<ILogger>();
        var context = CreateTestContext(
            eventName: "issues",
            action: "opened",
            gitHub: mockGitHub,
            logger: mockLogger);

        var handler = new IssueOpenedHandler();

        // Act
        await handler.HandleAsync(context, CancellationToken.None);

        // Assert
        mockLogger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Insufficient permissions")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());
    }

    private ProbotSharpContext CreateTestContext(
        string eventName,
        string? action = null,
        JObject? payload = null,
        IGitHubClient? gitHub = null,
        ILogger? logger = null)
    {
        return new ProbotSharpContext(
            id: Guid.NewGuid().ToString(),
            eventName: eventName,
            eventAction: action,
            payload: payload ?? new JObject(),
            logger: logger ?? Substitute.For<ILogger>(),
            gitHub: gitHub ?? Substitute.For<IGitHubClient>(),
            repository: new RepositoryInfo("testowner", "testrepo"),
            installation: new InstallationInfo(12345));
    }
}
```

### Test Configuration Loading

Test configuration with various scenarios:

```text
public class ConfigServiceTests
{
    [Fact]
    public async Task LoadConfigAsync_WhenFileExists_ReturnsConfig()
    {
        // Arrange
        var mockContent = Substitute.For<IRepositoryContentsClient>();
        var configYaml = "dryRun: false\nstaleIssueDays: 90";
        var configFile = new RepositoryContent(
            name: "mybot.yml",
            path: ".github/mybot.yml",
            sha: "abc123",
            size: configYaml.Length,
            type: ContentType.File,
            downloadUrl: "https://example.com",
            url: "https://api.github.com",
            htmlUrl: "https://github.com",
            gitUrl: "https://api.github.com",
            content: Convert.ToBase64String(Encoding.UTF8.GetBytes(configYaml)),
            encoding: "base64");

        mockContent.GetAllContentsByRef(
            Arg.Any<string>(),
            Arg.Any<string>(),
            ".github/mybot.yml",
            Arg.Any<string>())
            .Returns(new[] { configFile });

        var mockGitHub = Substitute.For<IGitHubClient>();
        mockGitHub.Repository.Content.Returns(mockContent);

        var context = CreateTestContext(gitHub: mockGitHub);
        var service = new ConfigService();

        // Act
        var config = await service.LoadConfigAsync(context, CancellationToken.None);

        // Assert
        Assert.False(config.DryRun);
        Assert.Equal(90, config.StaleIssueDays);
    }

    [Fact]
    public async Task LoadConfigAsync_WhenFileNotFound_ReturnsDefaults()
    {
        // Arrange
        var mockContent = Substitute.For<IRepositoryContentsClient>();
        mockContent.GetAllContentsByRef(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>())
            .ThrowsAsync(new NotFoundException("Not found", HttpStatusCode.NotFound));

        var mockGitHub = Substitute.For<IGitHubClient>();
        mockGitHub.Repository.Content.Returns(mockContent);

        var context = CreateTestContext(gitHub: mockGitHub);
        var service = new ConfigService();

        // Act
        var config = await service.LoadConfigAsync(context, CancellationToken.None);

        // Assert - should return defaults
        Assert.NotNull(config);
        Assert.True(config.DryRun); // Default is true
        Assert.Equal(60, config.StaleIssueDays); // Default value
    }
}
```

### Test Edge Cases

Always test edge cases and error scenarios:

```text
[Theory]
[InlineData(null, null)] // Missing repository info
[InlineData("", "")] // Empty strings
[InlineData("owner", null)] // Partial info
public async Task HandleAsync_WithInvalidRepository_HandlesGracefully(
    string? owner,
    string? name)
{
    // Arrange
    var context = CreateTestContext(
        repository: owner != null && name != null
            ? new RepositoryInfo(owner, name)
            : null);
    var handler = new MyHandler();

    // Act & Assert - should not throw
    await handler.HandleAsync(context, CancellationToken.None);
}

[Fact]
public async Task HandleAsync_WhenCancelled_ThrowsOperationCancelledException()
{
    // Arrange
    var cts = new CancellationTokenSource();
    cts.Cancel();

    var context = CreateTestContext();
    var handler = new LongRunningHandler();

    // Act & Assert
    await Assert.ThrowsAsync<OperationCanceledException>(
        async () => await handler.HandleAsync(context, cts.Token));
}
```

### Use Property-Based Testing

For complex logic, use FsCheck or similar for property-based testing:

```text
using FsCheck;
using FsCheck.Xunit;

public class ValidationTests
{
    [Property]
    public Property SanitizeInput_AlwaysReturnsNonNullString(string input)
    {
        var result = InputValidator.SanitizeInput(input);
        return (result != null).ToProperty();
    }

    [Property]
    public Property SanitizeInput_RemovesDangerousCharacters(string input)
    {
        var result = InputValidator.SanitizeInput(input);
        var hasDangerousChars = result.Any(c => "<>\"'`".Contains(c));
        return (!hasDangerousChars).ToProperty();
    }
}
```

---

## Deployment

### Use Environment Variables for Secrets

Never commit credentials to source control. Use environment variables:

```text
// Startup.cs or Program.cs
var builder = WebApplication.CreateBuilder(args);

// Load from environment variables
var appId = builder.Configuration["GITHUB_APP_ID"]
    ?? throw new InvalidOperationException("GITHUB_APP_ID not set");

var privateKeyPath = builder.Configuration["GITHUB_PRIVATE_KEY_PATH"]
    ?? throw new InvalidOperationException("GITHUB_PRIVATE_KEY_PATH not set");

var webhookSecret = builder.Configuration["GITHUB_WEBHOOK_SECRET"]
    ?? throw new InvalidOperationException("GITHUB_WEBHOOK_SECRET not set");

// Or use .env file for local development (never commit!)
if (builder.Environment.IsDevelopment())
{
    DotNetEnv.Env.Load();
}
```

**.env.example** (commit this):
```bash
GITHUB_APP_ID=your_app_id
GITHUB_PRIVATE_KEY_PATH=/path/to/private-key.pem
GITHUB_WEBHOOK_SECRET=your_webhook_secret
ASPNETCORE_ENVIRONMENT=Development
```

**.env** (NEVER commit):
```bash
GITHUB_APP_ID=123456
GITHUB_PRIVATE_KEY_PATH=/home/user/.ssh/github-app.pem
GITHUB_WEBHOOK_SECRET=actual_secret_here
```

### Monitor Your App

Implement comprehensive monitoring and observability:

```text
public class MonitoredHandler : IEventHandler
{
    private readonly ILogger<MonitoredHandler> _logger;
    private readonly IMetricsPort _metrics;

    public MonitoredHandler(
        ILogger<MonitoredHandler> logger,
        IMetricsPort metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Processing {EventName}.{Action} for {Repo}",
                context.EventName,
                context.EventAction,
                context.GetRepositoryFullName());

            await ProcessEvent(context, ct);

            _metrics.IncrementCounter("events_processed", 1, new Dictionary<string, object>
            {
                ["event_name"] = context.EventName,
                ["event_action"] = context.EventAction ?? "none",
                ["status"] = "success"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process {EventName}.{Action}: {Message}",
                context.EventName,
                context.EventAction,
                ex.Message);

            _metrics.IncrementCounter("events_processed", 1, new Dictionary<string, object>
            {
                ["event_name"] = context.EventName,
                ["event_action"] = context.EventAction ?? "none",
                ["status"] = "error"
            });

            throw;
        }
        finally
        {
            stopwatch.Stop();

            _metrics.RecordHistogram(
                "event_processing_duration_ms",
                stopwatch.ElapsedMilliseconds,
                new Dictionary<string, object>
                {
                    ["event_name"] = context.EventName
                });
        }
    }
}
```

### Provide Health Checks

ProbotSharp includes `/health` endpoint by default, but you can add custom health checks:

```text
// Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<GitHubApiHealthCheck>("github_api")
    .AddCheck<ConfigurationHealthCheck>("configuration")
    .AddNpgSql(connectionString, name: "database")
    .AddRedis(redisConnection, name: "redis");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Custom health check
public class GitHubApiHealthCheck : IHealthCheck
{
    private readonly IGitHubClient _gitHub;

    public GitHubApiHealthCheck(IGitHubClient gitHub)
    {
        _gitHub = gitHub;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            // Simple API call to check connectivity
            await _gitHub.Meta.GetMetadata();

            return HealthCheckResult.Healthy("GitHub API is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "GitHub API is not accessible",
                ex);
        }
    }
}
```

### Provide Root Metadata Endpoint

A root endpoint (`/`) should provide application metadata and help users discover available endpoints:

```text
// Program.cs
app.MapGet("/", () => Results.Ok(new
{
    application = "MyGitHubBot",
    description = "Automated issue labeling and triage",
    version = "1.2.0",
    endpoints = new
    {
        webhooks = "/webhooks",
        health = "/health",
        metrics = "/metrics"
    },
    repository = "https://github.com/myorg/my-bot",
    documentation = "https://github.com/myorg/my-bot#readme"
}));
```

**Why this matters:**
- **Discoverability** - Users can explore your API without reading docs
- **Operations** - Monitoring tools can verify deployment versions
- **Debugging** - Quick way to confirm the right app/version is deployed
- **Standards** - Follows REST API best practices for resource discovery

### Implement Graceful Shutdown

Handle shutdown signals properly to finish processing in-flight events:

```text
// Program.cs
var app = builder.Build();

// Register shutdown handler
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStopping.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Application is shutting down, waiting for in-flight requests...");
});

lifetime.ApplicationStopped.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Application has stopped");
});

await app.RunAsync();
```

---

## Documentation

### Provide Clear README

Your app's README should include:

```markdown
# My Awesome Bot

Brief description of what your bot does.

## Features

- Feature 1
- Feature 2
- Feature 3

## Installation

1. [Create a GitHub App](https://github.com/settings/apps/new) with these settings:
   - **Permissions:**
     - Issues: Read & Write
     - Pull Requests: Read & Write
   - **Subscribe to events:**
     - Issues
     - Pull requests

2. Install the app on your repository

3. Create configuration file `.github/mybot.yml`:
   ```yaml
   dryRun: true
   enableAutoLabeling: true
   ```

## Configuration

All configuration options with defaults:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `dryRun` | boolean | `true` | Preview actions without executing |
| `enableAutoLabeling` | boolean | `true` | Automatically label new issues |
| `staleIssueDays` | number | `60` | Days before marking as stale |

## Usage

Example behavior when an issue is opened:
1. Checks if issue is from a bot (skips if true)
2. Adds "needs-triage" label
3. Posts welcome comment

## Development

See the repository documentation for development setup.

## License

MIT
```

### Document Required Permissions

Clearly list all required GitHub App permissions and why they're needed:

```markdown
## Required Permissions

This app requires the following GitHub App permissions:

| Permission | Access | Reason |
|------------|--------|--------|
| Issues | Read & Write | To label issues and post comments |
| Pull Requests | Read | To check PR status for auto-merge |
| Metadata | Read | Required for all apps |

## Required Webhook Events

Subscribe to these webhook events:

- `issues` - To handle issue events (opened, labeled, closed)
- `pull_request` - To handle PR events (opened, synchronize)
```

### Include Configuration Schema

Provide a complete configuration schema:

```markdown
## Configuration Schema

```yaml
# .github/mybot.yml

# Enable/disable features
enableAutoLabeling: true      # default: true
enableStaleIssueClosing: true # default: false

# Behavior settings
dryRun: false                 # default: true (preview mode)
maxBulkOperations: 100        # default: 50

# Stale issue settings
staleIssueDays: 90            # default: 60
staleIssueLabel: "stale"      # default: "stale"
closeComment: |               # default: generic message
  This issue has been closed due to inactivity.

# Label configuration
defaultLabels:                # default: ["needs-triage"]
  - "needs-triage"
  - "awaiting-review"

exemptLabels:                 # default: []
  - "pinned"
  - "security"
```
```

---

## Community

### Be Responsive to Issues

- Respond to issues and questions promptly
- Use issue templates to gather necessary information
- Be kind and welcoming to all contributors
- Thank people for their contributions

### Accept Contributions

- Provide development setup instructions in your documentation
- Review pull requests in a timely manner
- Provide constructive feedback
- Recognize and celebrate contributions

### Follow Semantic Versioning

Use [Semantic Versioning](https://semver.org/) for releases:

- **MAJOR** version for incompatible API changes
- **MINOR** version for backwards-compatible functionality
- **PATCH** version for backwards-compatible bug fixes

```markdown
## Changelog

### [2.1.0] - 2025-10-05
#### Added
- Support for custom label colors
- Dry-run mode for all destructive actions

#### Changed
- Improved error messages for permission issues

#### Fixed
- Bug where bot would respond to its own comments

### [2.0.0] - 2025-09-15
#### Breaking Changes
- Configuration file moved from `.github/config.yml` to `.github/mybot.yml`
- `enableLabeling` renamed to `enableAutoLabeling`
```

---

## C#-Specific Best Practices

### Use Dependency Injection

Leverage ASP.NET Core's built-in dependency injection:

```text
// Register services in Program.cs
builder.Services.AddSingleton<IConfigService, ConfigService>();
builder.Services.AddScoped<IIssueProcessor, IssueProcessor>();
builder.Services.AddTransient<IEventHandler, IssueOpenedHandler>();

// Use constructor injection in handlers
public class IssueOpenedHandler : IEventHandler
{
    private readonly IConfigService _configService;
    private readonly IIssueProcessor _processor;
    private readonly ILogger<IssueOpenedHandler> _logger;

    public IssueOpenedHandler(
        IConfigService configService,
        IIssueProcessor processor,
        ILogger<IssueOpenedHandler> logger)
    {
        _configService = configService;
        _processor = processor;
        _logger = logger;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        var config = await _configService.LoadConfigAsync(context, ct);
        await _processor.ProcessAsync(context, config, ct);
    }
}
```

### Leverage ILogger for Structured Logging

Use `ILogger<T>` with structured logging:

```text
public class MyHandler : IEventHandler
{
    private readonly ILogger<MyHandler> _logger;

    public MyHandler(ILogger<MyHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        // ✅ GOOD: Structured logging with named parameters
        _logger.LogInformation(
            "Processing issue #{IssueNumber} in {Repository} by @{Author}",
            issueNumber,
            context.GetRepositoryFullName(),
            authorLogin);

        // ❌ BAD: String interpolation loses structure
        _logger.LogInformation(
            $"Processing issue #{issueNumber} in {context.GetRepositoryFullName()}");

        // ✅ GOOD: Different log levels
        _logger.LogDebug("Detailed debug information for {Event}", context.EventName);
        _logger.LogInformation("Normal operational message");
        _logger.LogWarning("Warning about {Issue}", "something unusual");
        _logger.LogError(exception, "Error processing {Event}", context.EventName);
    }
}
```

### Implement IDisposable When Needed

Properly manage resources with `IDisposable`:

```text
public class ResourceHandler : IEventHandler, IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public ResourceHandler()
    {
        _httpClient = new HttpClient();
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ResourceHandler));
        }

        // Use resources...
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _httpClient?.Dispose();
        }

        _disposed = true;
    }
}

// Better: Use dependency injection to manage lifetime
builder.Services.AddHttpClient<IEventHandler, MyHandler>();
```

### Use CancellationToken

Always accept and propagate `CancellationToken`:

```text
public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
{
    // Pass cancellation token to all async operations
    var config = await LoadConfigAsync(context, ct);

    // Check for cancellation in loops
    foreach (var item in items)
    {
        ct.ThrowIfCancellationRequested();
        await ProcessItemAsync(item, ct);
    }

    // Use cancellation token with timeout
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    cts.CancelAfter(TimeSpan.FromSeconds(30));

    await LongRunningOperationAsync(cts.Token);
}
```

**ConfigureAwait guidance:**

```text
// In library code (event handlers, services):
// Use ConfigureAwait(false) to avoid capturing sync context
var result = await SomeOperationAsync().ConfigureAwait(false);

// In application code (ASP.NET Core controllers, minimal APIs):
// Don't use ConfigureAwait - let the framework manage context
var result = await SomeOperationAsync();
```

---

## Additional Resources

- [Probot Documentation](https://probot.github.io/docs/)
- [GitHub Apps Documentation](https://docs.github.com/en/developers/apps)
- [Octokit.NET Documentation](https://octokitnet.readthedocs.io/)
- [ASP.NET Core Best Practices](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/best-practices)
- [ProbotSharp Architecture Guide](Architecture.md)

## Pre-Release Checklist

Before publishing your app, review the [Best Practices Checklist](BestPractices-Checklist.md).
