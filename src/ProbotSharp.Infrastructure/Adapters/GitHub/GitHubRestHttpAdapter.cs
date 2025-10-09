// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Net;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Infrastructure.Adapters.GitHub;

/// <summary>
/// HTTP adapter for interacting with the GitHub REST API via <see cref="IHttpClientFactory"/> configured clients.
/// Implements comprehensive resilience patterns including retry with jitter, circuit breaker, and timeout.
/// </summary>
/// <remarks>
/// <para><b>Resilience Policy Configuration:</b></para>
/// <list type="bullet">
/// <item><description><b>Retry Policy:</b> 3 retries with exponential backoff (base: 2 seconds) and jitter (±20%) to prevent thundering herd</description></item>
/// <item><description><b>Circuit Breaker:</b> Opens after 5 consecutive failures, breaks for 30 seconds, then allows 1 test request (half-open)</description></item>
/// <item><description><b>Timeout Policy:</b> 30-second timeout per request to prevent hung connections</description></item>
/// <item><description><b>Handles:</b> Transient HTTP errors (5xx, 408), rate limits (429), network failures, timeouts</description></item>
/// </list>
/// <para><b>Rationale:</b></para>
/// <list type="bullet">
/// <item><description>GitHub API has rate limits and occasional transient failures - retry handles these gracefully</description></item>
/// <item><description>Exponential backoff with jitter prevents synchronized retry storms from multiple clients</description></item>
/// <item><description>Circuit breaker prevents cascading failures when GitHub API is degraded</description></item>
/// <item><description>Timeout prevents thread pool exhaustion from hung requests</description></item>
/// </list>
/// </remarks>
public sealed class GitHubRestHttpAdapter : IGitHubRestClientPort
{
    /// <summary>Maximum number of retry attempts for transient failures.</summary>
    private const int MaxRetryAttempts = 3;

    /// <summary>Base delay in seconds for exponential backoff (2^attempt * base).</summary>
    private const int RetryBaseDelaySeconds = 2;

    /// <summary>Jitter factor (±20%) to add randomness to retry delays.</summary>
    private const double RetryJitterFactor = 0.2;

    /// <summary>Number of consecutive failures before circuit breaker opens.</summary>
    private const int CircuitBreakerFailureThreshold = 5;

    /// <summary>Duration in seconds to keep circuit breaker open before allowing test request.</summary>
    private const int CircuitBreakerBreakDurationSeconds = 30;

    /// <summary>Timeout in seconds for each HTTP request.</summary>
    private const int TimeoutSeconds = 30;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GitHubRestHttpAdapter> _logger;
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubRestHttpAdapter"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory used to resolve named GitHub HTTP clients.</param>
    /// <param name="logger">The application logger.</param>
    public GitHubRestHttpAdapter(IHttpClientFactory httpClientFactory, ILogger<GitHubRestHttpAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(logger);

        this._httpClientFactory = httpClientFactory;
        this._logger = logger;
        this._pipeline = CreatePipeline(logger);
    }

    /// <inheritdoc />
    public async Task<Result<HttpResponseMessage>> SendAsync(Func<HttpClient, Task<HttpResponseMessage>> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            var client = this._httpClientFactory.CreateClient("GitHubRest");

            var response = await this._pipeline.ExecuteAsync(
                    async ct => await action(client).ConfigureAwait(false),
                    cancellationToken)
                .ConfigureAwait(false);
            return Result<HttpResponseMessage>.Success(response);
        }
        catch (BrokenCircuitException ex)
        {
            LogMessages.GitHubRestCircuitBreakerRejected(this._logger);
            return Result<HttpResponseMessage>.Failure("github_rest_circuit_breaker_open", "Circuit breaker is open due to repeated failures. Please try again later.");
        }
        catch (TimeoutRejectedException ex)
        {
            LogMessages.GitHubRestTimeout(this._logger, TimeoutSeconds * 1000, ex);
            return Result<HttpResponseMessage>.Failure("github_rest_timeout", $"Request timed out after {TimeoutSeconds} seconds.");
        }
#pragma warning disable CA1031 // Catching general exception is intentional to convert all HTTP errors to Result type
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogMessages.GitHubRestCallFailed(this._logger, ex);
            return Result<HttpResponseMessage>.Failure("github_rest_error", ex.Message);
        }
    }

    /// <summary>
    /// Creates the composite resilience pipeline with retry, circuit breaker, and timeout.
    /// </summary>
    /// <param name="logger">Logger instance for pipeline event logging.</param>
    /// <returns>Configured pipeline wrapping all resilience strategies.</returns>
    private static ResiliencePipeline<HttpResponseMessage> CreatePipeline(ILogger<GitHubRestHttpAdapter> logger)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            // Timeout strategy - outermost layer
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(TimeoutSeconds),
            })
            // Circuit breaker strategy - prevents cascading failures
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
                OnOpened = args =>
                {
                    LogMessages.GitHubRestCircuitBreakerOpened(
                        logger,
                        CircuitBreakerFailureThreshold,
                        CircuitBreakerBreakDurationSeconds * 1000);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    LogMessages.GitHubRestCircuitBreakerReset(logger);
                    return ValueTask.CompletedTask;
                },
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
                OnRetry = args =>
                {
                    LogMessages.GitHubRestRetrying(
                        logger,
                        args.AttemptNumber + 1,
                        MaxRetryAttempts,
                        args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                },
            })
            .Build();
    }
}
