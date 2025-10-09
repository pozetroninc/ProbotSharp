# GraphQL Bot Example

A comprehensive example demonstrating how to use GitHub's GraphQL API v4 with ProbotSharp through the `context.GraphQL` helper.

## What You'll Learn

This example demonstrates:

- **Basic GraphQL Queries** - Fetch issue and pull request data with precise field selection
- **GraphQL Mutations** - Create comments, update issues, and modify GitHub resources
- **Advanced Queries** - Complex nested queries, fragments, and aliases
- **Cursor-Based Pagination** - Efficiently fetch large datasets using GraphQL pagination
- **Strongly-Typed Responses** - Type-safe response handling with C# records
- **Error Handling** - Graceful error handling with Result pattern
- **Variables & Parameterization** - Safe query parameterization to prevent injection
- **Rate Limiting** - Monitor and manage GraphQL API rate limits

## Project Structure

```
GraphQLBot/
├── GraphQLApp.cs                  # Main app configuration
├── IssueGraphQLHandler.cs         # Basic issue queries & mutations
├── PullRequestGraphQLHandler.cs   # Advanced PR queries
├── AdvancedQueries.cs            # Complex queries with fragments & aliases
├── MutationExamples.cs           # Comprehensive mutation examples
├── PaginationExample.cs          # Cursor-based pagination patterns
└── README.md                     # This file
```

## Examples Overview

### 1. IssueGraphQLHandler.cs

**What it demonstrates:**
- Basic GraphQL query structure
- Query variables and parameterization
- Fetching nested data (labels, reactions)
- GraphQL mutations (adding comments)
- Strongly-typed response models

**Example Query:**
```graphql
query($owner: String!, $name: String!, $number: Int!) {
    repository(owner: $owner, name: $name) {
        issue(number: $number) {
            id
            title
            body
            author { login }
            labels(first: 10) {
                nodes { name color }
            }
            reactions(first: 10) {
                totalCount
            }
        }
    }
}
```

**Triggered by:** `issues.opened` event

### 2. PullRequestGraphQLHandler.cs

**What it demonstrates:**
- Complex nested GraphQL queries
- Multiple related resources in one query
- Pull request statistics (additions, deletions, files)
- Reviews and commit data
- Conditional logic based on query results

**Example Query:**
```graphql
query($owner: String!, $name: String!, $number: Int!) {
    repository(owner: $owner, name: $name) {
        pullRequest(number: $number) {
            id
            title
            additions
            deletions
            changedFiles
            reviews(first: 5) {
                totalCount
                nodes {
                    author { login }
                    state
                }
            }
            commits(first: 100) {
                totalCount
            }
        }
    }
}
```

**Triggered by:** `pull_request.opened` event

### 3. AdvancedQueries.cs

**What it demonstrates:**
- GraphQL fragments for reusable field selections
- Aliases for multiple queries with different parameters
- Complex nested data structures
- Repository insights and statistics
- Search API integration

**Features:**
- `GetRepositoryInsights()` - Comprehensive repository statistics
- `SearchIssuesWithFragments()` - Reusable issue fragments
- `GetMultipleRepositories()` - Batch queries with aliases

**Example Fragment:**
```graphql
fragment IssueFields on Issue {
    id
    number
    title
    state
    author { login }
    createdAt
}

query {
    repository(owner: "owner", name: "repo") {
        openIssues: issues(states: OPEN) {
            nodes { ...IssueFields }
        }
        closedIssues: issues(states: CLOSED) {
            nodes { ...IssueFields }
        }
    }
}
```

### 4. MutationExamples.cs

**What it demonstrates:**
- Creating issues programmatically
- Adding and removing labels
- Updating pull requests
- Adding reactions (thumbs up, heart, rocket, etc.)
- Requesting PR reviews
- Closing/reopening issues

**Available Mutations:**
- `CreateIssue()` - Create a new issue with labels
- `UpdatePullRequest()` - Update PR title and description
- `AddReactionToIssue()` - Add emoji reactions
- `RequestPullRequestReview()` - Request reviews from users
- `CloseIssue()` - Close an issue with a comment

**Example Mutation:**
```graphql
mutation($repositoryId: ID!, $title: String!, $body: String, $labelIds: [ID!]) {
    createIssue(input: {
        repositoryId: $repositoryId,
        title: $title,
        body: $body,
        labelIds: $labelIds
    }) {
        issue {
            id
            number
            title
            url
        }
    }
}
```

### 5. PaginationExample.cs

**What it demonstrates:**
- Cursor-based pagination pattern
- PageInfo usage (hasNextPage, endCursor)
- Fetching all pages of data
- Performance considerations
- Memory-efficient pagination

