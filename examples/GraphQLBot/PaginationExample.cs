// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Context;

namespace GraphQLBot;

/// <summary>
/// Demonstrates cursor-based pagination patterns in GraphQL for efficiently fetching large datasets.
/// Shows how to use pageInfo, cursors, and iterate through all pages of results.
/// </summary>
[EventHandler("push")]
public class PaginationExample : IEventHandler
{
    /// <summary>
    /// Handles push event by demonstrating various pagination patterns.
    /// </summary>
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Demonstrating GraphQL pagination for repository: {Repository}",
            context.GetRepositoryFullName());

        if (context.IsBot())
        {
            return;
        }

        var (owner, repo) = context.Repo();

        // Example 1: Paginate through all issues
        var allIssues = await GetAllIssuesWithPagination(context, owner, repo, cancellationToken);
        context.Logger.LogInformation("Total issues fetched: {Count}", allIssues.Count);

        // Example 2: Paginate through all pull requests
        var allPRs = await GetAllPullRequestsWithPagination(context, owner, repo, cancellationToken);
        context.Logger.LogInformation("Total PRs fetched: {Count}", allPRs.Count);

        // Example 3: Paginate through commit history
        var commits = await GetCommitHistory(context, owner, repo, cancellationToken);
        context.Logger.LogInformation("Total commits fetched: {Count}", commits.Count);
    }

    /// <summary>
    /// Demonstrates paginating through all repository issues using cursor-based pagination.
    /// This is the recommended pattern for fetching large datasets efficiently.
    /// </summary>
    private async Task<List<IssueNode>> GetAllIssuesWithPagination(
        ProbotSharpContext context,
        string owner,
        string repo,
        CancellationToken cancellationToken)
    {
        var allIssues = new List<IssueNode>();
        string? cursor = null;
        bool hasNextPage = true;
        int pageNumber = 1;

        context.Logger.LogInformation("Starting to fetch all issues with pagination...");

        while (hasNextPage && !cancellationToken.IsCancellationRequested)
        {
            var query = @"
                query($owner: String!, $name: String!, $cursor: String) {
                    repository(owner: $owner, name: $name) {
                        issues(first: 100, after: $cursor, states: [OPEN, CLOSED], orderBy: {field: CREATED_AT, direction: DESC}) {
                            pageInfo {
                                hasNextPage
                                endCursor
                                startCursor
                            }
                            totalCount
                            edges {
                                cursor
                                node {
                                    id
                                    number
                                    title
                                    state
                                    createdAt
                                    author {
                                        login
                                    }
                                    labels(first: 5) {
                                        nodes {
                                            name
                                        }
                                    }
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
                context.Logger.LogError(
                    "Failed to fetch issues page {PageNumber}: {Error}",
                    pageNumber,
                    result.ErrorMessage);
                break;
            }

            var issues = result.Value.Repository.Issues;

            // Extract nodes from edges
            var pageIssues = issues.Edges.Select(e => e.Node).ToList();
            allIssues.AddRange(pageIssues);

            context.Logger.LogInformation(
                "Page {PageNumber}: Fetched {Count} issues (Total so far: {Total}/{TotalCount})",
                pageNumber,
                pageIssues.Count,
                allIssues.Count,
                issues.TotalCount);

            // Log some sample issues from this page
            foreach (var issue in pageIssues.Take(3))
            {
                var labels = string.Join(", ", issue.Labels.Nodes.Select(l => l.Name));
                context.Logger.LogInformation(
                    "  - #{Number}: {Title} ({State}) by @{Author} [{Labels}]",
                    issue.Number,
                    issue.Title.Length > 50 ? issue.Title[..50] + "..." : issue.Title,
                    issue.State,
                    issue.Author.Login,
                    labels);
            }

            // Update pagination state
            hasNextPage = issues.PageInfo.HasNextPage;
            cursor = issues.PageInfo.EndCursor;
            pageNumber++;

            // Safety check to prevent infinite loops
            if (pageNumber > 100)
            {
                context.Logger.LogWarning("Reached maximum page limit (100), stopping pagination");
                break;
            }
        }

        context.Logger.LogInformation(
            "✅ Pagination complete: Fetched {Count} issues across {Pages} pages",
            allIssues.Count,
            pageNumber - 1);

        return allIssues;
    }

    /// <summary>
    /// Demonstrates paginating through pull requests with state filtering.
    /// </summary>
    private async Task<List<PullRequestNode>> GetAllPullRequestsWithPagination(
        ProbotSharpContext context,
        string owner,
        string repo,
        CancellationToken cancellationToken)
    {
        var allPRs = new List<PullRequestNode>();
        string? cursor = null;
        bool hasNextPage = true;
        int pageNumber = 1;

        context.Logger.LogInformation("Starting to fetch all pull requests with pagination...");

        while (hasNextPage && !cancellationToken.IsCancellationRequested)
        {
            var query = @"
                query($owner: String!, $name: String!, $cursor: String) {
                    repository(owner: $owner, name: $name) {
                        pullRequests(first: 50, after: $cursor, states: [OPEN, CLOSED, MERGED], orderBy: {field: CREATED_AT, direction: DESC}) {
                            pageInfo {
                                hasNextPage
                                endCursor
                            }
                            totalCount
                            edges {
                                node {
                                    id
                                    number
                                    title
                                    state
                                    merged
                                    createdAt
                                    author {
                                        login
                                    }
                                    additions
                                    deletions
                                    changedFiles
                                }
                            }
                        }
                    }
                }
            ";

            var variables = new { owner, name = repo, cursor };

            var result = await context.GraphQL.ExecuteAsync<PullRequestsPageResponse>(
                query,
                variables,
                cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                context.Logger.LogError(
                    "Failed to fetch PRs page {PageNumber}: {Error}",
                    pageNumber,
                    result.ErrorMessage);
                break;
            }

            var prs = result.Value.Repository.PullRequests;
            var pagePRs = prs.Edges.Select(e => e.Node).ToList();
            allPRs.AddRange(pagePRs);

            context.Logger.LogInformation(
                "Page {PageNumber}: Fetched {Count} PRs (Total: {Total}/{TotalCount})",
                pageNumber,
                pagePRs.Count,
                allPRs.Count,
                prs.TotalCount);

            hasNextPage = prs.PageInfo.HasNextPage;
            cursor = prs.PageInfo.EndCursor;
            pageNumber++;

            // Safety limit
            if (pageNumber > 50)
            {
                context.Logger.LogWarning("Reached maximum page limit (50), stopping pagination");
                break;
            }
        }

        // Analyze PR statistics
        var mergedPRs = allPRs.Count(pr => pr.Merged);
        var openPRs = allPRs.Count(pr => pr.State == "OPEN");
        var closedPRs = allPRs.Count(pr => pr.State == "CLOSED" && !pr.Merged);
        var totalChanges = allPRs.Sum(pr => pr.Additions + pr.Deletions);

        context.Logger.LogInformation(
            "✅ PR Statistics: Total={Total}, Open={Open}, Merged={Merged}, Closed={Closed}, TotalChanges={Changes}",
            allPRs.Count,
            openPRs,
            mergedPRs,
            closedPRs,
            totalChanges);

        return allPRs;
    }

    /// <summary>
    /// Demonstrates paginating through commit history on the default branch.
    /// Shows how to handle nested pagination (commits within a branch).
    /// </summary>
    private async Task<List<CommitNode>> GetCommitHistory(
        ProbotSharpContext context,
        string owner,
        string repo,
        CancellationToken cancellationToken)
    {
        var allCommits = new List<CommitNode>();
        string? cursor = null;
        bool hasNextPage = true;
        int pageNumber = 1;
        const int pageSize = 100; // GitHub allows up to 100 commits per page

        context.Logger.LogInformation("Starting to fetch commit history with pagination...");

        while (hasNextPage && !cancellationToken.IsCancellationRequested)
        {
            var query = @"
                query($owner: String!, $name: String!, $cursor: String) {
                    repository(owner: $owner, name: $name) {
                        defaultBranchRef {
                            name
                            target {
                                ... on Commit {
                                    history(first: 100, after: $cursor) {
                                        pageInfo {
                                            hasNextPage
                                            endCursor
                                        }
                                        totalCount
                                        edges {
                                            node {
                                                oid
                                                message
                                                committedDate
                                                author {
                                                    name
                                                    email
                                                    user {
                                                        login
                                                    }
                                                }
                                                additions
                                                deletions
                                                changedFiles
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            ";

            var variables = new { owner, name = repo, cursor };

            var result = await context.GraphQL.ExecuteAsync<CommitHistoryPageResponse>(
                query,
                variables,
                cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                context.Logger.LogError(
                    "Failed to fetch commits page {PageNumber}: {Error}",
                    pageNumber,
                    result.ErrorMessage);
                break;
            }

            var branch = result.Value.Repository.DefaultBranchRef;
            if (branch?.Target?.History == null)
            {
                context.Logger.LogWarning("No commit history found");
                break;
            }

            var history = branch.Target.History;
            var pageCommits = history.Edges.Select(e => e.Node).ToList();
            allCommits.AddRange(pageCommits);

            context.Logger.LogInformation(
                "Page {PageNumber}: Fetched {Count} commits on {Branch} (Total: {Total}/{TotalCount})",
                pageNumber,
                pageCommits.Count,
                branch.Name,
                allCommits.Count,
                history.TotalCount);

            // Log recent commits
            foreach (var commit in pageCommits.Take(2))
            {
                var firstLine = commit.Message.Split('\n')[0];
                var authorName = commit.Author.User?.Login ?? commit.Author.Name;
                context.Logger.LogInformation(
                    "  - {Oid} by {Author}: {Message} (+{Additions}/-{Deletions})",
                    commit.Oid[..7],
                    authorName,
                    firstLine.Length > 50 ? firstLine[..50] + "..." : firstLine,
                    commit.Additions,
                    commit.Deletions);
            }

            hasNextPage = history.PageInfo.HasNextPage;
            cursor = history.PageInfo.EndCursor;
            pageNumber++;

            // Limit pagination for demo purposes (can be adjusted based on needs)
            if (pageNumber > 10)
            {
                context.Logger.LogWarning("Reached maximum page limit (10), stopping pagination");
                break;
            }
        }

        // Calculate statistics
        var totalAdditions = allCommits.Sum(c => c.Additions);
        var totalDeletions = allCommits.Sum(c => c.Deletions);
        var totalFiles = allCommits.Sum(c => c.ChangedFiles);

        context.Logger.LogInformation(
            "✅ Commit Statistics: Total={Total}, +{Additions}/-{Deletions}, Files={Files}",
            allCommits.Count,
            totalAdditions,
            totalDeletions,
            totalFiles);

        return allCommits;
    }

    // Response types for issues pagination
    private record IssuesPageResponse(IssuesRepositoryData Repository);
    private record IssuesRepositoryData(IssuesPageData Issues);
    private record IssuesPageData(PageInfo PageInfo, int TotalCount, List<IssueEdge> Edges);
    private record PageInfo(bool HasNextPage, string? EndCursor, string? StartCursor);
    private record IssueEdge(string Cursor, IssueNode Node);
    private record IssueNode(
        string Id,
        int Number,
        string Title,
        string State,
        DateTime CreatedAt,
        IssueAuthor Author,
        IssueLabels Labels);
    private record IssueAuthor(string Login);
    private record IssueLabels(List<IssueLabel> Nodes);
    private record IssueLabel(string Name);

    // Response types for pull requests pagination
    private record PullRequestsPageResponse(PullRequestsRepositoryData Repository);
    private record PullRequestsRepositoryData(PullRequestsPageData PullRequests);
    private record PullRequestsPageData(PageInfo PageInfo, int TotalCount, List<PullRequestEdge> Edges);
    private record PullRequestEdge(PullRequestNode Node);
    private record PullRequestNode(
        string Id,
        int Number,
        string Title,
        string State,
        bool Merged,
        DateTime CreatedAt,
        PullRequestAuthor Author,
        int Additions,
        int Deletions,
        int ChangedFiles);
    private record PullRequestAuthor(string Login);

    // Response types for commit history pagination
    private record CommitHistoryPageResponse(CommitHistoryRepositoryData Repository);
    private record CommitHistoryRepositoryData(BranchRefData? DefaultBranchRef);
    private record BranchRefData(string Name, CommitTargetData? Target);
    private record CommitTargetData(CommitHistoryData History);
    private record CommitHistoryData(PageInfo PageInfo, int TotalCount, List<CommitEdge> Edges);
    private record CommitEdge(CommitNode Node);
    private record CommitNode(
        string Oid,
        string Message,
        DateTime CommittedDate,
        CommitAuthor Author,
        int Additions,
        int Deletions,
        int ChangedFiles);
    private record CommitAuthor(string Name, string Email, CommitUser? User);
    private record CommitUser(string Login);
}
