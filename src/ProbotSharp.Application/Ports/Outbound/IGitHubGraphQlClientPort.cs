// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Port for executing GitHub GraphQL API queries and mutations.
/// Abstracts the underlying GraphQL client implementation to maintain hexagonal architecture.
/// </summary>
public interface IGitHubGraphQlClientPort
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
