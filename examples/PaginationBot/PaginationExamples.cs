// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Octokit;
using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Context;

namespace PaginationBot;

/// <summary>
/// Demonstrates various pagination patterns for working with GitHub's API.
/// Each example method shows a different approach to fetching and processing paginated data.
/// </summary>
public class PaginationExamples : IEventHandler
{
    /// <summary>
    /// Handles the webhook event by running all pagination examples.
    /// </summary>
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Starting pagination examples for repository {Repo}",
            context.GetRepositoryFullName());

        // Run all examples
        await Example1_GetAllIssues(context);
        await Example2_GetFirst100Issues(context);
        await Example3_EarlyExit(context);
        await Example4_ProcessIncrementally(context);
        await Example5_RateLimitAware(context);

        context.Logger.LogInformation("All pagination examples completed successfully");
    }

    /// <summary>
    /// Example 1: Simple usage - Get all issues using the convenience extension.
    /// This is the most straightforward approach when you need all results.
    /// </summary>
    private async Task Example1_GetAllIssues(ProbotSharpContext context)
    {
        context.Logger.LogInformation("=== Example 1: Get All Issues ===");

        try
        {
            // Use the context extension to get all issues
            var allIssues = await context.GetAllIssuesAsync();

            // Calculate some statistics
            var openCount = allIssues.Count(i => i.State == ItemState.Open);
            var closedCount = allIssues.Count(i => i.State == ItemState.Closed);
            var withLabels = allIssues.Count(i => i.Labels.Any());

            context.Logger.LogInformation(
                "Retrieved {Total} total issues: {Open} open, {Closed} closed, {Labeled} with labels",
                allIssues.Count, openCount, closedCount, withLabels);

            // Find the oldest open issue
            var oldestOpen = allIssues
                .Where(i => i.State == ItemState.Open)
                .OrderBy(i => i.CreatedAt)
                .FirstOrDefault();

            if (oldestOpen != null)
            {
                context.Logger.LogInformation(
                    "Oldest open issue: #{Number} created {DaysAgo} days ago",
                    oldestOpen.Number,
                    (DateTimeOffset.UtcNow - oldestOpen.CreatedAt).Days);
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "Error in Example 1");
        }
    }

    /// <summary>
    /// Example 2: Get only the first 100 issues using ApiOptions.
    /// This is useful when you only need recent results and want to minimize API calls.
    /// </summary>
    private async Task Example2_GetFirst100Issues(ProbotSharpContext context)
    {
        context.Logger.LogInformation("=== Example 2: Get First 100 Issues ===");

        try
        {
            // Configure ApiOptions to get only the first 100 issues
            var options = new ApiOptions
            {
                PageSize = 100,  // Maximum items per page
                PageCount = 1    // Only fetch 1 page
            };

            var recentIssues = await context.GetAllIssuesAsync(options);

            context.Logger.LogInformation(
                "Retrieved {Count} most recent issues",
                recentIssues.Count);

            // Process only issues from the last 30 days
            var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);
            var recentActiveIssues = recentIssues
                .Where(i => i.UpdatedAt > thirtyDaysAgo)
                .ToList();

            context.Logger.LogInformation(
                "Found {Count} issues active in the last 30 days",
                recentActiveIssues.Count);
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "Error in Example 2");
        }
    }

    /// <summary>
    /// Example 3: Early exit - Find a specific issue and stop searching.
    /// This demonstrates how to search through pages until you find what you're looking for.
    /// </summary>
    private async Task Example3_EarlyExit(ProbotSharpContext context)
    {
        context.Logger.LogInformation("=== Example 3: Early Exit Pattern ===");

        try
        {
            var (owner, repo) = context.Repo();
            int pageNumber = 1;
            Issue? targetIssue = null;
            int searchedCount = 0;

            // Search through pages until we find an issue with a "bug" label
            while (targetIssue == null)
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
                    context.Logger.LogInformation(
                        "No bug issues found after searching {Count} issues",
                        searchedCount);
                    break;
                }

                searchedCount += issues.Count;

                // Look for the first issue with a "bug" label
                targetIssue = issues.FirstOrDefault(i =>
                    i.Labels.Any(l => l.Name.Equals("bug", StringComparison.OrdinalIgnoreCase)));

                if (targetIssue != null)
                {
                    context.Logger.LogInformation(
                        "Found bug issue #{Number} after searching {Count} issues",
                        targetIssue.Number, searchedCount);
                    break;
                }

                pageNumber++;

                // Safety limit: don't search more than 500 issues
                if (searchedCount >= 500)
                {
                    context.Logger.LogInformation(
                        "Reached search limit of 500 issues without finding a bug");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "Error in Example 3");
        }
    }

    /// <summary>
    /// Example 4: Process issues incrementally, page by page.
    /// This is useful for large datasets where you want to process results as you fetch them.
    /// </summary>
    private async Task Example4_ProcessIncrementally(ProbotSharpContext context)
    {
        context.Logger.LogInformation("=== Example 4: Incremental Processing ===");

        try
        {
            var (owner, repo) = context.Repo();
            int pageNumber = 1;
            int totalProcessed = 0;
            int totalWithAssignees = 0;

            // Process issues page by page
            while (true)
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
                    break; // No more issues to process
                }

                // Process each issue in this batch
                foreach (var issue in issues)
                {
                    totalProcessed++;

                    if (issue.Assignees.Any())
                    {
                        totalWithAssignees++;
                        context.Logger.LogDebug(
                            "Issue #{Number} is assigned to {Count} people",
                            issue.Number, issue.Assignees.Count);
                    }
                }

                context.Logger.LogInformation(
                    "Processed page {Page}: {Batch} issues ({Total} total so far)",
                    pageNumber, issues.Count, totalProcessed);

                pageNumber++;

                // Optional: Limit to first 200 issues for demo
                if (totalProcessed >= 200)
                {
                    context.Logger.LogInformation(
                        "Reached demo limit of 200 issues");
                    break;
                }

                // Add a small delay between pages to be nice to the API
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }

            context.Logger.LogInformation(
                "Incremental processing complete: {Total} issues processed, {Assigned} with assignees",
                totalProcessed, totalWithAssignees);
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "Error in Example 4");
        }
    }

    /// <summary>
    /// Example 5: Rate limit aware pagination.
    /// This demonstrates how to check rate limits and pause if necessary.
    /// </summary>
    private async Task Example5_RateLimitAware(ProbotSharpContext context)
    {
        context.Logger.LogInformation("=== Example 5: Rate Limit Aware Processing ===");

        try
        {
            var (owner, repo) = context.Repo();

            // Check initial rate limit status
            var initialRateLimit = await context.GitHub.RateLimit.GetRateLimits();
            var coreLimit = initialRateLimit.Resources.Core;

            context.Logger.LogInformation(
                "Starting rate limit: {Remaining}/{Limit} requests remaining, resets at {Reset}",
                coreLimit.Remaining, coreLimit.Limit, coreLimit.Reset.ToLocalTime());

            if (coreLimit.Remaining < 100)
            {
                context.Logger.LogWarning(
                    "Rate limit is low ({Remaining} remaining). Consider waiting before bulk operations.",
                    coreLimit.Remaining);
            }

            int pageNumber = 1;
            int totalProcessed = 0;
            const int LOW_RATE_LIMIT_THRESHOLD = 50;

            while (true)
            {
                // Check rate limit before each batch
                var currentRateLimit = await context.GitHub.RateLimit.GetRateLimits();
                var currentCore = currentRateLimit.Resources.Core;

                if (currentCore.Remaining < LOW_RATE_LIMIT_THRESHOLD)
                {
                    var resetTime = currentCore.Reset;
                    var waitTime = resetTime - DateTimeOffset.UtcNow;

                    if (waitTime.TotalSeconds > 0)
                    {
                        context.Logger.LogWarning(
                            "Rate limit low ({Remaining} remaining). Waiting {Seconds} seconds until reset.",
                            currentCore.Remaining, (int)waitTime.TotalSeconds);

                        await Task.Delay(waitTime);
                    }
                }

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
                    break;
                }

                totalProcessed += issues.Count;

                context.Logger.LogInformation(
                    "Processed page {Page}: {Count} issues (Rate limit: {Remaining} remaining)",
                    pageNumber, issues.Count, currentCore.Remaining);

                pageNumber++;

                // For demo purposes, only process 2 pages
                if (pageNumber > 2)
                {
                    break;
                }

                // Add delay between pages
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            // Check final rate limit status
            var finalRateLimit = await context.GitHub.RateLimit.GetRateLimits();
            var finalCore = finalRateLimit.Resources.Core;

            context.Logger.LogInformation(
                "Processing complete. Rate limit: {Remaining}/{Limit} remaining (used {Used} requests)",
                finalCore.Remaining,
                finalCore.Limit,
                coreLimit.Remaining - finalCore.Remaining);
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "Error in Example 5");
        }
    }
}
