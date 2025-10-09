// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Context;

namespace GraphQLBot;

/// <summary>
/// Demonstrates GraphQL mutation patterns for creating and modifying GitHub resources.
/// Shows how to use mutations for issues, pull requests, labels, reactions, and reviews.
/// </summary>
[EventHandler("issue_comment", "created")]
public class MutationExamples : IEventHandler
{
    /// <summary>
    /// Handles issue comment created event by demonstrating various GraphQL mutations.
    /// </summary>
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Mutation examples triggered by comment on {Repository}",
            context.GetRepositoryFullName());

        if (context.IsBot())
        {
            return;
        }

        // Check if comment contains a command
        var commentBody = context.Payload["comment"]?["body"]?.ToString();
        if (string.IsNullOrEmpty(commentBody))
        {
            return;
        }

        // Execute different mutations based on comment content
        if (commentBody.Contains("/demo-create-issue"))
        {
            await CreateIssueExample(context, cancellationToken);
        }
        else if (commentBody.Contains("/demo-update-pr"))
        {
            await UpdatePullRequestExample(context, cancellationToken);
        }
        else if (commentBody.Contains("/demo-add-reaction"))
        {
            await AddReactionExample(context, cancellationToken);
        }
        else if (commentBody.Contains("/demo-request-review"))
        {
            await RequestPullRequestReviewExample(context, cancellationToken);
        }
        else if (commentBody.Contains("/demo-close-issue"))
        {
            await CloseIssueExample(context, cancellationToken);
        }
    }

    /// <summary>
    /// Demonstrates creating a new issue using GraphQL mutation.
    /// First gets the repository ID, then creates the issue with labels.
    /// </summary>
    private async Task CreateIssueExample(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        var (owner, repo) = context.Repo();

        // Step 1: Get repository ID (needed for createIssue mutation)
        var getRepoIdQuery = @"
            query($owner: String!, $name: String!) {
                repository(owner: $owner, name: $name) {
                    id
                    labels(first: 10) {
                        nodes {
                            id
                            name
                        }
                    }
                }
            }
        ";

        var repoResult = await context.GraphQL.ExecuteAsync<RepositoryIdResponse>(
            getRepoIdQuery,
            new { owner, name = repo },
            cancellationToken);

        if (!repoResult.IsSuccess || repoResult.Value == null)
        {
            context.Logger.LogError("Failed to get repository ID: {Error}", repoResult.ErrorMessage);
            return;
        }

        var repositoryId = repoResult.Value.Repository.Id;
        var bugLabel = repoResult.Value.Repository.Labels.Nodes
            .FirstOrDefault(l => l.Name == "bug");

        // Step 2: Create issue mutation
        var createIssueMutation = @"
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
                        labels(first: 5) {
                            nodes {
                                name
                            }
                        }
                    }
                }
            }
        ";

        var labelIds = bugLabel != null ? new[] { bugLabel.Id } : null;
        var variables = new
        {
            repositoryId,
            title = "Demo Issue Created via GraphQL",
            body = "This issue was created programmatically using GraphQL mutation.\n\n" +
                   "It demonstrates:\n" +
                   "- Creating issues with the createIssue mutation\n" +
                   "- Adding labels at creation time\n" +
                   "- Getting the created issue details in the response",
            labelIds
        };

        var result = await context.GraphQL.ExecuteAsync<CreateIssueResponse>(
            createIssueMutation,
            variables,
            cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            var issue = result.Value.CreateIssue.Issue;
            context.Logger.LogInformation(
                "✅ Created issue #{Number}: {Title}",
                issue.Number,
                issue.Title);
            context.Logger.LogInformation("   URL: {Url}", issue.Url);

            if (issue.Labels.Nodes.Count > 0)
            {
                var labels = string.Join(", ", issue.Labels.Nodes.Select(l => l.Name));
                context.Logger.LogInformation("   Labels: {Labels}", labels);
            }
        }
        else
        {
            context.Logger.LogError("Failed to create issue: {Error}", result.ErrorMessage);
        }
    }

    /// <summary>
    /// Demonstrates updating a pull request title and description.
    /// </summary>
    private async Task UpdatePullRequestExample(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        // This assumes we're commenting on a PR
        var prNumber = context.Payload["issue"]?["number"]?.ToObject<int>();
        if (!prNumber.HasValue)
        {
            return;
        }

        var (owner, repo) = context.Repo();

        // Get PR node ID
        var getPrIdQuery = @"
            query($owner: String!, $name: String!, $number: Int!) {
                repository(owner: $owner, name: $name) {
                    pullRequest(number: $number) {
                        id
                        title
                        body
                    }
                }
            }
        ";

        var prResult = await context.GraphQL.ExecuteAsync<PullRequestIdResponse>(
            getPrIdQuery,
            new { owner, name = repo, number = prNumber.Value },
            cancellationToken);

        if (!prResult.IsSuccess || prResult.Value == null)
        {
            context.Logger.LogError("Failed to get PR ID: {Error}", prResult.ErrorMessage);
            return;
        }

        var pr = prResult.Value.Repository.PullRequest;

        // Update PR mutation
        var updatePrMutation = @"
            mutation($pullRequestId: ID!, $title: String, $body: String) {
                updatePullRequest(input: {
                    pullRequestId: $pullRequestId,
                    title: $title,
                    body: $body
                }) {
                    pullRequest {
                        id
                        number
                        title
                        body
                    }
                }
            }
        ";

        var variables = new
        {
            pullRequestId = pr.Id,
            title = $"[Updated] {pr.Title}",
            body = $"{pr.Body}\n\n---\n\n**Note:** This PR was updated via GraphQL mutation."
        };

        var result = await context.GraphQL.ExecuteAsync<UpdatePullRequestResponse>(
            updatePrMutation,
            variables,
            cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            var updatedPr = result.Value.UpdatePullRequest.PullRequest;
            context.Logger.LogInformation(
                "✅ Updated PR #{Number}: {Title}",
                updatedPr.Number,
                updatedPr.Title);
        }
        else
        {
            context.Logger.LogError("Failed to update PR: {Error}", result.ErrorMessage);
        }
    }

    /// <summary>
    /// Demonstrates adding reactions to issues or comments.
    /// Uses the addReaction mutation with different reaction types.
    /// </summary>
    private async Task AddReactionExample(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        // Get the comment ID to add reaction to
        var commentId = context.Payload["comment"]?["node_id"]?.ToString();
        if (string.IsNullOrEmpty(commentId))
        {
            return;
        }

        var addReactionMutation = @"
            mutation($subjectId: ID!, $content: ReactionContent!) {
                addReaction(input: {
                    subjectId: $subjectId,
                    content: $content
                }) {
                    reaction {
                        id
                        content
                        user {
                            login
                        }
                    }
                    subject {
                        id
                    }
                }
            }
        ";

        // Available reactions: THUMBS_UP, THUMBS_DOWN, LAUGH, HOORAY, CONFUSED, HEART, ROCKET, EYES
        var reactions = new[] { "THUMBS_UP", "ROCKET", "HEART" };

        foreach (var reactionContent in reactions)
        {
            var variables = new
            {
                subjectId = commentId,
                content = reactionContent
            };

            var result = await context.GraphQL.ExecuteAsync<AddReactionResponse>(
                addReactionMutation,
                variables,
                cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                var reaction = result.Value.AddReaction.Reaction;
                context.Logger.LogInformation(
                    "✅ Added {Reaction} reaction by {User}",
                    reaction.Content,
                    reaction.User.Login);
            }
            else
            {
                context.Logger.LogError(
                    "Failed to add {Reaction} reaction: {Error}",
                    reactionContent,
                    result.ErrorMessage);
            }
        }
    }

    /// <summary>
    /// Demonstrates requesting pull request reviews from specific users.
    /// </summary>
    private async Task RequestPullRequestReviewExample(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        var prNumber = context.Payload["issue"]?["number"]?.ToObject<int>();
        if (!prNumber.HasValue)
        {
            return;
        }

        var (owner, repo) = context.Repo();

        // Get PR ID and potential reviewers
        var getPrQuery = @"
            query($owner: String!, $name: String!, $number: Int!) {
                repository(owner: $owner, name: $name) {
                    pullRequest(number: $number) {
                        id
                    }
                    collaborators(first: 10) {
                        nodes {
                            id
                            login
                        }
                    }
                }
            }
        ";

        var prResult = await context.GraphQL.ExecuteAsync<PullRequestReviewersResponse>(
            getPrQuery,
            new { owner, name = repo, number = prNumber.Value },
            cancellationToken);

        if (!prResult.IsSuccess || prResult.Value == null)
        {
            context.Logger.LogError("Failed to get PR info: {Error}", prResult.ErrorMessage);
            return;
        }

        var pullRequestId = prResult.Value.Repository.PullRequest.Id;
        var reviewers = prResult.Value.Repository.Collaborators.Nodes.Take(2).ToList();

        if (reviewers.Count == 0)
        {
            context.Logger.LogWarning("No collaborators found to request reviews from");
            return;
        }

        // Request reviews mutation
        var requestReviewsMutation = @"
            mutation($pullRequestId: ID!, $userIds: [ID!]!) {
                requestReviews(input: {
                    pullRequestId: $pullRequestId,
                    userIds: $userIds
                }) {
                    pullRequest {
                        id
                        number
                        reviewRequests(first: 10) {
                            totalCount
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
            pullRequestId,
            userIds = reviewers.Select(r => r.Id).ToArray()
        };

        var result = await context.GraphQL.ExecuteAsync<RequestReviewsResponse>(
            requestReviewsMutation,
            variables,
            cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            var pr = result.Value.RequestReviews.PullRequest;
            var requestedReviewers = pr.ReviewRequests.Nodes
                .Select(rr => rr.RequestedReviewer.Login)
                .ToList();

            context.Logger.LogInformation(
                "✅ Requested reviews from: {Reviewers}",
                string.Join(", ", requestedReviewers));
        }
        else
        {
            context.Logger.LogError("Failed to request reviews: {Error}", result.ErrorMessage);
        }
    }

    /// <summary>
    /// Demonstrates closing an issue with a closing comment.
    /// </summary>
    private async Task CloseIssueExample(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        var (owner, repo, issueNumber) = context.Issue();

        // Get issue ID
        var getIssueIdQuery = @"
            query($owner: String!, $name: String!, $number: Int!) {
                repository(owner: $owner, name: $name) {
                    issue(number: $number) {
                        id
                        state
                    }
                }
            }
        ";

        var issueResult = await context.GraphQL.ExecuteAsync<IssueIdResponse>(
            getIssueIdQuery,
            new { owner, name = repo, number = issueNumber },
            cancellationToken);

        if (!issueResult.IsSuccess || issueResult.Value == null)
        {
            context.Logger.LogError("Failed to get issue ID: {Error}", issueResult.ErrorMessage);
            return;
        }

        var issueId = issueResult.Value.Repository.Issue.Id;
        var currentState = issueResult.Value.Repository.Issue.State;

        if (currentState == "CLOSED")
        {
            context.Logger.LogInformation("Issue is already closed");
            return;
        }

        // Close issue mutation
        var closeIssueMutation = @"
            mutation($issueId: ID!, $reason: IssueClosedStateReason) {
                closeIssue(input: {
                    issueId: $issueId,
                    stateReason: $reason
                }) {
                    issue {
                        id
                        number
                        state
                        stateReason
                    }
                }
            }
        ";

        var variables = new
        {
            issueId,
            reason = "COMPLETED"  // Options: COMPLETED, NOT_PLANNED
        };

        var result = await context.GraphQL.ExecuteAsync<CloseIssueResponse>(
            closeIssueMutation,
            variables,
            cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            var issue = result.Value.CloseIssue.Issue;
            context.Logger.LogInformation(
                "✅ Closed issue #{Number} with reason: {Reason}",
                issue.Number,
                issue.StateReason);

            // Add closing comment
            var addCommentMutation = @"
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

            await context.GraphQL.ExecuteAsync<AddCommentResponse>(
                addCommentMutation,
                new
                {
                    subjectId = issueId,
                    body = "This issue was closed via GraphQL mutation. ✅"
                },
                cancellationToken);
        }
        else
        {
            context.Logger.LogError("Failed to close issue: {Error}", result.ErrorMessage);
        }
    }

    // Response types for mutations
    private record RepositoryIdResponse(RepositoryIdData Repository);
    private record RepositoryIdData(string Id, LabelsData Labels);
    private record LabelsData(List<LabelNode> Nodes);
    private record LabelNode(string Id, string Name);

    private record CreateIssueResponse(CreateIssueData CreateIssue);
    private record CreateIssueData(CreatedIssueData Issue);
    private record CreatedIssueData(string Id, int Number, string Title, string Url, IssueLabelsData Labels);
    private record IssueLabelsData(List<IssueLabelNode> Nodes);
    private record IssueLabelNode(string Name);

    private record PullRequestIdResponse(PullRequestIdRepositoryData Repository);
    private record PullRequestIdRepositoryData(PullRequestIdData PullRequest);
    private record PullRequestIdData(string Id, string Title, string Body);

    private record UpdatePullRequestResponse(UpdatePullRequestData UpdatePullRequest);
    private record UpdatePullRequestData(UpdatedPullRequestData PullRequest);
    private record UpdatedPullRequestData(string Id, int Number, string Title, string Body);

    private record AddReactionResponse(AddReactionData AddReaction);
    private record AddReactionData(ReactionData Reaction, ReactionSubjectData Subject);
    private record ReactionData(string Id, string Content, ReactionUserData User);
    private record ReactionUserData(string Login);
    private record ReactionSubjectData(string Id);

    private record PullRequestReviewersResponse(ReviewersRepositoryData Repository);
    private record ReviewersRepositoryData(PullRequestForReviewData PullRequest, CollaboratorsData Collaborators);
    private record PullRequestForReviewData(string Id);
    private record CollaboratorsData(List<CollaboratorNode> Nodes);
    private record CollaboratorNode(string Id, string Login);

    private record RequestReviewsResponse(RequestReviewsData RequestReviews);
    private record RequestReviewsData(PullRequestWithReviewsData PullRequest);
    private record PullRequestWithReviewsData(string Id, int Number, ReviewRequestsData ReviewRequests);
    private record ReviewRequestsData(int TotalCount, List<ReviewRequestNode> Nodes);
    private record ReviewRequestNode(RequestedReviewerData RequestedReviewer);
    private record RequestedReviewerData(string Login);

    private record IssueIdResponse(IssueIdRepositoryData Repository);
    private record IssueIdRepositoryData(IssueIdData Issue);
    private record IssueIdData(string Id, string State);

    private record CloseIssueResponse(CloseIssueData CloseIssue);
    private record CloseIssueData(ClosedIssueData Issue);
    private record ClosedIssueData(string Id, int Number, string State, string StateReason);

    private record AddCommentResponse(AddCommentData AddComment);
    private record AddCommentData(CommentEdgeData CommentEdge);
    private record CommentEdgeData(CommentNodeData Node);
    private record CommentNodeData(string Id);
}
