// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Context;

namespace GraphQLBot;

/// <summary>
/// Event handler that demonstrates using GraphQL to query pull request information.
/// Shows more advanced GraphQL queries including pagination and nested data.
/// </summary>
[EventHandler("pull_request", "opened")]
public class PullRequestGraphQLHandler : IEventHandler
{
    /// <summary>
    /// Handles the pull request opened event by querying PR details via GraphQL.
    /// </summary>
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Received pull request opened event for {Repository}",
            context.GetRepositoryFullName());

        // Don't respond to bot-created PRs
        if (context.IsBot())
        {
            context.Logger.LogDebug("Pull request was created by a bot, skipping");
            return;
        }

        // Extract pull request parameters
        var (owner, repo, prNumber) = context.PullRequest();

        try
        {
            // Example: Query pull request with reviews, comments, and files changed
            // This demonstrates a more complex GraphQL query with nested fields
            var query = @"
                query($owner: String!, $name: String!, $number: Int!) {
                    repository(owner: $owner, name: $name) {
                        pullRequest(number: $number) {
                            id
                            title
                            body
                            author {
                                login
                            }
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
                                    body
                                }
                            }
                            comments(first: 10) {
                                totalCount
                            }
                            commits(first: 1) {
                                totalCount
                            }
                            labels(first: 10) {
                                nodes {
                                    name
                                    color
                                }
                            }
                            mergeable
                            merged
                            isDraft
                        }
                    }
                }
            ";

            var variables = new
            {
                owner,
                name = repo,
                number = prNumber
            };

            var result = await context.GraphQL.ExecuteAsync<PullRequestQueryResponse>(
                query,
                variables,
                cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                var pr = result.Value.Repository.PullRequest;

                context.Logger.LogInformation(
                    "GraphQL query successful! PR: {Title}, Author: {Author}",
                    pr.Title,
                    pr.Author.Login);

                context.Logger.LogInformation(
                    "  Stats: +{Additions} -{Deletions}, {ChangedFiles} files, {Commits} commits",
                    pr.Additions,
                    pr.Deletions,
                    pr.ChangedFiles,
                    pr.Commits.TotalCount);

                context.Logger.LogInformation(
                    "  Status: Mergeable={Mergeable}, Merged={Merged}, Draft={IsDraft}",
                    pr.Mergeable,
                    pr.Merged,
                    pr.IsDraft);

                if (pr.Reviews.TotalCount > 0)
                {
                    context.Logger.LogInformation("  Reviews ({Count}):", pr.Reviews.TotalCount);
                    foreach (var review in pr.Reviews.Nodes)
                    {
                        context.Logger.LogInformation(
                            "    - {Reviewer}: {State}",
                            review.Author.Login,
                            review.State);
                    }
                }

                if (pr.Labels.Nodes.Count > 0)
                {
                    context.Logger.LogInformation("  Labels:");
                    foreach (var label in pr.Labels.Nodes)
                    {
                        context.Logger.LogInformation("    - {Name} (#{Color})", label.Name, label.Color);
                    }
                }

                // If it's a large PR, add a comment suggesting smaller PRs
                if (pr.ChangedFiles > 20 || pr.Additions > 500)
                {
                    await AddLargePRCommentViaGraphQL(context, pr.Id, pr.ChangedFiles, pr.Additions, cancellationToken);
                }
            }
            else
            {
                context.Logger.LogError(
                    "GraphQL query failed: {ErrorCode} - {ErrorMessage}",
                    result.ErrorCode,
                    result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogError(
                ex,
                "Failed to execute GraphQL query for pull request #{PRNumber}: {ErrorMessage}",
                prNumber,
                ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Adds a comment to large PRs suggesting they be broken down.
    /// </summary>
    private async Task AddLargePRCommentViaGraphQL(
        ProbotSharpContext context,
        string prId,
        int changedFiles,
        int additions,
        CancellationToken cancellationToken)
    {
        var mutation = @"
            mutation($subjectId: ID!, $body: String!) {
                addComment(input: {subjectId: $subjectId, body: $body}) {
                    commentEdge {
                        node {
                            id
                        }
                    }
                }
            }
        ";

        var body = $@"### Large Pull Request Notice

This pull request has **{changedFiles} changed files** and **+{additions} additions**.

Consider breaking large PRs into smaller, focused changes for easier review and faster iteration.

**Benefits of smaller PRs:**
- Faster review cycles
- Easier to spot bugs
- Simpler to revert if needed
- Better git history

---
_This comment was added via GraphQL mutation using ProbotSharp's context.GraphQL helper._";

        var variables = new
        {
            subjectId = prId,
            body
        };

        var result = await context.GraphQL.ExecuteAsync<AddCommentMutationResponse>(
            mutation,
            variables,
            cancellationToken);

        if (result.IsSuccess)
        {
            context.Logger.LogInformation("Successfully added large PR comment via GraphQL mutation");
        }
        else
        {
            context.Logger.LogError(
                "GraphQL mutation failed: {ErrorCode} - {ErrorMessage}",
                result.ErrorCode,
                result.ErrorMessage);
        }
    }

    // Response types for GraphQL queries
    private record PullRequestQueryResponse(RepositoryData Repository);
    private record RepositoryData(PullRequestData PullRequest);
    private record PullRequestData(
        string Id,
        string Title,
        string Body,
        AuthorData Author,
        int Additions,
        int Deletions,
        int ChangedFiles,
        ReviewsData Reviews,
        CommentsData Comments,
        CommitsData Commits,
        LabelsData Labels,
        string Mergeable,
        bool Merged,
        bool IsDraft);
    private record AuthorData(string Login);
    private record ReviewsData(int TotalCount, List<ReviewNode> Nodes);
    private record ReviewNode(AuthorData Author, string State, string Body);
    private record CommentsData(int TotalCount);
    private record CommitsData(int TotalCount);
    private record LabelsData(List<LabelNode> Nodes);
    private record LabelNode(string Name, string Color);

    // Response types for GraphQL mutations
    private record AddCommentMutationResponse(AddCommentData AddComment);
    private record AddCommentData(CommentEdgeData CommentEdge);
    private record CommentEdgeData(CommentNodeData Node);
    private record CommentNodeData(string Id);
}
