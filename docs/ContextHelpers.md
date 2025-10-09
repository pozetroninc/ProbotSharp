# Context Helpers

ProbotSharp provides convenience helper methods on the `ProbotSharpContext` object that mirror the ergonomics of Node.js Probot's `context.issue()` and `context.repo()` methods. These helpers eliminate repetitive payload extraction code and make event handlers more readable.

## Overview

Context helpers extract common parameters from webhook payloads using simple, type-safe methods:

- **`context.Issue()`** - Extracts owner, repo, and issue number
- **`context.Repo()`** - Extracts owner and repo
- **`context.PullRequest()`** - Extracts owner, repo, and pull request number

All helpers return tuples that can be destructured directly, providing clean, idiomatic C# code.

## Motivation

**Without context helpers** (manual extraction):
```text
[EventHandler("issues", "opened")]
public class IssueHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        // Manual payload extraction - verbose and repetitive
        var owner = context.Repository?.Owner;
        var repo = context.Repository?.Name;
        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>();

        if (owner == null || repo == null || !issueNumber.HasValue)
        {
            // Handle missing data
            return;
        }

        await context.GitHub.Issue.Comment.Create(owner, repo, issueNumber.Value, "Hello!");
    }
}
```

**With context helpers** (clean and concise):
```text
[EventHandler("issues", "opened")]
public class IssueHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        // One line extraction with automatic validation
        var (owner, repo, number) = context.Issue();

        await context.GitHub.Issue.Comment.Create(owner, repo, number, "Hello!");
    }
}
```

## API Reference

### `context.Issue()`

Extracts repository owner, name, and issue number from the context payload.

**Signature:**
```text
public static (string Owner, string Repo, int Number) Issue(this ProbotSharpContext context)
```

**Returns:**
Tuple of `(Owner, Repo, Number)`

**Throws:**
- `ArgumentNullException` - When context is null
- `InvalidOperationException` - When repository information or issue number is not found in payload

**Example:**
```text
[EventHandler("issues", "opened")]
public class AddLabelHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        var (owner, repo, number) = context.Issue();

        await context.GitHub.Issue.Labels.AddToIssue(
            owner,
            repo,
            number,
            new[] { "needs-triage" });
    }
}
```

**When to use:**
- Issue opened/closed/edited events
- Issue comment events
- Issue label events
- Any webhook event containing an `issue` object

### `context.Repo()`

Extracts repository owner and name from the context payload.

**Signature:**
```text
public static (string Owner, string Repo) Repo(this ProbotSharpContext context)
```

**Returns:**
Tuple of `(Owner, Repo)`

**Throws:**
- `ArgumentNullException` - When context is null
- `InvalidOperationException` - When repository information is not found in payload

**Example:**
```text
[EventHandler("push")]
public class PushHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        var (owner, repo) = context.Repo();

        // Get all issues for the repository
        var issues = await context.GitHub.Issue.GetAllForRepository(owner, repo);

        context.Logger.LogInformation(
            "{Repository} has {Count} open issues",
            $"{owner}/{repo}",
            issues.Count);
    }
}
```

**When to use:**
- Push events
- Release events
- Repository events
- Any webhook event where you need owner/repo but not a specific issue/PR number

### `context.PullRequest()`

Extracts repository owner, name, and pull request number from the context payload.

**Signature:**
```text
public static (string Owner, string Repo, int Number) PullRequest(this ProbotSharpContext context)
```

**Returns:**
Tuple of `(Owner, Repo, Number)`

**Throws:**
- `ArgumentNullException` - When context is null
- `InvalidOperationException` - When repository information or pull request number is not found in payload

**Example:**
```text
[EventHandler("pull_request", "opened")]
public class PRCheckerHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        var (owner, repo, number) = context.PullRequest();

        // Get PR details
        var pr = await context.GitHub.PullRequest.Get(owner, repo, number);

        if (pr.Commits > 50)
        {
            await context.GitHub.Issue.Comment.Create(
                owner,
                repo,
                number,
                "⚠️ This PR has more than 50 commits. Consider splitting it up!");
        }
    }
}
```

