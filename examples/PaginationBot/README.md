# PaginationBot Example

This example demonstrates various pagination patterns for working with GitHub's API in ProbotSharp.

## What This Example Shows

This bot demonstrates five different pagination patterns:

1. **Example 1: Get All Issues** - Simple usage of the `GetAllIssuesAsync()` convenience extension
2. **Example 2: Get First 100 Issues** - Using `ApiOptions` to limit results
3. **Example 3: Early Exit Pattern** - Searching through pages until a specific condition is met
4. **Example 4: Incremental Processing** - Processing issues page by page for large datasets
5. **Example 5: Rate Limit Aware** - Checking and respecting GitHub's rate limits during pagination

## Key Concepts

### Octokit.NET Pagination

Octokit.NET provides built-in pagination through `GetAll*` methods that automatically fetch all pages:

```text
var allIssues = await context.GitHub.Issue.GetAllForRepository(owner, repo);
```

### Using ApiOptions

Control pagination behavior with `ApiOptions`:

```text
var options = new ApiOptions
{
    PageSize = 100,  // Max items per page (max: 100)
    PageCount = 1,   // Number of pages to fetch
    StartPage = 1    // Starting page number
};

var issues = await context.GitHub.Issue.GetAllForRepository(owner, repo, options);
```

### Context Helper Extensions

ProbotSharp provides convenience extensions:

```text
// Simple - automatically extracts owner/repo from context
var allIssues = await context.GetAllIssuesAsync();

// With options
var recentIssues = await context.GetAllIssuesAsync(new ApiOptions
{
    PageSize = 100,
    PageCount = 1
});
```

## Running This Example

1. Build the solution:
   ```bash
   dotnet build
   ```

2. Configure your GitHub App (see main README.md)

3. Add a repository to your GitHub App installation

4. Watch the logs to see all five pagination patterns in action

## When to Use Each Pattern

- **Get All**: When you need the complete dataset and it's reasonably sized
- **Limited Fetch**: When you only need recent items (e.g., last 100 issues)
- **Early Exit**: When searching for a specific item and you can stop once found
- **Incremental Processing**: When processing large datasets that don't fit in memory
- **Rate Limit Aware**: When making many API calls or processing multiple repositories

## See Also

- [Pagination Documentation](../../docs/Pagination.md)
- [Context Helpers Documentation](../../docs/ContextHelpers.md)
- [GitHub API Pagination Guide](https://docs.github.com/en/rest/guides/using-pagination-in-the-rest-api)
