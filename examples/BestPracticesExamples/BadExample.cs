// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Octokit;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Context;

namespace BestPracticesExamples;

/// <summary>
/// BAD EXAMPLE: Demonstrates common anti-patterns and mistakes.
/// DO NOT USE THIS CODE - it's intentionally wrong to illustrate what to avoid.
/// See GoodExample.cs for the correct approach.
/// </summary>
[EventHandler("installation", "created")]
public class BadInstallationHandler : IEventHandler
{
    // ❌ BAD: No dependency injection, creates logger directly
    private readonly ILogger _logger = LoggerFactory.Create(builder => builder.AddConsole())
        .CreateLogger<BadInstallationHandler>();

    // ❌ BAD: No CancellationToken parameter
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        try
        {
            // ❌ BAD: Logs entire payload which may contain secrets
            _logger.LogInformation($"Received payload: {context.Payload}");

            // ❌ BAD: Performs bulk actions without explicit permission
            var issues = await context.GitHub.Issue.GetAllForRepository(
                context.Repository.Owner,
                context.Repository.Name);

            // ❌ BAD: No dry-run mode, immediately takes destructive actions
            foreach (var issue in issues)
            {
                // ❌ BAD: No cancellation token support in loop
                await context.GitHub.Issue.Labels.AddToIssue(
                    context.Repository.Owner,
                    context.Repository.Name,
                    issue.Number,
                    new[] { "auto-labeled" });

                // ❌ BAD: Multiple API calls in a loop instead of batching
                await context.GitHub.Issue.Comment.Create(
                    context.Repository.Owner,
                    context.Repository.Name,
                    issue.Number,
                    "Hi there! 😊 I'm so excited to be installed! 🎉 I went ahead and labeled all your issues!");
            }

            // ❌ BAD: String interpolation instead of structured logging
            _logger.LogInformation($"Labeled {issues.Count} issues on installation");
        }
        catch (Exception ex)
        {
            // ❌ BAD: Swallows exception without logging
            // Silent failures make debugging impossible
        }
    }
}

/// <summary>
/// BAD EXAMPLE: Stale issue handler with multiple problems.
/// </summary>
[EventHandler("schedule", "daily")]
public class BadStaleIssueHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        // ❌ BAD: No try-catch, will crash the app on error
        // ❌ BAD: Hard-coded configuration, no customization
        var staleDate = DateTimeOffset.UtcNow.AddDays(-30);

        var issues = await context.GitHub.Issue.GetAllForRepository(
            context.Repository.Owner,
            context.Repository.Name);

        // ❌ BAD: Uses blocking .Result instead of await
        var config = LoadConfigAsync(context).Result; // Deadlock risk!

        foreach (var issue in issues)
        {
            // ❌ BAD: No check for cancellation in long-running loop
            // ❌ BAD: No validation that issue.UpdatedAt exists
            if (issue.UpdatedAt < staleDate)
            {
                // ❌ BAD: No dry-run mode, immediately closes issues
                // ❌ BAD: Overly friendly, pretends to be human
                var comment = "Oh no! 😢 I'm so so sorry, but I have to close this issue because it's been inactive! " +
                             "I feel really bad about this! I hope you're not upset with me! Have a wonderful day! ✨🌟💖";

                await context.GitHub.Issue.Comment.Create(
                    context.Repository.Owner,
                    context.Repository.Name,
                    issue.Number,
                    comment);

                await context.GitHub.Issue.Update(
                    context.Repository.Owner,
                    context.Repository.Name,
                    issue.Number,
                    new IssueUpdate { State = ItemState.Closed });
            }
        }
    }

    // ❌ BAD: No cancellation token parameter
    private async Task<object> LoadConfigAsync(ProbotSharpContext context)
    {
        // ❌ BAD: No error handling, will crash if file doesn't exist
        var config = await context.GitHub.Repository.Content.GetAllContentsByRef(
            context.Repository.Owner,
            context.Repository.Name,
            ".github/stale.yml",
            "main");

        // ❌ BAD: Returns object instead of strongly-typed config
        return config;
    }
}

