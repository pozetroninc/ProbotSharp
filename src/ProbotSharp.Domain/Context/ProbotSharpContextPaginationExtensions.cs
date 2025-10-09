// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Octokit;

namespace ProbotSharp.Domain.Context;

/// <summary>
/// Extension methods for <see cref="ProbotSharpContext"/> to simplify pagination operations with the GitHub API.
/// These methods provide convenient wrappers around Octokit.NET's built-in pagination support.
/// </summary>
/// <remarks>
/// <para>
/// Octokit.NET provides built-in pagination through its <c>GetAll*</c> methods, which automatically
/// handle fetching all pages of results. These extension methods build on that foundation to provide
/// context-aware helpers that automatically extract repository information from the webhook payload.
/// </para>
/// <para>
/// For more information on pagination patterns and best practices, see the documentation at
/// docs/Pagination.md.
/// </para>
/// </remarks>
public static class ProbotSharpContextPaginationExtensions
{
    /// <summary>
    /// Gets all issues for the repository associated with this context.
    /// </summary>
    /// <param name="context">The probot context.</param>
    /// <param name="options">
    /// Options for pagination (page size, page count, start page).
    /// If null, fetches all issues with default pagination settings.
    /// </param>
    /// <returns>A read-only list of all issues for the repository.</returns>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when repository information is not found in the payload.
    /// </exception>
    /// <example>
    /// <code>
    /// // Get all issues
    /// var allIssues = await context.GetAllIssuesAsync();
    ///
    /// // Get first 100 issues only
    /// var options = new ApiOptions
    /// {
    ///     PageSize = 100,
    ///     PageCount = 1
    /// };
    /// var first100 = await context.GetAllIssuesAsync(options);
    ///
    /// // Use LINQ for early exit
    /// var allIssues = await context.GetAllIssuesAsync();
    /// var firstOpen = allIssues.FirstOrDefault(i => i.State == ItemState.Open);
    /// </code>
    /// </example>
    public static async Task<IReadOnlyList<Issue>> GetAllIssuesAsync(
        this ProbotSharpContext context,
        ApiOptions? options = null)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var (owner, repo) = context.Repo();
        return await context.GitHub.Issue.GetAllForRepository(owner, repo, options ?? ApiOptions.None);
    }

    /// <summary>
    /// Gets all pull requests for the repository associated with this context.
    /// </summary>
    /// <param name="context">The probot context.</param>
    /// <param name="options">
    /// Options for pagination (page size, page count, start page).
    /// If null, fetches all pull requests with default pagination settings.
    /// </param>
    /// <returns>A read-only list of all pull requests for the repository.</returns>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when repository information is not found in the payload.
    /// </exception>
    /// <example>
    /// <code>
    /// // Get all pull requests
    /// var allPRs = await context.GetAllPullRequestsAsync();
    ///
    /// // Get open pull requests only (filter after fetching)
    /// var allPRs = await context.GetAllPullRequestsAsync();
    /// var openPRs = allPRs.Where(pr => pr.State == ItemState.Open).ToList();
    ///
    /// // Get first page of 50 pull requests
    /// var options = new ApiOptions
    /// {
    ///     PageSize = 50,
    ///     PageCount = 1
    /// };
    /// var firstPage = await context.GetAllPullRequestsAsync(options);
    /// </code>
    /// </example>
    public static async Task<IReadOnlyList<PullRequest>> GetAllPullRequestsAsync(
        this ProbotSharpContext context,
        ApiOptions? options = null)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var (owner, repo) = context.Repo();
        return await context.GitHub.PullRequest.GetAllForRepository(owner, repo, options ?? ApiOptions.None);
    }

    /// <summary>
    /// Gets all comments for a specific issue in the repository associated with this context.
    /// </summary>
    /// <param name="context">The probot context.</param>
    /// <param name="issueNumber">The issue number to get comments for.</param>
    /// <param name="options">
    /// Options for pagination (page size, page count, start page).
    /// If null, fetches all comments with default pagination settings.
    /// </param>
    /// <returns>A read-only list of all comments for the specified issue.</returns>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when repository information is not found in the payload.
    /// </exception>
    /// <example>
    /// <code>
    /// // Get all comments for an issue
    /// var (owner, repo, number) = context.Issue();
    /// var allComments = await context.GetAllCommentsAsync(number);
    ///
    /// // Get first 10 comments
    /// var options = new ApiOptions
    /// {
    ///     PageSize = 10,
    ///     PageCount = 1
    /// };
    /// var recentComments = await context.GetAllCommentsAsync(number, options);
    ///
    /// // Find a specific comment
    /// var allComments = await context.GetAllCommentsAsync(number);
    /// var botComment = allComments.FirstOrDefault(c => c.User.Login == "mybot[bot]");
    /// </code>
    /// </example>
    public static async Task<IReadOnlyList<IssueComment>> GetAllCommentsAsync(
        this ProbotSharpContext context,
        int issueNumber,
        ApiOptions? options = null)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var (owner, repo) = context.Repo();
        return await context.GitHub.Issue.Comment.GetAllForIssue(owner, repo, issueNumber, options ?? ApiOptions.None);
    }

}