**Features:**
- `GetAllIssuesWithPagination()` - Paginate through all repository issues
- `GetAllPullRequestsWithPagination()` - Paginate through all PRs
- `GetCommitHistory()` - Paginate through commit history

**Pagination Pattern:**
```text
string? cursor = null;
bool hasNextPage = true;

while (hasNextPage)
{
    var result = await context.GraphQL.ExecuteAsync<Response>(@"
        query($cursor: String) {
            repository(owner: ""owner"", name: ""repo"") {
                issues(first: 100, after: $cursor) {
                    pageInfo {
                        hasNextPage
                        endCursor
                    }
                    edges {
                        node { id title }
                    }
                }
            }
        }
    ", new { cursor });

    // Process results...
    hasNextPage = result.Value.Repository.Issues.PageInfo.HasNextPage;
    cursor = result.Value.Repository.Issues.PageInfo.EndCursor;
}
```

## Setup Instructions

### Prerequisites

- .NET 8.0 SDK or later
- GitHub App with webhook permissions
- Repository with issues and pull requests (for testing)

### Configuration

1. **Install the GraphQLBot project:**

   The bot is already configured in the ProbotSharp solution and will be loaded automatically.

2. **Configure your GitHub App credentials:**

   Set up your `.env` file in the project root:
   ```bash
   APP_ID=your_app_id
   WEBHOOK_SECRET=your_webhook_secret
   PRIVATE_KEY_PATH=path/to/private-key.pem
   ```

3. **Install the GitHub App:**

   Install your GitHub App on a repository to receive webhooks.

### Running Locally

```bash
# From the solution root
dotnet run --project src/ProbotSharp.Bootstrap.Api

# The GraphQLBot will be loaded automatically
# Webhook endpoint: http://localhost:8080/webhooks
```

### Testing with Sample Events

Use the `receive` command to test with sample webhook payloads:

```bash
# Test issue opened event
dotnet run --project src/ProbotSharp.Bootstrap.Cli -- receive \
  --event issues \
  --action opened \
  --payload fixtures/issues-opened.json

# Test pull request opened event
dotnet run --project src/ProbotSharp.Bootstrap.Cli -- receive \
  --event pull_request \
  --action opened \
  --payload fixtures/pull-request-opened.json
```

## Response Types Pattern

All GraphQL responses use strongly-typed C# records:

```text
// Define records matching GraphQL response structure
private record IssueQueryResponse(RepositoryData Repository);
private record RepositoryData(IssueData Issue);
private record IssueData(string Id, string Title, AuthorData Author);
private record AuthorData(string Login);

// Use in query
var result = await context.GraphQL.ExecuteAsync<IssueQueryResponse>(
    query,
    variables,
    cancellationToken);

if (result.IsSuccess && result.Value != null)
{
    var issue = result.Value.Repository.Issue;
    Console.WriteLine($"Issue: {issue.Title} by {issue.Author.Login}");
}
```

## Error Handling Best Practices

Always check for success before accessing response data:

```text
var result = await context.GraphQL.ExecuteAsync<MyResponse>(query, variables, ct);

if (result.IsSuccess && result.Value != null)
{
    // Success path - safe to use result.Value
    ProcessData(result.Value);
}
else if (result.IsFailure)
{
    // Error path - log and handle gracefully
    context.Logger.LogError(
        "GraphQL query failed: {ErrorCode} - {ErrorMessage}",
        result.ErrorCode,
        result.ErrorMessage);

    // Don't throw - handle gracefully
    return;
}
```

## GraphQL Query Tips

### 1. Request Only What You Need

```text
// ❌ Bad - fetching unnecessary fields
query { repository { issues { nodes { id title body comments reactions } } } }

// ✅ Good - fetch only required fields
query { repository { issues { nodes { id title } } } }
```

### 2. Use Variables for Safety

```text
// ❌ Bad - string interpolation (injection risk)
var query = $"query {{ repository(owner: \"{owner}\") {{ ... }} }}";

// ✅ Good - parameterized with variables
var query = "query($owner: String!) { repository(owner: $owner) { ... } }";
var variables = new { owner };
```

### 3. Implement Pagination for Large Datasets

```text
// Always paginate when fetching lists
query($cursor: String) {
    repository(owner: "owner", name: "repo") {
        issues(first: 100, after: $cursor) {
            pageInfo { hasNextPage endCursor }
            nodes { id title }
        }
    }
}
```

### 4. Monitor Rate Limits

```text
// Check rate limit before expensive queries
var rateLimitQuery = "query { rateLimit { remaining resetAt } }";
var result = await context.GraphQL.ExecuteAsync<RateLimitResponse>(
    rateLimitQuery, null, cancellationToken);

if (result.Value?.RateLimit.Remaining < 100)
{
    context.Logger.LogWarning("Rate limit low, skipping expensive query");
    return;
}
```