/// <summary>
/// BAD EXAMPLE: Issue comment handler with security and validation issues.
/// </summary>
[EventHandler("issue_comment", "created")]
public class BadCommentHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        // ❌ BAD: No bot check - will respond to its own comments (infinite loop!)
        // ❌ BAD: No null checks, will throw NullReferenceException
        var comment = context.Payload["comment"]["body"].ToString();
        var issueNumber = (int)context.Payload["issue"]["number"];

        // ❌ BAD: No input validation or length limits
        // ❌ BAD: Unsafe regex without timeout (ReDoS vulnerability)
        if (System.Text.RegularExpressions.Regex.IsMatch(comment, @"^/delete\s+.*"))
        {
            // ❌ BAD: Dangerous command with no permission checks
            var repo = context.Payload["repository"]["full_name"].ToString();

            // ❌ BAD: Logs potentially sensitive data
            Console.WriteLine($"Deleting issue: {issueNumber} in {repo} - Comment: {comment}");

            // ❌ BAD: No error handling, will crash on permission denied
            await context.GitHub.Issue.Update(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber,
                new IssueUpdate { State = ItemState.Closed });
        }
    }
}

/// <summary>
/// BAD EXAMPLE: Configuration with no defaults and poor error handling.
/// </summary>
public class BadConfigService
{
    // ❌ BAD: No error handling, no cancellation token
    public async Task<BadConfig> LoadConfigAsync(ProbotSharpContext context)
    {
        // ❌ BAD: No try-catch, will crash if file doesn't exist
        var content = await context.GitHub.Repository.Content.GetAllContentsByRef(
            context.Repository.Owner,
            context.Repository.Name,
            ".github/config.yml",
            "main"); // ❌ BAD: Hard-coded branch name

        var yaml = content.First().Content;

        // ❌ BAD: No error handling for invalid YAML
        var config = YamlDotNet.Serialization.Deserializer.Deserialize<BadConfig>(yaml);

        // ❌ BAD: No logging about what was loaded
        return config;
    }
}

/// <summary>
/// BAD EXAMPLE: Configuration class with no defaults.
/// </summary>
public class BadConfig
{
    // ❌ BAD: No default values - users must configure everything
    public bool DryRun { get; set; }
    public int StaleIssueDays { get; set; }
    public string[] Labels { get; set; }
    public string Comment { get; set; }

    // ❌ BAD: Nullable properties with no null handling
    public string? OptionalSetting { get; set; }
}

/// <summary>
/// BAD EXAMPLE: Handler with poor error handling.
/// </summary>
[EventHandler("issues", "opened")]
public class BadErrorHandlingHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        try
        {
            // ❌ BAD: No validation that repository exists
            var owner = context.Repository.Owner; // NullReferenceException if Repository is null!

            // ❌ BAD: No null check on payload values
            var issueNumber = context.Payload["issue"]["number"].Value<int>();

            await context.GitHub.Issue.Labels.AddToIssue(
                owner,
                context.Repository.Name,
                issueNumber,
                new[] { "bug" });
        }
        catch (Exception)
        {
            // ❌ BAD: Catches all exceptions but does nothing
            // No logging, no re-throw, errors are silently swallowed
        }
    }
}

/// <summary>
/// BAD EXAMPLE: Handler with performance issues.
/// </summary>
[EventHandler("pull_request", "opened")]
public class BadPerformanceHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        // ❌ BAD: Makes unnecessary API call when data is in payload
        var pr = await context.GitHub.PullRequest.Get(
            context.Repository.Owner,
            context.Repository.Name,
            context.Payload["pull_request"]["number"].Value<int>());

        var title = pr.Title; // ❌ Could have used context.Payload["pull_request"]["title"]

        // ❌ BAD: Multiple separate API calls instead of batching
        var labels = new[] { "needs-review", "automated", "priority-low" };
        foreach (var label in labels)
        {
            await context.GitHub.Issue.Labels.AddToIssue(
                context.Repository.Owner,
                context.Repository.Name,
                pr.Number,
                new[] { label }); // Should batch all labels in one call
        }

        // ❌ BAD: Makes synchronous call in async method
        Thread.Sleep(5000); // Blocks the thread!

        // ❌ BAD: Uses .Result (can cause deadlocks)
        var comments = context.GitHub.Issue.Comment.GetAllForIssue(
            context.Repository.Owner,
            context.Repository.Name,
            pr.Number).Result;
    }
}

/// <summary>
/// BAD EXAMPLE: Handler with security issues.
/// </summary>
[EventHandler("issue_comment", "created")]
public class BadSecurityHandler : IEventHandler
{
    private readonly ILogger<BadSecurityHandler> _logger;

