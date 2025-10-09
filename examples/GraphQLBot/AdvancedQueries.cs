// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Context;

namespace GraphQLBot;

/// <summary>
/// Demonstrates advanced GraphQL query patterns including fragments, aliases, and complex nested queries.
/// Shows how to use GraphQL features for efficient data fetching and reusable query components.
/// </summary>
[EventHandler("repository", "created")]
public class AdvancedQueries : IEventHandler
{
    /// <summary>
    /// Handles repository created event by executing advanced GraphQL queries.
    /// </summary>
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Running advanced GraphQL queries for repository: {Repository}",
            context.GetRepositoryFullName());

        if (context.IsBot())
        {
            return;
        }

        var (owner, repo) = context.Repo();

        // Example 1: Get comprehensive repository insights
        await GetRepositoryInsights(context, owner, repo, cancellationToken);

        // Example 2: Search issues using fragments
        await SearchIssuesWithFragments(context, owner, repo, cancellationToken);

        // Example 3: Query multiple repositories with aliases
        await GetMultipleRepositoriesWithAliases(context, owner, cancellationToken);
    }

    /// <summary>
    /// Gets comprehensive repository insights using a complex nested query.
    /// Demonstrates fetching multiple related resources in a single GraphQL request.
    /// </summary>
    private async Task GetRepositoryInsights(
        ProbotSharpContext context,
        string owner,
        string repo,
        CancellationToken cancellationToken)
    {
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
                    languages(first: 10, orderBy: {field: SIZE, direction: DESC}) {
                        edges {
                            size
                            node {
                                name
                                color
                            }
                        }
                        totalSize
                    }
                    defaultBranchRef {
                        name
                        target {
                            ... on Commit {
                                history(first: 1) {
                                    totalCount
                                }
                            }
                        }
                    }
                    openIssues: issues(states: OPEN) {
                        totalCount
                    }
                    closedIssues: issues(states: CLOSED) {
                        totalCount
                    }
                    openPullRequests: pullRequests(states: OPEN) {
                        totalCount
                    }
                    mergedPullRequests: pullRequests(states: MERGED) {
                        totalCount
                    }
                    releases(first: 5, orderBy: {field: CREATED_AT, direction: DESC}) {
                        totalCount
                        nodes {
                            name
                            tagName
                            createdAt
                            isPrerelease
                        }
                    }
                    collaborators(first: 10) {
                        totalCount
                        nodes {
                            login
                            name
                        }
                    }
                    watchers {
                        totalCount
                    }
                }
            }
        ";

        var variables = new { owner, name = repo };

        var result = await context.GraphQL.ExecuteAsync<RepositoryInsightsResponse>(
            query,
            variables,
            cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            var repository = result.Value.Repository;
            context.Logger.LogInformation("=== Repository Insights for {RepoName} ===", repository.Name);
            context.Logger.LogInformation("Description: {Description}", repository.Description ?? "N/A");
            context.Logger.LogInformation("Created: {CreatedAt}", repository.CreatedAt);
            context.Logger.LogInformation("Stars: {Stars}, Forks: {Forks}, Watchers: {Watchers}",
                repository.StargazerCount,
                repository.ForkCount,
                repository.Watchers.TotalCount);

            if (repository.PrimaryLanguage != null)
            {
                context.Logger.LogInformation("Primary Language: {Language}",
                    repository.PrimaryLanguage.Name);
            }

            context.Logger.LogInformation("Languages breakdown (top {Count}):",
                repository.Languages.Edges.Count);
            foreach (var lang in repository.Languages.Edges)
            {
                var percentage = (double)lang.Size / repository.Languages.TotalSize * 100;
                context.Logger.LogInformation("  - {Language}: {Percentage:F1}%",
                    lang.Node.Name,
                    percentage);
            }

            if (repository.DefaultBranchRef?.Target != null)
            {
                context.Logger.LogInformation("Total commits on {Branch}: {Count}",
                    repository.DefaultBranchRef.Name,
                    repository.DefaultBranchRef.Target.History.TotalCount);
            }

            context.Logger.LogInformation("Issues: {Open} open, {Closed} closed",
                repository.OpenIssues.TotalCount,
                repository.ClosedIssues.TotalCount);

            context.Logger.LogInformation("Pull Requests: {Open} open, {Merged} merged",
                repository.OpenPullRequests.TotalCount,
                repository.MergedPullRequests.TotalCount);

            if (repository.Releases.TotalCount > 0)
            {
                context.Logger.LogInformation("Recent releases:");
                foreach (var release in repository.Releases.Nodes)
                {
                    context.Logger.LogInformation("  - {Name} ({Tag}) - {Date}",
                        release.Name,
                        release.TagName,
                        release.CreatedAt);
                }
            }

            context.Logger.LogInformation("Collaborators: {Count}", repository.Collaborators.TotalCount);
        }
        else
        {
            context.Logger.LogError("Failed to fetch repository insights: {Error}",
                result.ErrorMessage);
        }
    }

    /// <summary>
    /// Searches for issues using GraphQL fragments for reusable field selections.
    /// Demonstrates how fragments enable DRY (Don't Repeat Yourself) queries.
    /// </summary>
    private async Task SearchIssuesWithFragments(
        ProbotSharpContext context,
        string owner,
        string repo,
        CancellationToken cancellationToken)
    {
        // Using fragments to avoid repeating the same field selections
        var query = @"
            fragment IssueFields on Issue {
                id
                number
                title
                state
                createdAt
                updatedAt
                author {
                    login
                    avatarUrl
                }
                labels(first: 5) {
                    nodes {
                        name
                        color
                    }
                }
                comments {
                    totalCount
                }
            }

            fragment UserFields on User {
                login
                name
                email
                avatarUrl
            }

            query($owner: String!, $name: String!) {
                repository(owner: $owner, name: $name) {
                    openBugs: issues(first: 10, labels: [""bug""], states: OPEN) {
                        totalCount
                        nodes {
                            ...IssueFields
                            assignees(first: 3) {
                                nodes {
                                    ...UserFields
                                }
                            }
                        }
                    }
                    closedBugs: issues(first: 10, labels: [""bug""], states: CLOSED) {
                        totalCount
                        nodes {
                            ...IssueFields
                        }
                    }
                    enhancements: issues(first: 10, labels: [""enhancement""], states: OPEN) {
                        totalCount
                        nodes {
                            ...IssueFields
                        }
                    }
                }
            }
        ";

        var variables = new { owner, name = repo };

        var result = await context.GraphQL.ExecuteAsync<IssueSearchWithFragmentsResponse>(
            query,
            variables,
            cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            var data = result.Value.Repository;

            context.Logger.LogInformation("=== Issue Search Results (Using Fragments) ===");
            context.Logger.LogInformation("Open Bugs: {Count}", data.OpenBugs.TotalCount);
            foreach (var issue in data.OpenBugs.Nodes)
            {
                context.Logger.LogInformation("  #{Number}: {Title} by @{Author}",
                    issue.Number,
                    issue.Title,
                    issue.Author.Login);

                if (issue.Assignees?.Nodes.Count > 0)
                {
                    var assigneeNames = string.Join(", ",
                        issue.Assignees.Nodes.Select(a => $"@{a.Login}"));
                    context.Logger.LogInformation("    Assigned to: {Assignees}", assigneeNames);
                }
            }

            context.Logger.LogInformation("Closed Bugs: {Count}", data.ClosedBugs.TotalCount);
            context.Logger.LogInformation("Open Enhancements: {Count}", data.Enhancements.TotalCount);
        }
        else
        {
            context.Logger.LogError("Failed to search issues: {Error}", result.ErrorMessage);
        }
    }

    /// <summary>
    /// Queries multiple repositories using aliases to fetch the same fields with different parameters.
    /// Demonstrates how to batch multiple queries in a single GraphQL request.
    /// </summary>
    private async Task GetMultipleRepositoriesWithAliases(
        ProbotSharpContext context,
        string owner,
        CancellationToken cancellationToken)
    {
        // Using aliases to query multiple repositories in one request
        var query = @"
            query($owner: String!) {
                user(login: $owner) {
                    login
                    name
                }
                repo1: repository(owner: $owner, name: ""repo1"") {
                    name
                    stargazerCount
                    openIssues: issues(states: OPEN) {
                        totalCount
                    }
                }
                repo2: repository(owner: $owner, name: ""repo2"") {
                    name
                    stargazerCount
                    openIssues: issues(states: OPEN) {
                        totalCount
                    }
                }
                repo3: repository(owner: $owner, name: ""repo3"") {
                    name
                    stargazerCount
                    openIssues: issues(states: OPEN) {
                        totalCount
                    }
                }
            }
        ";

        var variables = new { owner };

        var result = await context.GraphQL.ExecuteAsync<MultipleRepositoriesResponse>(
            query,
            variables,
            cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            context.Logger.LogInformation("=== Multiple Repositories (Using Aliases) ===");
            context.Logger.LogInformation("Owner: {Name} (@{Login})",
                result.Value.User.Name,
                result.Value.User.Login);

            LogRepositoryStats(context, "Repository 1", result.Value.Repo1);
            LogRepositoryStats(context, "Repository 2", result.Value.Repo2);
            LogRepositoryStats(context, "Repository 3", result.Value.Repo3);
        }
        else
        {
            context.Logger.LogError("Failed to fetch multiple repositories: {Error}",
                result.ErrorMessage);
        }
    }

    private void LogRepositoryStats(ProbotSharpContext context, string label, RepoStats? repo)
    {
        if (repo == null)
        {
            context.Logger.LogInformation("{Label}: Not found", label);
            return;
        }

        context.Logger.LogInformation("{Label}: {Name} - {Stars} stars, {Issues} open issues",
            label,
            repo.Name,
            repo.StargazerCount,
            repo.OpenIssues.TotalCount);
    }

    // Response types for repository insights query
    private record RepositoryInsightsResponse(RepositoryInsightsData Repository);
    private record RepositoryInsightsData(
        string Id,
        string Name,
        string? Description,
        DateTime CreatedAt,
        int StargazerCount,
        int ForkCount,
        bool IsPrivate,
        LanguageData? PrimaryLanguage,
        LanguagesData Languages,
        BranchRefData? DefaultBranchRef,
        IssuesCountData OpenIssues,
        IssuesCountData ClosedIssues,
        PullRequestsCountData OpenPullRequests,
        PullRequestsCountData MergedPullRequests,
        ReleasesData Releases,
        CollaboratorsData Collaborators,
        WatchersData Watchers);
    private record LanguageData(string Name, string Color);
    private record LanguagesData(List<LanguageEdgeData> Edges, int TotalSize);
    private record LanguageEdgeData(int Size, LanguageNodeData Node);
    private record LanguageNodeData(string Name, string Color);
    private record BranchRefData(string Name, CommitTargetData Target);
    private record CommitTargetData(CommitHistoryData History);
    private record CommitHistoryData(int TotalCount);
    private record IssuesCountData(int TotalCount);
    private record PullRequestsCountData(int TotalCount);
    private record ReleasesData(int TotalCount, List<ReleaseNodeData> Nodes);
    private record ReleaseNodeData(string Name, string TagName, DateTime CreatedAt, bool IsPrerelease);
    private record CollaboratorsData(int TotalCount, List<UserNodeData> Nodes);
    private record UserNodeData(string Login, string? Name);
    private record WatchersData(int TotalCount);

    // Response types for issue search with fragments
    private record IssueSearchWithFragmentsResponse(IssueSearchRepositoryData Repository);
    private record IssueSearchRepositoryData(
        IssueListData OpenBugs,
        IssueListData ClosedBugs,
        IssueListData Enhancements);
    private record IssueListData(int TotalCount, List<IssueNodeData> Nodes);
    private record IssueNodeData(
        string Id,
        int Number,
        string Title,
        string State,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        IssueAuthorData Author,
        IssueLabelsData Labels,
        IssueCommentsData Comments,
        AssigneesData? Assignees);
    private record IssueAuthorData(string Login, string AvatarUrl);
    private record IssueLabelsData(List<LabelNodeData> Nodes);
    private record LabelNodeData(string Name, string Color);
    private record IssueCommentsData(int TotalCount);
    private record AssigneesData(List<AssigneeNodeData> Nodes);
    private record AssigneeNodeData(string Login, string? Name, string? Email, string AvatarUrl);

    // Response types for multiple repositories with aliases
    private record MultipleRepositoriesResponse(UserData User, RepoStats? Repo1, RepoStats? Repo2, RepoStats? Repo3);
    private record UserData(string Login, string Name);
    private record RepoStats(string Name, int StargazerCount, RepoIssuesData OpenIssues);
    private record RepoIssuesData(int TotalCount);
}
