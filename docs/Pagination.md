# Pagination in ProbotSharp

## Overview

Pagination is essential when working with GitHub's API, as most list endpoints return results in pages rather than all at once. This document explains how to handle pagination in ProbotSharp using Octokit.NET's built-in pagination support and provides patterns for common scenarios.

**Key Concepts:**
- Octokit.NET automatically handles pagination through `GetAll*` methods
- Use `ApiOptions` to control page size, page count, and starting page
- ProbotSharp provides convenience extensions for common pagination patterns
- Early exit strategies can improve performance when you don't need all results

## Table of Contents

1. [Basic Pagination with GetAll Methods](#basic-pagination-with-getall-methods)
2. [Using ApiOptions for Pagination Control](#using-apioptions-for-pagination-control)
3. [Context Helper Extensions](#context-helper-extensions)
4. [Early Exit Patterns with LINQ](#early-exit-patterns-with-linq)
5. [Performance Best Practices](#performance-best-practices)
6. [Comparison with Node.js Probot](#comparison-with-nodejs-probot)
7. [Common Pagination Scenarios](#common-pagination-scenarios)
8. [Rate Limiting Considerations](#rate-limiting-considerations)

---

## Basic Pagination with GetAll Methods

Octokit.NET provides `GetAll*` methods that automatically handle pagination for you. These methods fetch all pages of results and return them as a single collection.

### Example: Get All Issues

```text
public class IssueListenerApp : IProbotApp
{
    public void Initialize(IProbot probot)
    {
        probot.On("issues.opened", HandleIssueOpened);
    }

    private async Task HandleIssueOpened(ProbotSharpContext context)
    {
        // Extract repository information
        var (owner, repo) = context.Repo();

        // Get all issues for the repository
        var allIssues = await context.GitHub.Issue.GetAllForRepository(owner, repo);

        context.Logger.LogInformation(
            "Repository {Owner}/{Repo} has {Count} issues",
            owner, repo, allIssues.Count);
    }
}
```

### Example: Get All Pull Requests

```text
private async Task HandlePullRequestOpened(ProbotSharpContext context)
{
    var (owner, repo) = context.Repo();

    // Get all pull requests
    var allPullRequests = await context.GitHub.PullRequest.GetAllForRepository(owner, repo);

    // Filter open PRs
    var openPRs = allPullRequests.Where(pr => pr.State == ItemState.Open).ToList();

    context.Logger.LogInformation(
        "Found {OpenCount} open PRs out of {TotalCount} total",
        openPRs.Count, allPullRequests.Count);
}
```

### Example: Get All Comments

```text
private async Task HandleIssueComment(ProbotSharpContext context)
{
    var (owner, repo, issueNumber) = context.Issue();

    // Get all comments for the issue
    var allComments = await context.GitHub.Issue.Comment.GetAllForIssue(
        owner, repo, issueNumber);

    context.Logger.LogInformation(
        "Issue #{Number} has {Count} comments",
        issueNumber, allComments.Count);
}
```

---

## Using ApiOptions for Pagination Control

Use `ApiOptions` to control pagination behavior when you don't need all results or want to process results incrementally.

### ApiOptions Properties

- **PageSize**: Number of items per page (default: 30, max: 100)
- **PageCount**: Number of pages to fetch (default: all pages)
- **StartPage**: The page number to start from (default: 1)

### Example: Get First 100 Issues

```text
private async Task GetRecentIssues(ProbotSharpContext context)
{
    var (owner, repo) = context.Repo();

    // Get only the first 100 issues (1 page of 100 items)
    var options = new ApiOptions
    {
        PageSize = 100,
        PageCount = 1
    };

    var recentIssues = await context.GitHub.Issue.GetAllForRepository(
        owner, repo, options);

    context.Logger.LogInformation(
        "Retrieved {Count} most recent issues",
        recentIssues.Count);
}
```

### Example: Get Issues in Batches

```text
private async Task ProcessIssuesInBatches(ProbotSharpContext context)
{
    var (owner, repo) = context.Repo();
    int pageNumber = 1;
    const int pageSize = 50;

    while (true)
    {
        var options = new ApiOptions
        {
            PageSize = pageSize,
            StartPage = pageNumber,
            PageCount = 1
        };

        var issues = await context.GitHub.Issue.GetAllForRepository(
            owner, repo, options);

        if (issues.Count == 0)
        {
            break; // No more issues
        }

        // Process this batch
        foreach (var issue in issues)
        {
            context.Logger.LogInformation("Processing issue #{Number}", issue.Number);
            // Do work with issue
        }

        pageNumber++;

        // Optional: Add delay to avoid rate limiting
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
}
```

### Example: Get Specific Page Range

```text
private async Task GetMiddlePages(ProbotSharpContext context)
{
    var (owner, repo) = context.Repo();

    // Get pages 5-7 (3 pages starting from page 5)
    var options = new ApiOptions
    {
        PageSize = 30,
        StartPage = 5,
        PageCount = 3
    };

    var issues = await context.GitHub.Issue.GetAllForRepository(
        owner, repo, options);

    context.Logger.LogInformation(
        "Retrieved {Count} issues from pages 5-7",
        issues.Count);
}
```

---

## Context Helper Extensions

ProbotSharp provides convenience extension methods on `ProbotSharpContext` that automatically extract repository information from the webhook payload.

### GetAllIssuesAsync

```text
// Simple usage - get all issues
var allIssues = await context.GetAllIssuesAsync();

// With pagination options
var options = new ApiOptions { PageSize = 100, PageCount = 1 };
var recentIssues = await context.GetAllIssuesAsync(options);
```

### GetAllPullRequestsAsync

```text
// Simple usage - get all pull requests
var allPRs = await context.GetAllPullRequestsAsync();

// Get first 50 PRs
var options = new ApiOptions { PageSize = 50, PageCount = 1 };
var firstBatch = await context.GetAllPullRequestsAsync(options);
```

### GetAllCommentsAsync

```text
// Get all comments for a specific issue
var (owner, repo, number) = context.Issue();
var allComments = await context.GetAllCommentsAsync(number);

// Get first 20 comments
var options = new ApiOptions { PageSize = 20, PageCount = 1 };
var recentComments = await context.GetAllCommentsAsync(number, options);
```

### GetAllInstallationRepositoriesAsync

```text
// Get all repositories for the installation
var allRepos = await context.GetAllInstallationRepositoriesAsync();

foreach (var repo in allRepos)
{
    context.Logger.LogInformation("Found repository: {FullName}", repo.FullName);
}
```

---

## Early Exit Patterns with LINQ

When you need to find specific items without fetching all results, consider these patterns.

### Pattern 1: Find First Match

```text
private async Task FindBugLabel(ProbotSharpContext context)
{
    // Fetch all issues (or use ApiOptions to limit)
    var allIssues = await context.GetAllIssuesAsync();

    // Use LINQ to find the first match
    var firstBugIssue = allIssues.FirstOrDefault(issue =>
        issue.Labels.Any(label => label.Name == "bug"));

    if (firstBugIssue != null)
    {
        context.Logger.LogInformation(
            "Found first bug issue: #{Number}",
            firstBugIssue.Number);
    }
}
```

### Pattern 2: Take Limited Results

```text
private async Task ProcessRecentIssues(ProbotSharpContext context)
{
    // Get all issues sorted by creation date
    var allIssues = await context.GetAllIssuesAsync();

    // Take only the 10 most recent
    var recentIssues = allIssues
        .OrderByDescending(i => i.CreatedAt)
        .Take(10)
        .ToList();

    foreach (var issue in recentIssues)
    {
        // Process each recent issue
        await ProcessIssue(context, issue);
    }
}
```

### Pattern 3: Conditional Processing with Break

```text
private async Task ProcessUntilCondition(ProbotSharpContext context)
{
    var (owner, repo) = context.Repo();
    int pageNumber = 1;
    bool foundTarget = false;

    while (!foundTarget)
    {
        var options = new ApiOptions
        {
            PageSize = 50,
            StartPage = pageNumber,
            PageCount = 1
        };

        var issues = await context.GitHub.Issue.GetAllForRepository(
            owner, repo, options);

        if (issues.Count == 0)
        {
            break; // No more issues
        }

        foreach (var issue in issues)
        {
            if (issue.Title.Contains("BREAKING CHANGE"))
            {
                context.Logger.LogInformation(
                    "Found breaking change in issue #{Number}",
                    issue.Number);
                foundTarget = true;
                break;
            }
        }

        pageNumber++;
    }
}
```

---

## Performance Best Practices

### 1. Use Appropriate Page Sizes

```text
// Good: Use larger page sizes to reduce API calls
var options = new ApiOptions { PageSize = 100 }; // Max allowed

// Avoid: Small page sizes increase API calls
var options = new ApiOptions { PageSize = 10 }; // Too small
```

### 2. Limit Results When Possible

```text
// Good: Only fetch what you need
var options = new ApiOptions
{
    PageSize = 100,
    PageCount = 1 // Only get first 100
};
var recentIssues = await context.GetAllIssuesAsync(options);

// Avoid: Fetching all results when you only need a few
var allIssues = await context.GetAllIssuesAsync(); // May fetch thousands
var first10 = allIssues.Take(10).ToList();
```

### 3. Use Filters at the API Level

```text
// Good: Filter at the API level using request parameters
var request = new RepositoryIssueRequest
{
    State = ItemStateFilter.Open,
    Labels = new List<string> { "bug" }
};
var openBugs = await context.GitHub.Issue.GetAllForRepository(owner, repo, request);

// Less efficient: Fetch all and filter in memory
var allIssues = await context.GetAllIssuesAsync();
var openBugs = allIssues.Where(i => i.State == ItemState.Open).ToList();
```

### 4. Cache Results When Appropriate

```text
private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

private async Task<IReadOnlyList<Issue>> GetCachedIssues(ProbotSharpContext context)
{
    var (owner, repo) = context.Repo();
    var cacheKey = $"issues:{owner}/{repo}";

    if (_cache.TryGetValue(cacheKey, out IReadOnlyList<Issue> cached))
    {
        return cached;
    }

    var issues = await context.GetAllIssuesAsync();

    _cache.Set(cacheKey, issues, TimeSpan.FromMinutes(5));

    return issues;
}
```

### 5. Process Incrementally for Large Datasets

```text
private async Task ProcessLargeDataset(ProbotSharpContext context)
{
    var (owner, repo) = context.Repo();
    int pageNumber = 1;
    int processedCount = 0;

    while (true)
    {
        var options = new ApiOptions
        {
            PageSize = 100,
            StartPage = pageNumber,
            PageCount = 1
        };

        var issues = await context.GitHub.Issue.GetAllForRepository(
            owner, repo, options);

        if (issues.Count == 0)
        {
            break;
        }

        // Process each issue immediately
        foreach (var issue in issues)
        {
            await ProcessIssueAsync(context, issue);
            processedCount++;
        }

        context.Logger.LogInformation(
            "Processed {Count} issues so far...",
            processedCount);

        pageNumber++;

        // Add delay to respect rate limits
        await Task.Delay(TimeSpan.FromSeconds(1));
    }

    context.Logger.LogInformation(
        "Completed processing {Total} issues",
        processedCount);
}
```

---

## Comparison with Node.js Probot

### Node.js Probot Pattern

```javascript
// Node.js uses octokit.paginate
const allIssues = await context.octokit.paginate(
  context.octokit.issues.listForRepo,
  context.repo(),
  (response) => response.data
);

// With options
const first100 = await context.octokit.paginate(
  context.octokit.issues.listForRepo,
  { ...context.repo(), per_page: 100 },
  (response, done) => {
    if (response.data.length === 100) done();
    return response.data;
  }
);
```

### ProbotSharp Equivalent

```text
// ProbotSharp uses Octokit.NET's GetAll methods
var allIssues = await context.GetAllIssuesAsync();

// With options
var options = new ApiOptions
{
    PageSize = 100,
    PageCount = 1
};
var first100 = await context.GetAllIssuesAsync(options);

// Or without helper extension
var (owner, repo) = context.Repo();
var allIssues = await context.GitHub.Issue.GetAllForRepository(owner, repo);
```

### Key Differences

| Feature | Node.js Probot | ProbotSharp |
|---------|----------------|--------------|
| **Pagination Method** | `octokit.paginate` | `GetAll*` methods |
| **Automatic Fetching** | Yes (via callback) | Yes (built-in) |
| **Page Control** | Via callback | Via `ApiOptions` |
| **Early Exit** | `done()` callback | Page count limits + LINQ |
| **Type Safety** | Runtime | Compile-time |

---

## Common Pagination Scenarios

### Scenario 1: Audit All Issues

```text
public async Task AuditAllIssues(ProbotSharpContext context)
{
    var allIssues = await context.GetAllIssuesAsync();

    var stats = new
    {
        Total = allIssues.Count,
        Open = allIssues.Count(i => i.State == ItemState.Open),
        Closed = allIssues.Count(i => i.State == ItemState.Closed),
        WithLabels = allIssues.Count(i => i.Labels.Any()),
        WithAssignees = allIssues.Count(i => i.Assignees.Any())
    };

    context.Logger.LogInformation(
        "Issue Audit: {Total} total, {Open} open, {Closed} closed",
        stats.Total, stats.Open, stats.Closed);
}
```

### Scenario 2: Find Stale Pull Requests

```text
public async Task FindStalePullRequests(ProbotSharpContext context)
{
    var allPRs = await context.GetAllPullRequestsAsync();
    var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);

    var stalePRs = allPRs
        .Where(pr => pr.State == ItemState.Open)
        .Where(pr => pr.UpdatedAt < thirtyDaysAgo)
        .OrderBy(pr => pr.UpdatedAt)
        .ToList();

    context.Logger.LogInformation(
        "Found {Count} stale pull requests",
        stalePRs.Count);

    foreach (var pr in stalePRs)
    {
        // Add a label or comment
        var (owner, repo) = context.Repo();
        await context.GitHub.Issue.Labels.AddToIssue(
            owner, repo, pr.Number, new[] { "stale" });
    }
}
```

### Scenario 3: Sync Issues to External System

```text
public async Task SyncIssuesToExternalSystem(ProbotSharpContext context)
{
    var (owner, repo) = context.Repo();
    int pageNumber = 1;
    int syncedCount = 0;

    while (true)
    {
        var options = new ApiOptions
        {
            PageSize = 100,
            StartPage = pageNumber,
            PageCount = 1
        };

        var issues = await context.GitHub.Issue.GetAllForRepository(
            owner, repo, options);

        if (issues.Count == 0)
        {
            break;
        }

        // Sync this batch to external system
        await SyncBatchToExternalSystem(issues);
        syncedCount += issues.Count;

        context.Logger.LogInformation(
            "Synced {Count} issues to external system",
            syncedCount);

        pageNumber++;

        // Respect rate limits
        await Task.Delay(TimeSpan.FromSeconds(2));
    }
}
```

### Scenario 4: Generate Report from Comments

```text
public async Task GenerateCommentReport(ProbotSharpContext context, int issueNumber)
{
    var allComments = await context.GetAllCommentsAsync(issueNumber);

    var report = new
    {
        TotalComments = allComments.Count,
        UniqueAuthors = allComments.Select(c => c.User.Login).Distinct().Count(),
        BotComments = allComments.Count(c => c.User.Type == AccountType.Bot),
        AverageLength = allComments.Average(c => c.Body.Length),
        FirstComment = allComments.FirstOrDefault()?.CreatedAt,
        LastComment = allComments.LastOrDefault()?.CreatedAt
    };

    context.Logger.LogInformation(
        "Comment Report for Issue #{Number}: {Total} comments from {Authors} authors",
        issueNumber, report.TotalComments, report.UniqueAuthors);
}
```

---

## Rate Limiting Considerations

GitHub enforces rate limits on API calls. When paginating through large datasets, be mindful of these limits.

### Check Rate Limit Status

```text
private async Task CheckRateLimit(ProbotSharpContext context)
{
    var rateLimit = await context.GitHub.RateLimit.GetRateLimits();
    var coreLimit = rateLimit.Resources.Core;

    context.Logger.LogInformation(
        "Rate Limit: {Remaining}/{Limit} remaining, resets at {Reset}",
        coreLimit.Remaining,
        coreLimit.Limit,
        coreLimit.Reset);

    if (coreLimit.Remaining < 100)
    {
        context.Logger.LogWarning("Rate limit running low!");
    }
}
```

### Add Delays Between Pages

```text
private async Task ProcessWithRateLimiting(ProbotSharpContext context)
{
    var (owner, repo) = context.Repo();
    int pageNumber = 1;

    while (true)
    {
        // Check rate limit before each batch
        var rateLimit = await context.GitHub.RateLimit.GetRateLimits();
        if (rateLimit.Resources.Core.Remaining < 10)
        {
            var resetTime = rateLimit.Resources.Core.Reset;
            var waitTime = resetTime - DateTimeOffset.UtcNow;

            context.Logger.LogWarning(
                "Rate limit low, waiting {Seconds} seconds",
                waitTime.TotalSeconds);

            await Task.Delay(waitTime);
        }

        var options = new ApiOptions
        {
            PageSize = 100,
            StartPage = pageNumber,
            PageCount = 1
        };

        var issues = await context.GitHub.Issue.GetAllForRepository(
            owner, repo, options);

        if (issues.Count == 0)
        {
            break;
        }

        // Process issues
        foreach (var issue in issues)
        {
            await ProcessIssueAsync(context, issue);
        }

        pageNumber++;

        // Add delay between pages
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
}
```

### Use Conditional Requests (ETags)

Octokit.NET automatically handles ETags for caching, reducing API calls for unchanged resources.

```text
// Octokit.NET automatically uses ETags when available
// No special code needed - it's handled transparently
var issues = await context.GetAllIssuesAsync();
```

---

## Additional Resources

- [Octokit.NET Documentation](https://octokitnet.readthedocs.io/)
- [GitHub API Pagination Guide](https://docs.github.com/en/rest/guides/using-pagination-in-the-rest-api)
- [GitHub Rate Limiting](https://docs.github.com/en/rest/overview/resources-in-the-rest-api#rate-limiting)
- [Context Helpers Documentation](./ContextHelpers.md)
- [Example: PaginationBot](../examples/PaginationBot/)

---

## See Also

- [Architecture Overview](./Architecture.md)
- [Context Helpers](./ContextHelpers.md)
- [Best Practices](./BestPractices.md)
- [Local Development](./LocalDevelopment.md)