**When to use:**
- Pull request opened/closed/merged events
- Pull request review events
- Pull request comment events
- Any webhook event containing a `pull_request` object

## Pagination Helpers

ProbotSharp provides convenience extensions for common pagination scenarios. These extensions automatically handle repository extraction and integrate with Octokit.NET's built-in pagination support.

### `context.GetAllIssuesAsync()`

Fetches all issues for the repository in the context.

**Signature:**
```text
public static Task<IReadOnlyList<Issue>> GetAllIssuesAsync(
    this ProbotSharpContext context,
    ApiOptions? options = null)
```

**Example:**
```text
[EventHandler("repository", "added")]
public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
{
    // Get all issues
    var allIssues = await context.GetAllIssuesAsync();

    // Or get only the first 100
    var recentIssues = await context.GetAllIssuesAsync(new ApiOptions
    {
        PageSize = 100,
        PageCount = 1
    });
}
```

### `context.GetAllPullRequestsAsync()`

Fetches all pull requests for the repository in the context.

**Signature:**
```text
public static Task<IReadOnlyList<PullRequest>> GetAllPullRequestsAsync(
    this ProbotSharpContext context,
    ApiOptions? options = null)
```

**Example:**
```text
var allPRs = await context.GetAllPullRequestsAsync();
var openPRs = allPRs.Where(pr => pr.State == ItemState.Open).ToList();
```

### `context.GetAllCommentsAsync()`

Fetches all comments for a specific issue.

**Signature:**
```text
public static Task<IReadOnlyList<IssueComment>> GetAllCommentsAsync(
    this ProbotSharpContext context,
    int issueNumber,
    ApiOptions? options = null)
```

**Example:**
```text
var (owner, repo, number) = context.Issue();
var allComments = await context.GetAllCommentsAsync(number);

// Find bot's previous comment
var botComment = allComments.FirstOrDefault(c => c.User.Login == "mybot[bot]");
```

### `context.GetAllInstallationRepositoriesAsync()`

Fetches all repositories for the current installation.

**Signature:**
```text
public static Task<IReadOnlyList<Repository>> GetAllInstallationRepositoriesAsync(
    this ProbotSharpContext context,
    ApiOptions? options = null)
```

**Example:**
```text
var allRepos = await context.GetAllInstallationRepositoriesAsync();
foreach (var repo in allRepos)
{
    context.Logger.LogInformation("Processing repository: {FullName}", repo.FullName);
}
```

**For comprehensive pagination documentation, patterns, and best practices, see [Pagination Guide](./Pagination.md).**

## GraphQL API Helper

ProbotSharp provides first-class support for GitHub's GraphQL API v4 through the `context.GraphQL` property. GraphQL enables precise data fetching and complex queries in a single request, reducing API calls and improving performance.

### Overview

The `context.GraphQL` property provides direct access to GitHub's GraphQL API, similar to `context.octokit.graphql` in Node.js Probot. It supports both queries and mutations with strongly-typed responses.

**Key Benefits:**
- **Precise Data Fetching**: Request exactly the fields you need
- **Single Request**: Fetch related data in one query instead of multiple REST calls
- **Strongly Typed**: Type-safe responses with C# records
- **Efficient**: Reduce bandwidth and rate limit consumption

### Basic Usage

```text
[EventHandler("issues", "opened")]
public class GraphQLExampleHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        var (owner, repo, issueNumber) = context.Issue();

        // Define GraphQL query with variables
        var query = @"
            query($owner: String!, $name: String!, $number: Int!) {
                repository(owner: $owner, name: $name) {
                    issue(number: $number) {
                        id
                        title
                        author {
                            login
                        }
                        labels(first: 10) {
                            nodes {
                                name
                            }
                        }
                    }
                }
            }
        ";

        var variables = new { owner, name = repo, number = issueNumber };

        // Execute query with strongly-typed response
        var result = await context.GraphQL.ExecuteAsync<IssueQueryResponse>(
            query,
            variables,
            cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            var issue = result.Value.Repository.Issue;
            context.Logger.LogInformation(
                "Issue: {Title} by {Author}",
                issue.Title,
                issue.Author.Login);
        }
    }

    // Define C# records matching GraphQL response structure
    private record IssueQueryResponse(RepositoryData Repository);
    private record RepositoryData(IssueData Issue);
    private record IssueData(string Id, string Title, AuthorData Author, LabelsData Labels);
    private record AuthorData(string Login);
    private record LabelsData(List<LabelNode> Nodes);
    private record LabelNode(string Name);
}
```

