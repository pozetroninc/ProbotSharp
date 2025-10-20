// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Linq;

using Octokit;

using ProbotSharp.Domain.Contracts;

namespace ProbotSharp.Domain.Context;

/// <summary>
/// Represents the context for a webhook event, providing access to the payload, GitHub API client, and related metadata.
/// This is the primary abstraction that Probot app developers interact with when handling webhook events.
/// </summary>
public sealed class ProbotSharpContext
{
    private readonly Dictionary<string, object> _metadata = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ProbotSharpContext"/> class.
    /// </summary>
    /// <param name="id">Unique delivery ID for this webhook.</param>
    /// <param name="eventName">Name of the webhook event (e.g., "issues", "pull_request").</param>
    /// <param name="eventAction">Action associated with the event (e.g., "opened", "closed"), or null if no action.</param>
    /// <param name="payload">Raw webhook payload as a JSON object.</param>
    /// <param name="logger">Logger scoped to this webhook delivery.</param>
    /// <param name="gitHub">Authenticated Octokit GitHub client for the installation.</param>
    /// <param name="graphQL">GraphQL client for executing GitHub GraphQL API queries and mutations.</param>
    /// <param name="repository">Repository information if present in the payload.</param>
    /// <param name="installation">Installation information if present in the payload.</param>
    /// <param name="isDryRun">Indicates whether the context is running in dry-run mode for safe testing of bulk operations.</param>
    public ProbotSharpContext(
        string id,
        string eventName,
        string? eventAction,
        JObject payload,
        ILogger logger,
        IGitHubClient gitHub,
        IGitHubGraphQlClient graphQL,
        RepositoryInfo? repository,
        InstallationInfo? installation,
        bool isDryRun = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(gitHub);
        ArgumentNullException.ThrowIfNull(graphQL);

        this.Id = id;
        this.EventName = eventName;
        this.EventAction = eventAction;
        this.Payload = payload;
        this.Logger = logger;
        this.GitHub = gitHub;
        this.GraphQL = graphQL;
        this.Repository = repository;
        this.Installation = installation;
        this.IsDryRun = isDryRun;
    }

    /// <summary>
    /// Gets the metadata dictionary for storing extension data.
    /// Internal to allow Application/Infrastructure layers to attach services.
    /// </summary>
    internal Dictionary<string, object> Metadata => this._metadata;

    /// <summary>
    /// Gets the unique delivery ID for this webhook event.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the name of the webhook event (e.g., "issues", "pull_request").
    /// </summary>
    public string EventName { get; }

    /// <summary>
    /// Gets the action associated with the event (e.g., "opened", "closed"), or null if no action.
    /// </summary>
    public string? EventAction { get; }

    /// <summary>
    /// Gets the raw webhook payload as a JSON object.
    /// </summary>
    public JObject Payload { get; }

    /// <summary>
    /// Gets the logger scoped to this webhook delivery.
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// Gets the authenticated Octokit GitHub client for this installation.
    /// </summary>
    public IGitHubClient GitHub { get; }

    /// <summary>
    /// Gets the GraphQL client for executing GitHub GraphQL API queries and mutations.
    /// This provides direct access to GitHub's GraphQL API, similar to context.octokit.graphql in Node.js Probot.
    /// </summary>
    /// <example>
    /// <code>
    /// var result = await context.GraphQL.ExecuteAsync&lt;MyResponse&gt;(@"
    ///   query($owner: String!, $name: String!) {
    ///     repository(owner: $owner, name: $name) {
    ///       issues(first: 10) {
    ///         nodes { title }
    ///       }
    ///     }
    ///   }
    /// ", new { owner = "myorg", name = "myrepo" });
    /// </code>
    /// </example>
    public IGitHubGraphQlClient GraphQL { get; }

    /// <summary>
    /// Gets the repository information if present in the payload.
    /// </summary>
    public RepositoryInfo? Repository { get; }

    /// <summary>
    /// Gets the installation information if present in the payload.
    /// </summary>
    public InstallationInfo? Installation { get; }

    /// <summary>
    /// Gets a value indicating whether this context is running in dry-run mode.
    /// When true, apps should log what they would do without actually making any changes.
    /// This is useful for safely testing bulk operations before executing them.
    /// Set via the PROBOT_DRY_RUN environment variable.
    /// </summary>
    public bool IsDryRun { get; }

    /// <summary>
    /// Checks if the sender of the event is a bot.
    /// </summary>
    /// <returns>True if the sender is a bot, false otherwise.</returns>
    public bool IsBot()
    {
        var sender = this.Payload["sender"];
        if (sender == null)
        {
            return false;
        }

        var type = sender["type"]?.Value<string>();
        if (type == "Bot")
        {
            return true;
        }

        var isBot = sender["type"]?.Value<string>() == "Bot";
        return isBot;
    }

    /// <summary>
    /// Deserializes the payload to a specific type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <returns>The deserialized payload.</returns>
    public T GetPayload<T>()
        where T : class
    {
        var result = this.Payload.ToObject<T>();
        if (result == null)
        {
            throw new InvalidOperationException($"Failed to deserialize payload to {typeof(T).Name}");
        }

        return result;
    }

    /// <summary>
    /// Gets the full repository name in "owner/repo" format.
    /// </summary>
    /// <returns>The repository full name.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no repository information is available.</exception>
    public string GetRepositoryFullName()
    {
        if (this.Repository == null)
        {
            throw new InvalidOperationException("No repository information available in this context");
        }

        return $"{this.Repository.Owner}/{this.Repository.Name}";
    }
}