## Comparison with REST API

**REST API** (multiple requests):
```text
var repo = await context.GitHub.Repository.Get(owner, repo);
var issues = await context.GitHub.Issue.GetAllForRepository(owner, repo);
var pr = await context.GitHub.PullRequest.Get(owner, repo, number);
var reviews = await context.GitHub.PullRequest.Review.GetAll(owner, repo, number);
// 4 API calls, potential over-fetching
```

**GraphQL API** (single request):
```text
var result = await context.GraphQL.ExecuteAsync<Response>(@"
    query($owner: String!, $name: String!, $prNumber: Int!) {
        repository(owner: $owner, name: $name) {
            name
            stargazerCount
            issues(first: 10) { nodes { title } }
            pullRequest(number: $prNumber) {
                title
                reviews(first: 10) { nodes { state } }
            }
        }
    }
", new { owner, name = repo, prNumber });
// 1 API call, fetch exactly what you need
```

## Common Use Cases

### 1. Auto-Labeling with Context

```text
// Get issue details with file changes
var query = @"
    query($owner: String!, $name: String!, $number: Int!) {
        repository(owner: $owner, name: $name) {
            issue(number: $number) {
                id
                title
                body
                timelineItems(first: 1, itemTypes: [CROSS_REFERENCED_EVENT]) {
                    nodes {
                        ... on CrossReferencedEvent {
                            source {
                                ... on PullRequest {
                                    files(first: 10) {
                                        nodes { path }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
";

// Determine labels based on context
var labels = DetermineLabels(issue.TimelineItems);
await AddLabels(issue.Id, labels);
```

### 2. PR Size Analysis

```text
// Get PR statistics in one query
var result = await context.GraphQL.ExecuteAsync<PRStatsResponse>(@"
    query($owner: String!, $name: String!, $number: Int!) {
        repository(owner: $owner, name: $name) {
            pullRequest(number: $number) {
                additions
                deletions
                changedFiles
                commits { totalCount }
            }
        }
    }
", new { owner, name = repo, number = prNumber });

var pr = result.Value.Repository.PullRequest;
if (pr.ChangedFiles > 20 || pr.Additions > 500)
{
    await AddComment(pr.Id, "⚠️ Large PR - consider breaking it down");
}
```

### 3. Team Mentions

```text
// Get organization teams and members
var teams = await context.GraphQL.ExecuteAsync<TeamsResponse>(@"
    query($org: String!) {
        organization(login: $org) {
            teams(first: 20) {
                nodes {
                    name
                    members { nodes { login } }
                }
            }
        }
    }
", new { org = "myorg" });

// Mention team members in comment
var teamMembers = teams.Value.Organization.Teams
    .SelectMany(t => t.Members.Nodes)
    .Select(m => $"@{m.Login}");
```

## Resources

- **GitHub GraphQL API Docs:** https://docs.github.com/en/graphql
- **GraphQL Explorer:** https://docs.github.com/en/graphql/overview/explorer
- **ProbotSharp GraphQL Guide:** [/docs/GraphQL.md](/docs/GraphQL.md)
- **Context Helpers:** [/docs/ContextHelpers.md](/docs/ContextHelpers.md)
- **Architecture:** [/docs/Architecture.md](/docs/Architecture.md#graphql-integration)

## Next Steps

1. **Explore the examples** - Review each handler to understand different patterns
2. **Modify queries** - Try adding/removing fields to see how responses change
3. **Test mutations** - Create issues, add labels, request reviews programmatically
4. **Implement pagination** - Fetch large datasets efficiently with cursor-based pagination
5. **Build your own** - Create custom GraphQL queries for your specific use case

## Troubleshooting

### Common Issues

**1. Null Reference Errors**
- Always check `result.IsSuccess && result.Value != null` before accessing data
- Ensure your response types match the GraphQL schema exactly

**2. Field Not Found**
- Verify field names match GitHub's GraphQL schema (case-sensitive)
- Check if the field requires specific GitHub App permissions

**3. Rate Limit Exceeded**
- Monitor rate limits with `query { rateLimit { remaining } }`
- Implement caching for frequently accessed data
- Use pagination to reduce query complexity

**4. Variables Not Working**
- Ensure variable names in query match object property names
- Variable names are case-sensitive: use `$owner` not `$Owner`

**5. Authentication Errors**
- Verify your GitHub App has the required permissions
- Check that the installation token is valid

## Learn More

- Study the [GitHub GraphQL Schema](https://docs.github.com/en/graphql/reference)
- Use the [GraphQL Explorer](https://docs.github.com/en/graphql/overview/explorer) to test queries
- Review [GraphQL Best Practices](https://graphql.org/learn/best-practices/)
- Read the [ProbotSharp GraphQL Guide](/docs/GraphQL.md)