### Query Examples

#### Repository Information

```text
var query = @"
    query($owner: String!, $name: String!) {
        repository(owner: $owner, name: $name) {
            id
            name
            description
            stargazerCount
            forkCount
            primaryLanguage {
                name
                color
            }
        }
    }
";

var variables = new { owner, name = repo };
var result = await context.GraphQL.ExecuteAsync<RepoResponse>(query, variables, cancellationToken);
```

#### Pull Request Details

```text
var query = @"
    query($owner: String!, $name: String!, $number: Int!) {
        repository(owner: $owner, name: $name) {
            pullRequest(number: $number) {
                id
                title
                state
                additions
                deletions
                changedFiles
                reviews(first: 5) {
                    totalCount
                    nodes {
                        author {
                            login
                        }
                        state
                    }
                }
            }
        }
    }
";
```

### Mutation Examples

#### Add Comment

```text
var mutation = @"
    mutation($subjectId: ID!, $body: String!) {
        addComment(input: {subjectId: $subjectId, body: $body}) {
            commentEdge {
                node {
                    id
                    createdAt
                }
            }
        }
    }
";

var variables = new
{
    subjectId = issueId,  // Node ID from previous query
    body = "Thank you for this contribution!"
};

var result = await context.GraphQL.ExecuteAsync<AddCommentResponse>(
    mutation,
    variables,
    cancellationToken);
```

#### Add Labels

```text
var mutation = @"
    mutation($labelableId: ID!, $labelIds: [ID!]!) {
        addLabelsToLabelable(input: {labelableId: $labelableId, labelIds: $labelIds}) {
            labelable {
                ... on Issue {
                    labels(first: 10) {
                        nodes {
                            name
                        }
                    }
                }
            }
        }
    }
";

var variables = new
{
    labelableId = issueId,
    labelIds = new[] { labelId1, labelId2 }
};
```

### Pagination

GraphQL uses cursor-based pagination for efficient data traversal:

```text
public async Task<List<IssueNode>> GetAllOpenIssues(
    ProbotSharpContext context,
    string owner,
    string repo,
    CancellationToken cancellationToken)
{
    var allIssues = new List<IssueNode>();
    string? cursor = null;
    bool hasNextPage = true;

    while (hasNextPage)
    {
        var query = @"
            query($owner: String!, $name: String!, $cursor: String) {
                repository(owner: $owner, name: $name) {
                    issues(first: 100, after: $cursor, states: OPEN) {
                        pageInfo {
                            hasNextPage
                            endCursor
                        }
                        edges {
                            node {
                                id
                                number
                                title
                            }
                        }
                    }
                }
            }
        ";

        var variables = new { owner, name = repo, cursor };
        var result = await context.GraphQL.ExecuteAsync<IssuesPageResponse>(
            query,
            variables,
            cancellationToken);

        if (!result.IsSuccess || result.Value == null)
        {
            break;
        }

        var issues = result.Value.Repository.Issues;
        allIssues.AddRange(issues.Edges.Select(e => e.Node));

        hasNextPage = issues.PageInfo.HasNextPage;
        cursor = issues.PageInfo.EndCursor;
    }

    return allIssues;
}

private record IssuesPageResponse(RepositoryData Repository);
private record RepositoryData(IssuesData Issues);
private record IssuesData(PageInfoData PageInfo, List<IssueEdgeData> Edges);
private record PageInfoData(bool HasNextPage, string? EndCursor);
private record IssueEdgeData(IssueNode Node);
private record IssueNode(string Id, int Number, string Title);
```

### Error Handling

Always check for success and handle errors gracefully:

