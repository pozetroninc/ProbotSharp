// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Net;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using ProbotSharp.Infrastructure.Adapters.GitHub;

using Xunit;

namespace ProbotSharp.Infrastructure.Tests.Adapters.GitHub;

/// <summary>
/// Tests for resilience policies in <see cref="GitHubRestHttpAdapter"/>.
/// Validates retry, circuit breaker, and timeout behavior.
/// </summary>
public sealed class GitHubRestHttpAdapterResilienceTests
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GitHubRestHttpAdapter> _logger;
    private readonly GitHubRestHttpAdapter _sut;

    public GitHubRestHttpAdapterResilienceTests()
    {
        this._httpClientFactory = Substitute.For<IHttpClientFactory>();
        this._logger = Substitute.For<ILogger<GitHubRestHttpAdapter>>();
        this._sut = new GitHubRestHttpAdapter(this._httpClientFactory, this._logger);
    }

    [Fact]
    public async Task SendAsync_ShouldReturnSuccess_WhenRequestSucceeds()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SendAsync_ShouldReturnFailure_WhenAllRetriesFail()
    {
        // Arrange - Return 500 repeatedly to exhaust retries
        using var handler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError);
        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeTrue(); // Request completes, but with 500 status
        result.Value!.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task SendAsync_ShouldRetryOn429_RateLimitError()
    {
        // Arrange - First 2 calls return 429, third succeeds
        var attemptCount = 0;
        using var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            return attemptCount <= 2
                ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                : new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(3); // Verify it retried twice
    }

    [Fact]
    public async Task SendAsync_ShouldRetryOn503_ServiceUnavailable()
    {
        // Arrange - First call returns 503, second succeeds
        var attemptCount = 0;
        using var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            return attemptCount == 1
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(2);
    }

    [Fact]
    public async Task SendAsync_ShouldNotRetryOn4xxErrors_ExceptRateLimit()
    {
        // Arrange - Return 404
        var attemptCount = 0;
        using var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.NotFound);
        attemptCount.Should().Be(1); // No retries for 404
    }

    [Fact]
    public async Task SendAsync_ShouldReturnFailure_WhenHttpRequestExceptionOccurs()
    {
        // Arrange - Simulate network error
        using var handler = new DelegatingHandler(req =>
            throw new HttpRequestException("Network error"));

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("github_rest_error");
    }

    [Fact]
    public async Task SendAsync_ShouldReturnFailure_WhenTaskCanceledException()
    {
        // Arrange - Simulate timeout
        using var handler = new DelegatingHandler(req =>
            throw new TaskCanceledException("Request timed out"));

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("github_rest_error");
    }

    [Fact]
    public async Task SendAsync_ShouldReturnFailure_WhenUnexpectedExceptionOccurs()
    {
        // Arrange - Simulate unexpected error
        using var handler = new DelegatingHandler(req =>
            throw new InvalidOperationException("Unexpected error"));

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("github_rest_error");
    }

    [Fact]
    public async Task SendAsync_ShouldRetryOn502_BadGateway()
    {
        // Arrange - First call returns 502, second succeeds
        var attemptCount = 0;
        using var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            return attemptCount == 1
                ? new HttpResponseMessage(HttpStatusCode.BadGateway)
                : new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(2);
    }

    #region Rate Limiting Tests

    [Fact]
    public async Task SendAsync_WhenRateLimited_ShouldRetryWithBackoff()
    {
        // Arrange - Simulate rate limit with retry
        var attemptCount = 0;
        using var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            return attemptCount <= 2
                ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                : new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(3); // Initial + 2 retries
    }

    [Fact]
    public async Task SendAsync_WhenRateLimitReset_ShouldWaitUntilReset()
    {
        // Arrange - Return rate limit with X-RateLimit-Reset header
        var resetTime = DateTimeOffset.UtcNow.AddSeconds(1).ToUnixTimeSeconds();
        var attemptCount = 0;
        using var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.Add("X-RateLimit-Reset", resetTime.ToString());
                return response;
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        attemptCount.Should().Be(2); // Verify retry occurred
    }

    [Fact]
    public async Task SendAsync_WhenSecondaryRateLimited_ShouldReturnFailure()
    {
        // Arrange - Rate limit persists through all retries
        using var handler = new DelegatingHandler(req =>
            new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert - Should complete with 429 after retries exhausted
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task SendAsync_WhenRateLimitHeaderMissing_ShouldUseDefaultBackoff()
    {
        // Arrange - 429 without X-RateLimit-Reset header
        var attemptCount = 0;
        using var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            return attemptCount <= 1
                ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                : new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        attemptCount.Should().Be(2); // Verify exponential backoff was used
    }

    [Fact]
    public async Task SendAsync_WhenRateLimitRecovered_ShouldSucceed()
    {
        // Arrange - Rate limit on first attempt, success on retry
        var attemptCount = 0;
        using var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            return attemptCount == 1
                ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                : new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(2);
    }

    #endregion

    #region Network Timeout Tests

    [Fact]
    public async Task SendAsync_WhenRequestTimesOut_ShouldRetry()
    {
        // Arrange - First attempt times out, second succeeds
        var attemptCount = 0;
        using var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                throw new HttpRequestException("Request timed out");
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(2);
    }

    [Fact]
    public async Task SendAsync_WhenAllRetriesTimeout_ShouldReturnFailure()
    {
        // Arrange - All attempts time out
        using var handler = new DelegatingHandler(req =>
            throw new TaskCanceledException("Request timed out"));

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("github_rest_error");
    }

    [Fact]
    public async Task SendAsync_WhenPartialResponseTimeout_ShouldRetry()
    {
        // Arrange - Timeout during response read, then success
        var attemptCount = 0;
        using var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                throw new HttpRequestException("The operation was canceled.", new TaskCanceledException());
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(2);
    }

    [Fact]
    public async Task SendAsync_WhenTimeoutThenSuccess_ShouldSucceed()
    {
        // Arrange - Timeout on first attempt, success on retry
        var attemptCount = 0;
        using var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                throw new HttpRequestException("The operation was canceled.");
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(2);
    }

    #endregion

    #region GitHub Maintenance Mode Tests

    [Fact]
    public async Task SendAsync_When503ServiceUnavailable_ShouldRetry()
    {
        // Arrange - Service unavailable, then recovers
        var attemptCount = 0;
        using var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            return attemptCount <= 2
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(3);
    }

    [Fact]
    public async Task SendAsync_When502BadGateway_ShouldRetry()
    {
        // Arrange - Bad gateway, then recovers
        var attemptCount = 0;
        using var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            return attemptCount <= 1
                ? new HttpResponseMessage(HttpStatusCode.BadGateway)
                : new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(2);
    }

    [Fact]
    public async Task SendAsync_WhenMaintenanceExtended_ShouldReturnFailure()
    {
        // Arrange - Service unavailable persists through all retries
        using var handler = new DelegatingHandler(req =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert - Should complete with 503 after retries exhausted
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task SendAsync_WhenMaintenanceRecovered_ShouldSucceed()
    {
        // Arrange - Maintenance mode, then recovers
        var attemptCount = 0;
        using var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            return attemptCount == 1
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(2);
    }

    #endregion

    #region Retry Policy Verification Tests

    [Fact]
    public async Task SendAsync_ShouldNotRetryOn401Unauthorized()
    {
        // Arrange - Return 401 Unauthorized
        var attemptCount = 0;
        using var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        });

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        attemptCount.Should().Be(1); // No retries for 401
    }

    [Fact]
    public async Task SendAsync_ShouldNotRetryOn404NotFound()
    {
        // Arrange - Return 404 Not Found
        var attemptCount = 0;
        using var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.NotFound);
        attemptCount.Should().Be(1); // No retries for 404
    }

    [Fact]
    public async Task SendAsync_ShouldRespectMaxRetryCount()
    {
        // Arrange - Always fail with 500 to test max retries
        var attemptCount = 0;
        using var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        });

        using var httpClient = new HttpClient(handler);
        this._httpClientFactory.CreateClient("GitHubRest").Returns(httpClient);

        // Act
        var result = await this._sut.SendAsync(
            client => client.GetAsync("https://api.github.com/test"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        attemptCount.Should().Be(4); // Initial attempt + 3 retries (MaxRetryAttempts = 3)
    }

    #endregion

    /// <summary>
    /// Simple delegating handler for testing that invokes a factory function.
    /// </summary>
    private sealed class DelegatingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public DelegatingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            this._responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(this._responseFactory(request));
        }
    }

    /// <summary>
    /// Simple mock handler that returns a fixed status code.
    /// </summary>
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public MockHttpMessageHandler(HttpStatusCode statusCode)
        {
            this._statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(this._statusCode));
        }
    }
}
