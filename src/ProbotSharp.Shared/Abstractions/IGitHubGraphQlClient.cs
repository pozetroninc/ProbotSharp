// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Shared.Abstractions;

/// <summary>
/// Interface for executing GitHub GraphQL API queries and mutations.
/// This abstraction allows the domain layer to depend on GraphQL functionality
/// without coupling to specific implementation details.
/// </summary>
public interface IGitHubGraphQlClient
{
    /// <summary>
    /// Executes a GraphQL query or mutation against the GitHub API.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type to deserialize the GraphQL result into.</typeparam>
    /// <param name="query">The GraphQL query or mutation string.</param>
    /// <param name="variables">Optional variables to parameterize the query.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A Result containing the deserialized response or an error.</returns>
    Task<Result<TResponse>> ExecuteAsync<TResponse>(
        string query,
        object? variables = null,
        CancellationToken cancellationToken = default);
}