```text
var result = await context.GraphQL.ExecuteAsync<MyResponse>(query, variables, cancellationToken);

if (result.IsSuccess && result.Value != null)
{
    // Success path
    var data = result.Value;
    ProcessData(data);
}
else if (result.IsFailure)
{
    // Error path
    context.Logger.LogError(
        "GraphQL query failed: {ErrorCode} - {ErrorMessage}",
        result.ErrorCode,
        result.ErrorMessage);

    // Handle gracefully - don't throw
    return;
}
```

### Response Type Definitions

Define C# records that match your GraphQL response structure:

```text
// GraphQL Response Structure:
// {
//   "repository": {
//     "issue": {
//       "title": "Bug report",
//       "author": { "login": "user" },
//       "labels": {
//         "nodes": [{ "name": "bug" }]
//       }
//     }
//   }
// }

// Matching C# Records:
private record IssueResponse(RepositoryData Repository);
private record RepositoryData(IssueData Issue);
private record IssueData(string Title, AuthorData Author, LabelsData Labels);
private record AuthorData(string Login);
private record LabelsData(List<LabelNode> Nodes);
private record LabelNode(string Name);
```

### Best Practices

1. **Use Variables**: Always use variables instead of string interpolation for safety and cacheability:

```text
// ✅ Good
var query = "query($owner: String!) { ... }";
var variables = new { owner };

// ❌ Bad
var query = $"query {{ repository(owner: \"{owner}\") {{ ... }} }}";
```

2. **Request Only What You Need**: Fetch only the fields you'll use:

```text
// ✅ Good - minimal query
var query = @"query { repository { name stargazerCount } }";

// ❌ Bad - fetching unused data
var query = @"query { repository { name description owner { ... } issues { ... } } }";
```

3. **Check Rate Limits**: Monitor your rate limit for expensive queries:

```text
var rateLimitQuery = "query { rateLimit { remaining resetAt } }";
var result = await context.GraphQL.ExecuteAsync<RateLimitResponse>(
    rateLimitQuery, null, cancellationToken);
```

4. **Use Fragments**: Reuse field selections with fragments:

```text
var query = @"
    fragment IssueFields on Issue {
        id
        number
        title
        state
    }

    query {
        repository(owner: ""owner"", name: ""repo"") {
            openIssues: issues(states: OPEN) { nodes { ...IssueFields } }
            closedIssues: issues(states: CLOSED) { nodes { ...IssueFields } }
        }
    }
";
```

### Comparison with REST

**REST API:**
```text
// Multiple requests needed
var repo = await context.GitHub.Repository.Get(owner, repo);
var issues = await context.GitHub.Issue.GetAllForRepository(owner, repo);
var pr = await context.GitHub.PullRequest.Get(owner, repo, number);
```

**GraphQL API:**
```text
// Single request
var query = @"
    query($owner: String!, $name: String!, $prNumber: Int!) {
        repository(owner: $owner, name: $name) {
            name
            stargazerCount
            issues(first: 10) { nodes { title } }
            pullRequest(number: $prNumber) { title state }
        }
    }
";
var result = await context.GraphQL.ExecuteAsync<Response>(query, variables, cancellationToken);
```

**For comprehensive GraphQL documentation including advanced topics, troubleshooting, and more examples, see [GraphQL Guide](./GraphQL.md).**

## Comparison with Node.js Probot

ProbotSharp's context helpers provide similar functionality to Node.js Probot but leverage C#'s tuple deconstruction for cleaner syntax.

### Node.js Probot
```javascript
export default (app) => {
  app.on("issues.opened", async (context) => {
    // Returns object with owner, repo, number, plus any additional params
    const params = context.issue({ body: "Hello World!" });

    return context.octokit.issues.createComment(params);
  });
};
```

### ProbotSharp
```text
[EventHandler("issues", "opened")]
public class IssueGreeter : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        // Tuple deconstruction - type-safe and concise
        var (owner, repo, number) = context.Issue();

        await context.GitHub.Issue.Comment.Create(owner, repo, number, "Hello World!");
    }
}
```

