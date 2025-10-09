// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ProbotSharp.Domain.Contracts;

namespace ProbotSharp.Infrastructure.Context;

/// <summary>
/// GraphQL client implementation that uses installation-specific authentication token.
/// This implementation is created per-webhook to ensure proper authentication scope.
/// </summary>
internal sealed class InstallationAuthenticatedGraphQLClient : IGitHubGraphQlClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstallationAuthenticatedGraphQLClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory for creating GitHub API clients.</param>
    /// <param name="accessToken">Installation access token for authentication. Can be null or empty for unauthenticated requests (limited use).</param>
    public InstallationAuthenticatedGraphQLClient(IHttpClientFactory httpClientFactory, string? accessToken)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        _httpClient = httpClientFactory.CreateClient("GitHubGraphQL");

        // Only set authorization header if token is provided
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }

    /// <inheritdoc/>
    public async Task<GraphQLResult<TResponse>> ExecuteAsync<TResponse>(
        string query,
        object? variables = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query, nameof(query));

        try
        {
            var requestBody = new
            {
                query,
                variables,
            };

            using var content = new StringContent(
                JsonSerializer.Serialize(requestBody, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                    new Uri("graphql", UriKind.Relative),
                    content,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return GraphQLResult<TResponse>.Failure(
                    "github_graphql_http_error",
                    $"GraphQL request failed with status {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var graphqlResponse = JsonSerializer.Deserialize<GraphQLResponse<TResponse>>(responseContent, JsonOptions);

            if (graphqlResponse?.Errors is { Length: > 0 })
            {
                var errorMessages = string.Join("; ", graphqlResponse.Errors.Select(e => e.Message));
                return GraphQLResult<TResponse>.Failure(
                    "github_graphql_error",
                    $"GraphQL query returned errors: {errorMessages}");
            }

            if (graphqlResponse == null || graphqlResponse.Data == null)
            {
                return GraphQLResult<TResponse>.Failure(
                    "github_graphql_no_data",
                    "GraphQL response contains no data");
            }

            return GraphQLResult<TResponse>.Success(graphqlResponse.Data);
        }
        catch (JsonException ex)
        {
            return GraphQLResult<TResponse>.Failure(
                "github_graphql_deserialization_error",
                $"Failed to deserialize GraphQL response: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            return GraphQLResult<TResponse>.Failure(
                "github_graphql_http_error",
                $"HTTP request failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return GraphQLResult<TResponse>.Failure(
                "github_graphql_error",
                $"GraphQL request failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Represents a GraphQL response envelope containing data or errors.
    /// </summary>
    private sealed class GraphQLResponse<T>
    {
        public T? Data { get; set; }
        public GraphQLError[]? Errors { get; set; }
    }

    /// <summary>
    /// Represents a GraphQL error returned by the GitHub API.
    /// </summary>
    private sealed class GraphQLError
    {
        public string Message { get; set; } = string.Empty;
    }
}
