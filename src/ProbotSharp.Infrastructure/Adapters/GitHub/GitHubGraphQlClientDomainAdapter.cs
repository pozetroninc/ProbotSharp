// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.Contracts;

namespace ProbotSharp.Infrastructure.Adapters.GitHub;

/// <summary>
/// Adapter that wraps IGitHubGraphQlClientPort and exposes it as IGitHubGraphQlClient for the domain layer.
/// This allows the domain layer to use GraphQL without depending on the Application layer.
/// </summary>
public sealed class GitHubGraphQlClientDomainAdapter : IGitHubGraphQlClient
{
    private readonly IGitHubGraphQlClientPort _port;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubGraphQlClientDomainAdapter"/> class.
    /// </summary>
    /// <param name="port">The GraphQL client port from the application layer.</param>
    public GitHubGraphQlClientDomainAdapter(IGitHubGraphQlClientPort port)
    {
        _port = port ?? throw new ArgumentNullException(nameof(port));
    }

    /// <inheritdoc/>
    public async Task<GraphQLResult<TResponse>> ExecuteAsync<TResponse>(
        string query,
        object? variables = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _port.ExecuteAsync<TResponse>(query, variables, cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            return GraphQLResult<TResponse>.Success(result.Value);
        }

        return GraphQLResult<TResponse>.Failure(
            result.Error?.Code ?? "unknown_error",
            result.Error?.Message ?? "GraphQL query failed");
    }
}