**Key Differences:**
1. **Return Type**: Node.js returns objects; C# returns tuples (more type-safe)
2. **Parameter Merging**: Node.js merges additional params into the result object; C# separates extraction from API calls
3. **Validation**: Both throw on missing data, but C# provides compile-time type safety

## Error Handling

All context helpers throw `InvalidOperationException` with descriptive messages when required data is missing from the payload.

**Example:**
```text
[EventHandler("push")]
public class SafePushHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        try
        {
            // This will throw if repository info is missing
            var (owner, repo) = context.Repo();

            // Safe to use owner/repo here
            await ProcessPushAsync(owner, repo);
        }
        catch (InvalidOperationException ex)
        {
            context.Logger.LogWarning(
                ex,
                "Failed to extract repository info from payload: {Message}",
                ex.Message);
            // Handle missing data gracefully
        }
    }
}
```

**Common Error Messages:**
- `"Repository owner not found in payload. Ensure the webhook event includes repository information."`
- `"Repository name not found in payload. Ensure the webhook event includes repository information."`
- `"Issue number not found in payload. Ensure this is an issue-related webhook event."`
- `"Pull request number not found in payload. Ensure this is a pull request-related webhook event."`

## Best Practices

### 1. Use the Right Helper for the Event

```text
// ✅ Good - matches event type
[EventHandler("issues", "opened")]
public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
{
    var (owner, repo, number) = context.Issue(); // Correct
}

// ❌ Bad - wrong helper for event type
[EventHandler("push")]
public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
{
    var (owner, repo, number) = context.Issue(); // Will throw! Push events don't have issues
}
```

### 2. Destructure Only What You Need

```text
// If you only need owner/repo, use Repo() instead of Issue()
[EventHandler("issues", "opened")]
public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
{
    // ✅ Good - if you need all three
    var (owner, repo, number) = context.Issue();

    // ✅ Also good - if you only need owner/repo
    var (owner, repo) = context.Repo();

    // Both work, but Repo() is more semantically correct if you don't need the issue number
}
```

### 3. Combine with Octokit.NET Methods

Context helpers work seamlessly with Octokit.NET's API methods:

```text
[EventHandler("pull_request", "opened")]
public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
{
    var (owner, repo, number) = context.PullRequest();

    // Get PR details
    var pr = await context.GitHub.PullRequest.Get(owner, repo, number);

    // Get PR files
    var files = await context.GitHub.PullRequest.Files(owner, repo, number);

    // Add reviewers
    var reviewRequest = new PullRequestReviewRequest(new[] { "reviewer1", "reviewer2" });
    await context.GitHub.PullRequest.ReviewRequest.Create(owner, repo, number, reviewRequest);
}
```

### 4. Handle Bot Events

Always check if the event was triggered by a bot to avoid infinite loops:

```text
[EventHandler("issues", "opened")]
public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
{
    // Check bot first, before extracting params
    if (context.IsBot())
    {
        return;
    }

    var (owner, repo, number) = context.Issue();
    await context.GitHub.Issue.Comment.Create(owner, repo, number, "Welcome!");
}
```

## Migration Guide

If you have existing code that manually extracts payload parameters, migration is straightforward:

### Before:
```text
var owner = context.Repository?.Owner ?? throw new InvalidOperationException("Missing owner");
var repo = context.Repository?.Name ?? throw new InvalidOperationException("Missing repo");
var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>()
    ?? throw new InvalidOperationException("Missing issue number");

await context.GitHub.Issue.Comment.Create(owner, repo, issueNumber, "Hello!");
```

### After:
```text
var (owner, repo, issueNumber) = context.Issue();
await context.GitHub.Issue.Comment.Create(owner, repo, issueNumber, "Hello!");
```

**Benefits:**
- 80% less boilerplate code
- Consistent error messages
- Type-safe tuple deconstruction
- Matches Node.js Probot ergonomics

## See Also

- [Context Helper Methods](Architecture.md#context-helper-methods)
- [Event Routing and Error Handling](Architecture.md#event-routing-and-error-handling)
- [Octokit.NET Documentation](https://octokitnet.readthedocs.io/)
- [Node.js Probot Context Helpers](https://probot.github.io/docs/context/)