    public BadSecurityHandler(ILogger<BadSecurityHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        var comment = context.Payload["comment"]?["body"]?.ToString();

        // ❌ BAD: Logs user input without sanitization
        _logger.LogInformation($"Processing comment: {comment}");

        // ❌ BAD: No input validation - user can inject malicious content
        var response = $"You said: {comment}";

        // ❌ BAD: No length limit - user can cause memory issues with huge comment
        await context.GitHub.Issue.Comment.Create(
            context.Repository.Owner,
            context.Repository.Name,
            context.Payload["issue"]["number"].Value<int>(),
            response);

        // ❌ BAD: Logs entire context which may contain tokens
        _logger.LogDebug($"Context: {System.Text.Json.JsonSerializer.Serialize(context)}");

        // ❌ BAD: Logs authorization header
        var token = context.GitHub.Connection.Credentials.GetToken();
        _logger.LogDebug($"Using token: {token}"); // Exposes secret!
    }
}

/// <summary>
/// BAD EXAMPLE: Handler that doesn't respect async/await best practices.
/// </summary>
[EventHandler("push")]
public class BadAsyncHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        // ❌ BAD: Async void method (fire and forget, can't handle errors)
        ProcessAsync(context);

        // ❌ BAD: Task.Run in ASP.NET Core context (unnecessary overhead)
        await Task.Run(async () =>
        {
            await DoWorkAsync(context);
        });

        // ❌ BAD: Multiple awaits without ConfigureAwait in library code
        var result1 = await SlowOperationAsync(); // Captures sync context
        var result2 = await AnotherSlowOperationAsync(); // Captures sync context

        // ❌ BAD: Using Task.WaitAll with async tasks
        var tasks = new[] { Task1(), Task2(), Task3() };
        Task.WaitAll(tasks); // Blocks the thread!
    }

    // ❌ BAD: Async void instead of async Task
    private async void ProcessAsync(ProbotSharpContext context)
    {
        await DoWorkAsync(context);
        // Any exceptions here will crash the app!
    }

    private Task SlowOperationAsync() => Task.Delay(1000);
    private Task AnotherSlowOperationAsync() => Task.Delay(1000);
    private Task Task1() => Task.CompletedTask;
    private Task Task2() => Task.CompletedTask;
    private Task Task3() => Task.CompletedTask;
    private Task DoWorkAsync(ProbotSharpContext context) => Task.CompletedTask;
}

/// <summary>
/// BAD EXAMPLE: Resource management issues.
/// </summary>
public class BadResourceHandler : IEventHandler
{
    // ❌ BAD: Creates HttpClient per instance (socket exhaustion)
    private readonly HttpClient _httpClient = new HttpClient();

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        // ❌ BAD: Creates new instance each time (should use DI)
        using var client = new HttpClient();

        // ❌ BAD: No timeout set (can hang indefinitely)
        var response = await client.GetAsync("https://api.example.com/slow-endpoint");

        // ❌ BAD: Doesn't dispose of resources
        var stream = await response.Content.ReadAsStreamAsync();
        // Stream is never disposed!

        // ❌ BAD: No error handling for HTTP errors
        var content = await response.Content.ReadAsStringAsync();
    }

    // ❌ BAD: No IDisposable implementation to clean up HttpClient
}

/// <summary>
/// Summary of Anti-Patterns Demonstrated:
///
/// 1. EMPATHY VIOLATIONS:
///    - Overly friendly, pretends to be human
///    - Uses excessive emojis and exclamation marks
///
/// 2. AUTONOMY VIOLATIONS:
///    - Bulk actions without permission
///    - No dry-run mode
///    - Destructive actions on installation
///
/// 3. CONFIGURATION ISSUES:
///    - No defaults, requires full configuration
///    - Hard-coded values
///    - Poor error handling for missing config
///
/// 4. ERROR HANDLING ISSUES:
///    - No try-catch blocks
///    - Swallowing exceptions
///    - No validation of inputs
///    - Will crash on null values
///
/// 5. PERFORMANCE ISSUES:
///    - Unnecessary API calls
///    - No batching
///    - Blocking async code
///    - Multiple sequential calls instead of parallel
///
/// 6. SECURITY ISSUES:
///    - No bot checks (infinite loops)
///    - Logs secrets and tokens
///    - No input validation
///    - ReDoS vulnerabilities
///    - No length limits
///
/// 7. C# SPECIFIC ISSUES:
///    - No dependency injection
///    - Blocking with .Result and .Wait()
///    - No ConfigureAwait in library code
///    - Async void methods
///    - Poor resource management
///    - No cancellation token usage
///
/// See GoodExample.cs for the correct implementations of all these patterns.
/// </summary>
