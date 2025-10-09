// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Contracts;

/// <summary>
/// Result wrapper for operations that may succeed or fail.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public sealed class GraphQLResult<T>
{
    private GraphQLResult(T? value, bool isSuccess, string? errorCode, string? errorMessage)
    {
        this.Value = value;
        this.IsSuccess = isSuccess;
        this.ErrorCode = errorCode;
        this.ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the value if the operation was successful.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the error code if the operation failed.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static GraphQLResult<T> Success(T value) => new(value, true, null, null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static GraphQLResult<T> Failure(string errorCode, string errorMessage) =>
        new(default, false, errorCode, errorMessage);
}

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
    /// <returns>A GraphQLResult containing the deserialized response or an error.</returns>
    Task<GraphQLResult<TResponse>> ExecuteAsync<TResponse>(
        string query,
        object? variables = null,
        CancellationToken cancellationToken = default);
}
