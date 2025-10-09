// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Infrastructure.Adapters.GitHub;

/// <summary>
/// GitHub GraphQL client adapter with retry policy and error handling for GraphQL API operations.
/// Implements resilience patterns including retry with exponential backoff, circuit breaker, and timeout.
/// </summary>
public sealed partial class GitHubGraphQlClientAdapter : IGitHubGraphQlClientPort
{
    private const int MaxRetryAttempts = 3;
    private const int RetryBaseDelaySeconds = 2;
    private const int CircuitBreakerFailureThreshold = 5;
    private const int CircuitBreakerBreakDurationSeconds = 30;
    private const int TimeoutSeconds = 30;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GitHubGraphQlClientAdapter> _logger;
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubGraphQlClientAdapter"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory for GitHub GraphQL requests.</param>
    /// <param name="logger">The logger instance.</param>
    public GitHubGraphQlClientAdapter(IHttpClientFactory httpClientFactory, ILogger<GitHubGraphQlClientAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(logger);

        this._httpClientFactory = httpClientFactory;
        this._logger = logger;
        this._pipeline = CreatePipeline(logger);
    }

    /// <inheritdoc />
    public async Task<Result<TResponse>> ExecuteAsync<TResponse>(
        string query,
        object? variables = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query, nameof(query));

        try
        {
            var client = this._httpClientFactory.CreateClient("GitHubGraphQL");

            var requestBody = new
            {
                query,
                variables,
            };

            using var content = new StringContent(
                JsonSerializer.Serialize(requestBody, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await this._pipeline.ExecuteAsync(
                    async ct => await client.PostAsync(new Uri("graphql", UriKind.Relative), content, ct).ConfigureAwait(false),
                    cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                LogMessages.GraphQlRequestFailed(this._logger, response.StatusCode, errorContent);
                return Result<TResponse>.Failure(
                    "github_graphql_http_error",
                    $"GraphQL request failed with status {response.StatusCode}",
                    errorContent);
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var graphqlResponse = JsonSerializer.Deserialize<GraphQlResponse<TResponse>>(responseContent, JsonOptions);

            if (graphqlResponse?.Errors is { Length: > 0 })
            {
                var errorMessages = string.Join("; ", graphqlResponse.Errors.Select(e => e.Message));
                LogMessages.GraphQlErrorsReturned(this._logger, errorMessages);
                return Result<TResponse>.Failure(
                    "github_graphql_error",
                    "GraphQL query returned errors",
                    errorMessages);
            }

            if (graphqlResponse == null || graphqlResponse.Data == null)
            {
                LogMessages.GraphQlNoData(this._logger);
                return Result<TResponse>.Failure(
                    "github_graphql_no_data",
                    "GraphQL response contains no data");
            }

            return Result<TResponse>.Success(graphqlResponse.Data);
        }
        catch (JsonException ex)
        {
            LogMessages.GraphQlDeserializationFailed(this._logger, ex);
            return Result<TResponse>.Failure(
                "github_graphql_deserialization_error",
                "Failed to deserialize GraphQL response",
                ex.Message);
        }
        catch (Exception ex)
        {
            LogMessages.GraphQlRequestFailedUnexpected(this._logger, ex);
            return Result<TResponse>.Failure(
                "github_graphql_error",
                "GraphQL request failed",
                ex.Message);
        }
    }

    private static ResiliencePipeline<HttpResponseMessage> CreatePipeline(ILogger<GitHubGraphQlClientAdapter> logger)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            // Timeout strategy
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(TimeoutSeconds),
            })
            // Circuit breaker strategy
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = CircuitBreakerFailureThreshold,
                BreakDuration = TimeSpan.FromSeconds(CircuitBreakerBreakDurationSeconds),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(response =>
                        response.StatusCode == HttpStatusCode.TooManyRequests ||
                        (int)response.StatusCode >= 500),
            })
            // Retry strategy with exponential backoff and jitter
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(RetryBaseDelaySeconds),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(response =>
                        response.StatusCode == HttpStatusCode.TooManyRequests ||
                        response.StatusCode == HttpStatusCode.RequestTimeout ||
                        (int)response.StatusCode >= 500),
            })
            .Build();
    }

    /// <summary>
    /// Represents a GraphQL response envelope containing data or errors.
    /// </summary>
    /// <typeparam name="T">The type of the data payload.</typeparam>
    [SuppressMessage("Performance", "CA1812", Justification = "Instantiated via System.Text.Json serialization")]
    private sealed class GraphQlResponse<T>
    {
        /// <summary>Gets or sets the response data.</summary>
        public T? Data { get; set; }

        /// <summary>Gets or sets the GraphQL errors if the query failed.</summary>
        public GraphQlError[]? Errors { get; set; }
    }

    /// <summary>
    /// Represents a GraphQL error returned by the GitHub API.
    /// </summary>
    [SuppressMessage("Performance", "CA1812", Justification = "Instantiated via System.Text.Json serialization")]
    private sealed class GraphQlError
    {
        /// <summary>Gets or sets the error message.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Gets or sets the error locations in the query.</summary>
        public GraphQlErrorLocation[]? Locations { get; set; }

        /// <summary>Gets or sets the path to the field that caused the error.</summary>
        public string[]? Path { get; set; }
    }

    /// <summary>
    /// Represents the location of an error in a GraphQL query.
    /// </summary>
    [SuppressMessage("Performance", "CA1812", Justification = "Instantiated via System.Text.Json serialization")]
    private sealed class GraphQlErrorLocation
    {
        /// <summary>Gets or sets the line number in the query.</summary>
        public int Line { get; set; }

        /// <summary>Gets or sets the column number in the query.</summary>
        public int Column { get; set; }
    }
}
