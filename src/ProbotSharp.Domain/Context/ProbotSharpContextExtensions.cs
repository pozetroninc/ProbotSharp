// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Context;

/// <summary>
/// Extension methods for <see cref="ProbotSharpContext"/> to provide additional functionality.
/// </summary>
public static class ProbotSharpContextExtensions
{
    /// <summary>
    /// Extracts repository owner, name, and issue number from the context payload.
    /// This is a convenience helper that mirrors Node.js Probot's context.issue() method.
    /// </summary>
    /// <param name="context">The probot context.</param>
    /// <returns>A tuple containing (Owner, Repo, Number).</returns>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when repository information or issue number is not found in the payload.
    /// </exception>
    /// <example>
    /// <code>
    /// var (owner, repo, number) = context.Issue();
    /// await context.GitHub.Issue.Comment.Create(owner, repo, number, "Hello!");
    /// </code>
    /// </example>
    public static (string Owner, string Repo, int Number) Issue(this ProbotSharpContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var owner = context.Repository?.Owner
            ?? throw new InvalidOperationException("Repository owner not found in payload. Ensure the webhook event includes repository information.");

        var repo = context.Repository?.Name
            ?? throw new InvalidOperationException("Repository name not found in payload. Ensure the webhook event includes repository information.");

        var number = context.Payload["issue"]?["number"]?.ToObject<int>()
            ?? throw new InvalidOperationException("Issue number not found in payload. Ensure this is an issue-related webhook event.");

        return (owner, repo, number);
    }

    /// <summary>
    /// Extracts repository owner and name from the context payload.
    /// This is a convenience helper that mirrors Node.js Probot's context.repo() method.
    /// </summary>
    /// <param name="context">The probot context.</param>
    /// <returns>A tuple containing (Owner, Repo).</returns>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when repository information is not found in the payload.
    /// </exception>
    /// <example>
    /// <code>
    /// var (owner, repo) = context.Repo();
    /// var issues = await context.GitHub.Issue.GetAllForRepository(owner, repo);
    /// </code>
    /// </example>
    public static (string Owner, string Repo) Repo(this ProbotSharpContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var owner = context.Repository?.Owner
            ?? throw new InvalidOperationException("Repository owner not found in payload. Ensure the webhook event includes repository information.");

        var repo = context.Repository?.Name
            ?? throw new InvalidOperationException("Repository name not found in payload. Ensure the webhook event includes repository information.");

        return (owner, repo);
    }

    /// <summary>
    /// Extracts repository owner, name, and pull request number from the context payload.
    /// This is a convenience helper that provides similar functionality to Node.js Probot's context.pullRequest() method.
    /// </summary>
    /// <param name="context">The probot context.</param>
    /// <returns>A tuple containing (Owner, Repo, Number).</returns>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when repository information or pull request number is not found in the payload.
    /// </exception>
    /// <example>
    /// <code>
    /// var (owner, repo, number) = context.PullRequest();
    /// var pr = await context.GitHub.PullRequest.Get(owner, repo, number);
    /// </code>
    /// </example>
    public static (string Owner, string Repo, int Number) PullRequest(this ProbotSharpContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var owner = context.Repository?.Owner
            ?? throw new InvalidOperationException("Repository owner not found in payload. Ensure the webhook event includes repository information.");

        var repo = context.Repository?.Name
            ?? throw new InvalidOperationException("Repository name not found in payload. Ensure the webhook event includes repository information.");

        var number = context.Payload["pull_request"]?["number"]?.ToObject<int>()
            ?? throw new InvalidOperationException("Pull request number not found in payload. Ensure this is a pull request-related webhook event.");

        return (owner, repo, number);
    }

    // Note: The attachment extension methods are implemented in CommentAttachmentService
    // which is injected into event handlers. The metadata extension methods are in
    // ProbotSharp.Application.Extensions.ProbotSharpContextMetadataExtensions to avoid
    // circular dependencies between Domain and Application layers.
}
