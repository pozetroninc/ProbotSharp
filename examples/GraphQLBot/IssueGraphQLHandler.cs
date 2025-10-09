// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Context;

namespace GraphQLBot;

/// <summary>
/// Event handler that demonstrates using GraphQL to query issue information.
/// Shows how to use context.GraphQL to execute GitHub GraphQL queries.
/// </summary>
[EventHandler("issues", "opened")]
public class IssueGraphQLHandler : IEventHandler
{
    /// <summary>
    /// Handles the issue opened event by querying issue details via GraphQL.
    /// </summary>
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Received issue opened event for {Repository}",
            context.GetRepositoryFullName());

        // Don't respond to bot-created issues
        if (context.IsBot())
        {
            context.Logger.LogDebug("Issue was created by a bot, skipping");
            return;
        }

        // Extract issue parameters
        var (owner, repo, issueNumber) = context.Issue();

        try
        {
            // Example 1: Query issue details using GraphQL
            // This demonstrates the basic GraphQL query pattern
            var query = @"
                query($owner: String!, $name: String!, $number: Int!) {
                    repository(owner: $owner, name: $name) {
                        issue(number: $number) {
                            id
                            title
                            body
                            author {
                                login
                            }
                            labels(first: 10) {
                                nodes {
                                    name
                                    color
                                }
                            }
                            reactions(first: 10) {
                                totalCount
                            }
                        }
                    }
                }
            ";

            var variables = new
            {
                owner,
                name = repo,
                number = issueNumber
            };

            var result = await context.GraphQL.ExecuteAsync<IssueQueryResponse>(
                query,
                variables,
                cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                var issue = result.Value.Repository.Issue;
                context.Logger.LogInformation(
                    "GraphQL query successful! Issue: {Title}, Author: {Author}, Labels: {LabelCount}, Reactions: {ReactionCount}",
                    issue.Title,
                    issue.Author.Login,
                    issue.Labels.Nodes.Count,
                    issue.Reactions.TotalCount);

                // Log label information
                foreach (var label in issue.Labels.Nodes)
                {
                    context.Logger.LogInformation("  - Label: {Name} (#{Color})", label.Name, label.Color);
                }

                // Example 2: Add a comment using GraphQL mutation
                // This demonstrates how to perform mutations
                await AddCommentViaGraphQL(context, issue.Id, cancellationToken);
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
                "Failed to execute GraphQL query for issue #{IssueNumber}: {ErrorMessage}",
                issueNumber,
                ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Demonstrates adding a comment using a GraphQL mutation.
    /// </summary>
    private async Task AddCommentViaGraphQL(
        ProbotSharpContext context,
        string issueId,
        CancellationToken cancellationToken)
    {
        var mutation = @"
            mutation($subjectId: ID!, $body: String!) {
                addComment(input: {subjectId: $subjectId, body: $body}) {
                    commentEdge {
                        node {
                            id
                            body
                            createdAt
                        }
                    }
                }
            }
        ";

        var variables = new
        {
            subjectId = issueId,
            body = "Thanks for opening this issue! This comment was added via GraphQL mutation using ProbotSharp's context.GraphQL helper."
        };

        var result = await context.GraphQL.ExecuteAsync<AddCommentMutationResponse>(
            mutation,
            variables,
            cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            var comment = result.Value.AddComment.CommentEdge.Node;
            context.Logger.LogInformation(
                "Successfully added comment via GraphQL mutation! Comment ID: {CommentId}, Created: {CreatedAt}",
                comment.Id,
                comment.CreatedAt);
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
    private record IssueQueryResponse(RepositoryData Repository);
    private record RepositoryData(IssueData Issue);
    private record IssueData(
        string Id,
        string Title,
        string Body,
        AuthorData Author,
        LabelsData Labels,
        ReactionsData Reactions);
    private record AuthorData(string Login);
    private record LabelsData(List<LabelNode> Nodes);
    private record LabelNode(string Name, string Color);
    private record ReactionsData(int TotalCount);

    // Response types for GraphQL mutations
    private record AddCommentMutationResponse(AddCommentData AddComment);
    private record AddCommentData(CommentEdgeData CommentEdge);
    private record CommentEdgeData(CommentNodeData Node);
    private record CommentNodeData(string Id, string Body, DateTime CreatedAt);
}
