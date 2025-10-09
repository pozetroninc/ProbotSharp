# GraphQL API Integration

ProbotSharp provides first-class support for GitHub's GraphQL API v4 through the `context.GraphQL` helper. GraphQL enables you to fetch exactly the data you need in a single request, reducing API calls and improving performance.

## Table of Contents
- [Overview](#overview)
- [Quick Start](#quick-start)
- [Queries](#queries)
- [Mutations](#mutations)
- [Advanced Topics](#advanced-topics)
- [Troubleshooting](#troubleshooting)
- [Best Practices](#best-practices)

## Overview

### Why GraphQL?

GitHub's GraphQL API v4 offers several advantages over the REST API:

1. **Precise Data Fetching**: Request exactly the fields you need, no more, no less
2. **Single Request for Complex Data**: Fetch related data in one query instead of multiple REST calls
3. **Strongly Typed**: Full type safety with C# records matching GraphQL schema
4. **Efficient**: Reduce bandwidth and API rate limit consumption
5. **Introspection**: Schema is self-documenting and explorable

### GraphQL vs REST

| Feature | REST API | GraphQL API |
|---------|----------|-------------|
| **Data Fetching** | Fixed endpoints return predefined data | Request exactly what you need |
| **Multiple Resources** | Multiple round trips required | Single request with nested queries |
| **Over-fetching** | Common - endpoints return extra data | Never - you specify all fields |
| **Under-fetching** | Common - need additional requests | Rare - fetch related data in one query |
| **Type Safety** | Manual type definitions | Schema-based, introspectable |

### Access in ProbotSharp

Access GitHub's GraphQL API through the `context.GraphQL` property:

```text
[EventHandler("issues", "opened")]
public class MyHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        // GraphQL client is available on context
        var result = await context.GraphQL.ExecuteAsync<MyResponse>(
            query: "query { viewer { login } }",
            variables: null,
            cancellationToken);
    }
}
```

## Quick Start

### Your First Query

Here's a simple example that queries the authenticated user's login:

```text
[EventHandler("issues", "opened")]
public class FirstGraphQLHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        // Define the GraphQL query
        var query = @"
            query {
                viewer {
                    login
                    name
                    email
                }
            }
        ";

        // Execute the query
        var result = await context.GraphQL.ExecuteAsync<ViewerResponse>(
            query,
            variables: null,
            cancellationToken);

        // Handle the result
        if (result.IsSuccess && result.Value != null)
        {
            var viewer = result.Value.Viewer;
            context.Logger.LogInformation(
                "Authenticated as: {Login} ({Name})",
                viewer.Login,
                viewer.Name);
        }
        else
        {
            context.Logger.LogError(
                "GraphQL query failed: {Error}",
                result.ErrorMessage);
        }
    }

    // Define response type matching GraphQL schema
    private record ViewerResponse(ViewerData Viewer);
    private record ViewerData(string Login, string Name, string Email);
}
```

### Using Variables

Variables make queries reusable and safer by preventing injection:

```text
[EventHandler("issues", "opened")]
public class VariableQueryHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        var (owner, repo, issueNumber) = context.Issue();

        // Define query with variables
        var query = @"
            query($owner: String!, $repo: String!, $number: Int!) {
                repository(owner: $owner, name: $repo) {
                    issue(number: $number) {
                        id
                        title
                        state
                    }
                }
            }
        ";

        // Pass variables as anonymous object
        var variables = new { owner, repo, number = issueNumber };

        var result = await context.GraphQL.ExecuteAsync<IssueResponse>(
            query,
            variables,
            cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            var issue = result.Value.Repository.Issue;
            context.Logger.LogInformation(
                "Issue #{Number}: {Title} ({State})",
                issueNumber,
                issue.Title,
                issue.State);
        }
    }

    private record IssueResponse(RepositoryData Repository);
    private record RepositoryData(IssueData Issue);
    private record IssueData(string Id, string Title, string State);
}
```

## Queries

### Repository Queries

#### Basic Repository Info

```text
var query = @"
    query($owner: String!, $name: String!) {
        repository(owner: $owner, name: $name) {
            id
            name
            description
            createdAt
            stargazerCount
            forkCount
            isPrivate
            primaryLanguage {
                name
                color
            }
            owner {
                login
                avatarUrl
            }
        }
    }
";

var variables = new { owner, name = repo };
var result = await context.GraphQL.ExecuteAsync<RepositoryResponse>(query, variables, cancellationToken);
```

#### Repository with Issues

```text
var query = @"
    query($owner: String!, $name: String!) {
        repository(owner: $owner, name: $name) {
            name
            issues(first: 10, states: OPEN, orderBy: {field: CREATED_AT, direction: DESC}) {
                totalCount
                edges {
                    node {
                        number
                        title
                        author {
                            login
                        }
                        createdAt
                        labels(first: 5) {
                            nodes {
                                name
                                color
                            }
                        }
                    }
                }
            }
        }
    }
";
```

### Issue Queries

#### Get Issue Details

```text
var query = @"
    query($owner: String!, $name: String!, $number: Int!) {
        repository(owner: $owner, name: $name) {
            issue(number: $number) {
                id
                title
                body
                state
                createdAt
                author {
                    login
                    avatarUrl
                }
                assignees(first: 10) {
                    nodes {
                        login
                        name
                    }
                }
                labels(first: 10) {
                    nodes {
                        id
                        name
                        color
                        description
                    }
                }
                comments(first: 10) {
                    totalCount
                    nodes {
                        id
                        body
                        author {
                            login
                        }
                        createdAt
                    }
                }
                reactions(first: 10) {
                    totalCount
                    nodes {
                        content
                        user {
                            login
                        }
                    }
                }
            }
        }
    }
";
```

#### Search Issues

```text
var query = @"
    query($searchQuery: String!) {
        search(query: $searchQuery, type: ISSUE, first: 20) {
            issueCount
            edges {
                node {
                    ... on Issue {
                        number
                        title
                        repository {
                            nameWithOwner
                        }
                        author {
                            login
                        }
                        createdAt
                    }
                }
            }
        }
    }
";

var variables = new { searchQuery = "is:open label:bug repo:owner/repo" };
```

### Pull Request Queries

#### Get Pull Request Details

```text
var query = @"
    query($owner: String!, $name: String!, $number: Int!) {
        repository(owner: $owner, name: $name) {
            pullRequest(number: $number) {
                id
                title
                body
                state
                merged
                mergeable
                isDraft
                additions
                deletions
                changedFiles
                author {
                    login
                }
                baseRef {
                    name
                }
                headRef {
                    name
                }
                commits(first: 100) {
                    totalCount
                    nodes {
                        commit {
                            message
                            author {
                                name
                                email
                            }
                        }
                    }
                }
                reviews(first: 10) {
                    totalCount
                    nodes {
                        author {
                            login
                        }
                        state
                        body
                        submittedAt
                    }
                }
                files(first: 100) {
                    totalCount
                    nodes {
                        path
                        additions
                        deletions
                    }
                }
            }
        }
    }
";
```

#### Get Pull Requests by State

```text
var query = @"
    query($owner: String!, $name: String!, $states: [PullRequestState!]) {
        repository(owner: $owner, name: $name) {
            pullRequests(first: 20, states: $states, orderBy: {field: UPDATED_AT, direction: DESC}) {
                totalCount
                nodes {
                    number
                    title
                    state
                    author {
                        login
                    }
                    updatedAt
                    reviewDecision
                }
            }
        }
    }
";

var variables = new { owner, name = repo, states = new[] { "OPEN" } };
```

### User Queries

#### Get User Profile

```text
var query = @"
    query($login: String!) {
        user(login: $login) {
            id
            login
            name
            bio
            email
            avatarUrl
            company
            location
            websiteUrl
            twitterUsername
            createdAt
            followers {
                totalCount
            }
            following {
                totalCount
            }
            repositories(first: 10, orderBy: {field: STARGAZERS, direction: DESC}) {
                totalCount
                nodes {
                    name
                    description
                    stargazerCount
                    primaryLanguage {
                        name
                    }
                }
            }
        }
    }
";

var variables = new { login = "octocat" };
```

### Organization Queries

#### Get Organization Info

```text
var query = @"
    query($login: String!) {
        organization(login: $login) {
            id
            login
            name
            description
            websiteUrl
            email
            avatarUrl
            createdAt
            repositories(first: 20, orderBy: {field: STARGAZERS, direction: DESC}) {
                totalCount
                nodes {
                    name
                    description
                    stargazerCount
                    isPrivate
                }
            }
            membersWithRole(first: 10) {
                totalCount
                nodes {
                    login
                    name
                }
            }
        }
    }
";
```

#### Get Organization Teams

```text
var query = @"
    query($org: String!) {
        organization(login: $org) {
            teams(first: 20) {
                totalCount
                nodes {
                    id
                    name
                    description
                    privacy
                    members {
                        totalCount
                    }
                    repositories {
                        totalCount
                    }
                }
            }
        }
    }
";
```

## Mutations

Mutations modify data on GitHub. They follow a similar pattern to queries but use the `mutation` keyword.

### Add Comment to Issue

```text
[EventHandler("issues", "opened")]
public class AddCommentHandler : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        // First, get the issue node ID
        var (owner, repo, issueNumber) = context.Issue();

        var getIdQuery = @"
            query($owner: String!, $name: String!, $number: Int!) {
                repository(owner: $owner, name: $name) {
                    issue(number: $number) {
                        id
                    }
                }
            }
        ";

        var idResult = await context.GraphQL.ExecuteAsync<IssueIdResponse>(
            getIdQuery,
            new { owner, name = repo, number = issueNumber },
            cancellationToken);

        if (!idResult.IsSuccess || idResult.Value == null)
        {
            return;
        }

        var issueId = idResult.Value.Repository.Issue.Id;

        // Add comment mutation
        var mutation = @"
            mutation($subjectId: ID!, $body: String!) {
                addComment(input: {subjectId: $subjectId, body: $body}) {
                    commentEdge {
                        node {
                            id
                            body
                            createdAt
                            author {
                                login
                            }
                        }
                    }
                }
            }
        ";

        var variables = new
        {
            subjectId = issueId,
            body = "Thank you for opening this issue! We'll review it shortly."
        };

        var result = await context.GraphQL.ExecuteAsync<AddCommentResponse>(
            mutation,
            variables,
            cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            var comment = result.Value.AddComment.CommentEdge.Node;
            context.Logger.LogInformation(
                "Added comment {CommentId} at {CreatedAt}",
                comment.Id,
                comment.CreatedAt);
        }
    }

    private record IssueIdResponse(RepositoryData Repository);
    private record RepositoryData(IssueIdData Issue);
    private record IssueIdData(string Id);

    private record AddCommentResponse(AddCommentData AddComment);
    private record AddCommentData(CommentEdgeData CommentEdge);
    private record CommentEdgeData(CommentNodeData Node);
    private record CommentNodeData(string Id, string Body, DateTime CreatedAt, AuthorData Author);
    private record AuthorData(string Login);
}
```

### Add Labels to Issue

```text
var mutation = @"
    mutation($labelableId: ID!, $labelIds: [ID!]!) {
        addLabelsToLabelable(input: {labelableId: $labelableId, labelIds: $labelIds}) {
            labelable {
                ... on Issue {
                    id
                    labels(first: 10) {
                        nodes {
                            id
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
    labelableId = issueId,  // Node ID of the issue
    labelIds = new[] { labelId1, labelId2 }  // Node IDs of labels
};

var result = await context.GraphQL.ExecuteAsync<AddLabelsResponse>(mutation, variables, cancellationToken);
```

### Create Issue

```text
var mutation = @"
    mutation($repositoryId: ID!, $title: String!, $body: String) {
        createIssue(input: {repositoryId: $repositoryId, title: $title, body: $body}) {
            issue {
                id
                number
                title
                url
            }
        }
    }
";

var variables = new
{
    repositoryId = repoId,  // Node ID of repository
    title = "New issue from bot",
    body = "This issue was created via GraphQL mutation"
};

var result = await context.GraphQL.ExecuteAsync<CreateIssueResponse>(mutation, variables, cancellationToken);
```

### Update Pull Request

```text
var mutation = @"
    mutation($pullRequestId: ID!, $title: String, $body: String) {
        updatePullRequest(input: {pullRequestId: $pullRequestId, title: $title, body: $body}) {
            pullRequest {
                id
                title
                body
            }
        }
    }
";

var variables = new
{
    pullRequestId = prId,
    title = "Updated PR title",
    body = "Updated description with more details"
};
```

### Add Reaction

```text
var mutation = @"
    mutation($subjectId: ID!, $content: ReactionContent!) {
        addReaction(input: {subjectId: $subjectId, content: $content}) {
            reaction {
                id
                content
                user {
                    login
                }
            }
        }
    }
";

var variables = new
{
    subjectId = issueId,
    content = "THUMBS_UP"  // Options: THUMBS_UP, THUMBS_DOWN, LAUGH, HOORAY, CONFUSED, HEART, ROCKET, EYES
};
```

### Request Pull Request Review

```text
var mutation = @"
    mutation($pullRequestId: ID!, $userIds: [ID!]!) {
        requestReviews(input: {pullRequestId: $pullRequestId, userIds: $userIds}) {
            pullRequest {
                id
                reviewRequests(first: 10) {
                    nodes {
                        requestedReviewer {
                            ... on User {
                                login
                            }
                        }
                    }
                }
            }
        }
    }
";

var variables = new
{
    pullRequestId = prId,
    userIds = new[] { userId1, userId2 }
};
```

## Advanced Topics

### Pagination with Cursor-Based Navigation

GraphQL uses cursor-based pagination for efficient data traversal:

```text
public async Task<List<IssueNode>> GetAllIssues(
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
                                state
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
private record IssueNode(string Id, int Number, string Title, string State);
```

### Using Fragments

Fragments let you reuse common field selections:

```text
var query = @"
    fragment IssueFields on Issue {
        id
        number
        title
        state
        author {
            login
        }
        createdAt
    }

    query($owner: String!, $name: String!) {
        repository(owner: $owner, name: $name) {
            openIssues: issues(first: 10, states: OPEN) {
                nodes {
                    ...IssueFields
                }
            }
            closedIssues: issues(first: 10, states: CLOSED) {
                nodes {
                    ...IssueFields
                }
            }
        }
    }
";
```

### Aliases for Multiple Queries

Fetch the same field with different arguments using aliases:

```text
var query = @"
    query($owner: String!, $name: String!) {
        repository(owner: $owner, name: $name) {
            openBugs: issues(first: 10, labels: [""bug""], states: OPEN) {
                totalCount
            }
            closedBugs: issues(first: 10, labels: [""bug""], states: CLOSED) {
                totalCount
            }
            openFeatures: issues(first: 10, labels: [""enhancement""], states: OPEN) {
                totalCount
            }
        }
    }
";
```

### Rate Limiting

Check your rate limit status:

```text
var query = @"
    query {
        rateLimit {
            limit
            cost
            remaining
            resetAt
        }
    }
";

var result = await context.GraphQL.ExecuteAsync<RateLimitResponse>(query, null, cancellationToken);

if (result.IsSuccess && result.Value != null)
{
    var rateLimit = result.Value.RateLimit;
    context.Logger.LogInformation(
        "Rate limit: {Remaining}/{Limit}, resets at {ResetAt}",
        rateLimit.Remaining,
        rateLimit.Limit,
        rateLimit.ResetAt);
}

private record RateLimitResponse(RateLimitData RateLimit);
private record RateLimitData(int Limit, int Cost, int Remaining, DateTime ResetAt);
```

### Strongly-Typed Responses

Always define C# records matching your GraphQL response structure:

```text
// GraphQL Response:
// {
//   "repository": {
//     "issue": {
//       "title": "Bug report",
//       "author": { "login": "user" }
//     }
//   }
// }

// Matching C# Records:
private record IssueResponse(RepositoryData Repository);
private record RepositoryData(IssueData Issue);
private record IssueData(string Title, AuthorData Author);
private record AuthorData(string Login);
```

### Error Handling

GraphQL queries return a `Result<T>` type. Always check for success:

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

    // Handle specific error codes
    if (result.ErrorCode == "RATE_LIMITED")
    {
        // Wait and retry
    }
}
```

## Troubleshooting

### Common Issues

#### 1. Null Reference Errors

**Problem**: `NullReferenceException` when accessing response data

**Solution**: Always check if result is successful and value is not null:

```text
if (result.IsSuccess && result.Value != null)
{
    var data = result.Value.Repository.Issue;  // Safe
}
```

#### 2. Field Type Mismatches

**Problem**: JSON deserialization fails

**Solution**: Ensure your C# record types match the GraphQL schema exactly:

```text
// GraphQL returns DateTime as string
private record IssueData(DateTime CreatedAt);  // Will deserialize correctly

// GraphQL returns nullable field
private record IssueData(string? Body);  // Use nullable for optional fields
```

#### 3. Variables Not Working

**Problem**: Variables are null or not being passed

**Solution**: Use anonymous objects with property names matching variable names:

```text
// Query defines: query($owner: String!, $name: String!)
var variables = new { owner = "myorg", name = "myrepo" };  // Correct

// Wrong - property names must match
var variables = new { Owner = "myorg", Name = "myrepo" };  // Won't work
```

#### 4. Rate Limit Exceeded

**Problem**: Getting rate limit errors

**Solution**: Check rate limit before expensive queries:

```text
// Check rate limit first
var rateLimitQuery = "query { rateLimit { remaining } }";
var rateLimitResult = await context.GraphQL.ExecuteAsync<RateLimitResponse>(
    rateLimitQuery, null, cancellationToken);

if (rateLimitResult.Value?.RateLimit.Remaining < 100)
{
    context.Logger.LogWarning("Rate limit low, skipping query");
    return;
}

// Proceed with expensive query
```

#### 5. Node ID vs Database ID

**Problem**: Confusion between node IDs and database IDs

**Solution**:
- Use `id` (Node ID) for GraphQL mutations - it's a global identifier
- Use `number` or `databaseId` for REST API calls

```text
// For GraphQL mutations, use 'id' field
var mutation = "mutation($id: ID!) { ... }";
var variables = new { id = issue.Id };  // Node ID

// For logging/display, use 'number'
context.Logger.LogInformation("Issue #{Number}", issue.Number);
```

### Debugging Tips

1. **Use GitHub GraphQL Explorer**: Test queries at https://docs.github.com/en/graphql/overview/explorer

2. **Log Query and Variables**:
```text
context.Logger.LogDebug("GraphQL Query: {Query}", query);
context.Logger.LogDebug("Variables: {Variables}", JsonConvert.SerializeObject(variables));
```

3. **Check Response Structure**:
```text
if (result.IsFailure)
{
    context.Logger.LogError("GraphQL Error: {Error}", result.ErrorMessage);
}
```

4. **Validate with Schema**: Use GitHub's schema documentation to verify field names and types

## Best Practices

### 1. Request Only What You Need

```text
// ❌ Bad - fetching unnecessary data
var query = @"
    query($owner: String!, $name: String!) {
        repository(owner: $owner, name: $name) {
            issues(first: 100) {
                nodes {
                    id
                    title
                    body
                    comments { ... }  // Not needed
                    reactions { ... }  // Not needed
                }
            }
        }
    }
";

// ✅ Good - fetch only required fields
var query = @"
    query($owner: String!, $name: String!) {
        repository(owner: $owner, name: $name) {
            issues(first: 100) {
                nodes {
                    id
                    title
                }
            }
        }
    }
";
```

### 2. Use Variables Instead of String Interpolation

```text
// ❌ Bad - injection risk and not cacheable
var query = $@"
    query {{
        repository(owner: ""{owner}"", name: ""{repo}"") {{
            ...
        }}
    }}
";

// ✅ Good - safe and cacheable
var query = @"
    query($owner: String!, $name: String!) {
        repository(owner: $owner, name: $name) {
            ...
        }
    }
";
var variables = new { owner, name = repo };
```

### 3. Implement Pagination for Large Datasets

```text
// ✅ Always paginate when fetching lists
var query = @"
    query($cursor: String) {
        repository(owner: ""owner"", name: ""repo"") {
            issues(first: 100, after: $cursor) {
                pageInfo {
                    hasNextPage
                    endCursor
                }
                nodes { ... }
            }
        }
    }
";
```

### 4. Cache Expensive Queries

```text
// Use caching for data that doesn't change frequently
var cacheKey = $"graphql:repo:{owner}:{repo}";
var cached = await cacheService.GetAsync<RepoData>(cacheKey);

if (cached != null)
{
    return cached;
}

var result = await context.GraphQL.ExecuteAsync<RepoResponse>(query, variables, cancellationToken);
if (result.IsSuccess && result.Value != null)
{
    await cacheService.SetAsync(cacheKey, result.Value, TimeSpan.FromMinutes(5));
}
```

### 5. Handle Errors Gracefully

```text
var result = await context.GraphQL.ExecuteAsync<Response>(query, variables, cancellationToken);

if (result.IsFailure)
{
    context.Logger.LogError("GraphQL query failed: {Error}", result.ErrorMessage);

    // Don't throw - handle gracefully
    return DefaultValue;
}

if (result.Value == null)
{
    context.Logger.LogWarning("GraphQL query returned null");
    return DefaultValue;
}
```

### 6. Use Meaningful Record Names

```text
// ✅ Good - descriptive names
private record IssueDetailsResponse(RepositoryData Repository);
private record RepositoryData(IssueData Issue);
private record IssueData(string Title, string Body, AuthorData Author);

// ❌ Bad - generic names
private record Response(Data Data);
private record Data(Thing Thing);
```

## See Also

- [GitHub GraphQL API Documentation](https://docs.github.com/en/graphql)
- [GitHub GraphQL Explorer](https://docs.github.com/en/graphql/overview/explorer)
- [GraphQL Official Documentation](https://graphql.org/learn/)
- [Context Helpers](./ContextHelpers.md)
- [Architecture Overview](./Architecture.md)
- [Example: GraphQLBot](../examples/GraphQLBot/README.md)
