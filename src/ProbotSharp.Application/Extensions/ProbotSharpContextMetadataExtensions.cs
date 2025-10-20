// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.Context;

namespace ProbotSharp.Application.Extensions;

/// <summary>
/// Extension methods for <see cref="ProbotSharpContext"/> to provide ergonomic metadata operations.
/// </summary>
public static class ProbotSharpContextMetadataExtensions
{
    /// <summary>
    /// Retrieves metadata for the current issue or pull request.
    /// </summary>
    /// <param name="context">The Probot context.</param>
    /// <param name="port">The metadata port instance.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The metadata value if found, otherwise null.</returns>
    /// <exception cref="InvalidOperationException">Thrown when repository or issue context is not available.</exception>
    public static async Task<string?> GetMetadataAsync(
        this ProbotSharpContext context,
        IMetadataPort port,
        string key,
        CancellationToken ct = default)
    {
        if (context.Repository == null)
        {
            throw new InvalidOperationException("Metadata requires repository context");
        }

        var issueNumber = GetIssueNumberFromPayload(context.Payload);
        if (issueNumber == null)
        {
            throw new InvalidOperationException("Metadata requires issue or pull request context");
        }

        return await port.GetAsync(context.Repository.Owner, context.Repository.Name, issueNumber.Value, key, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets metadata for the current issue or pull request.
    /// </summary>
    /// <param name="context">The Probot context.</param>
    /// <param name="port">The metadata port instance.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when repository or issue context is not available.</exception>
    public static async Task SetMetadataAsync(
        this ProbotSharpContext context,
        IMetadataPort port,
        string key,
        string value,
        CancellationToken ct = default)
    {
        if (context.Repository == null)
        {
            throw new InvalidOperationException("Metadata requires repository context");
        }

        var issueNumber = GetIssueNumberFromPayload(context.Payload);
        if (issueNumber == null)
        {
            throw new InvalidOperationException("Metadata requires issue or pull request context");
        }

        await port.SetAsync(context.Repository.Owner, context.Repository.Name, issueNumber.Value, key, value, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if metadata exists for the current issue or pull request.
    /// </summary>
    /// <param name="context">The Probot context.</param>
    /// <param name="port">The metadata port instance.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the metadata exists, otherwise false.</returns>
    /// <exception cref="InvalidOperationException">Thrown when repository or issue context is not available.</exception>
    public static async Task<bool> MetadataExistsAsync(
        this ProbotSharpContext context,
        IMetadataPort port,
        string key,
        CancellationToken ct = default)
    {
        if (context.Repository == null)
        {
            throw new InvalidOperationException("Metadata requires repository context");
        }

        var issueNumber = GetIssueNumberFromPayload(context.Payload);
        if (issueNumber == null)
        {
            throw new InvalidOperationException("Metadata requires issue or pull request context");
        }

        return await port.ExistsAsync(context.Repository.Owner, context.Repository.Name, issueNumber.Value, key, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes metadata for the current issue or pull request.
    /// </summary>
    /// <param name="context">The Probot context.</param>
    /// <param name="port">The metadata port instance.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when repository or issue context is not available.</exception>
    public static async Task DeleteMetadataAsync(
        this ProbotSharpContext context,
        IMetadataPort port,
        string key,
        CancellationToken ct = default)
    {
        if (context.Repository == null)
        {
            throw new InvalidOperationException("Metadata requires repository context");
        }

        var issueNumber = GetIssueNumberFromPayload(context.Payload);
        if (issueNumber == null)
        {
            throw new InvalidOperationException("Metadata requires issue or pull request context");
        }

        await port.DeleteAsync(context.Repository.Owner, context.Repository.Name, issueNumber.Value, key, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all metadata for the current issue or pull request.
    /// </summary>
    /// <param name="context">The Probot context.</param>
    /// <param name="port">The metadata port instance.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A dictionary containing all metadata key-value pairs.</returns>
    /// <exception cref="InvalidOperationException">Thrown when repository or issue context is not available.</exception>
    public static async Task<IDictionary<string, string>> GetAllMetadataAsync(
        this ProbotSharpContext context,
        IMetadataPort port,
        CancellationToken ct = default)
    {
        if (context.Repository == null)
        {
            throw new InvalidOperationException("Metadata requires repository context");
        }

        var issueNumber = GetIssueNumberFromPayload(context.Payload);
        if (issueNumber == null)
        {
            throw new InvalidOperationException("Metadata requires issue or pull request context");
        }

        return await port.GetAllAsync(context.Repository.Owner, context.Repository.Name, issueNumber.Value, ct).ConfigureAwait(false);
    }

    private static int? GetIssueNumberFromPayload(JObject payload)
    {
        // Try issue.number first
        var issueNumber = payload["issue"]?["number"]?.Value<int>();
        if (issueNumber != null)
        {
            return issueNumber;
        }

        // Try pull_request.number
        var prNumber = payload["pull_request"]?["number"]?.Value<int>();
        if (prNumber != null)
        {
            return prNumber;
        }

        return null;
    }
}
