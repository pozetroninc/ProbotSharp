// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Net;
using System.Text;
using System.Text.Json;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using ProbotSharp.Infrastructure.Adapters.GitHub;

using Xunit;

namespace ProbotSharp.Infrastructure.Tests.Adapters.GitHub;

/// <summary>
/// Tests for resilience policies in <see cref="GitHubGraphQlClientAdapter"/>.
/// Validates retry, circuit breaker, and timeout behavior for GraphQL operations.
/// </summary>
public sealed class GitHubGraphQlClientAdapterResilienceTests
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GitHubGraphQlClientAdapter> _logger;
    private readonly GitHubGraphQlClientAdapter _sut;

    public GitHubGraphQlClientAdapterResilienceTests()
    {
        this._httpClientFactory = Substitute.For<IHttpClientFactory>();
        this._logger = Substitute.For<ILogger<GitHubGraphQlClientAdapter>>();
        this._sut = new GitHubGraphQlClientAdapter(this._httpClientFactory, this._logger);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenGraphQlQuerySucceeds()
    {
        // Arrange
        var responseData = new { viewer = new { login = "testuser" } };
        var handler = new MockGraphQlHandler(HttpStatusCode.OK, responseData);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com/") };
        this._httpClientFactory.CreateClient("GitHubGraphQL").Returns(httpClient);

        var query = "query { viewer { login } }";

        // Act
        var result = await this._sut.ExecuteAsync<ViewerResponse>(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Viewer.Login.Should().Be("testuser");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenGraphQlReturnsErrors()
    {
        // Arrange
        var handler = new MockGraphQlHandler(
            HttpStatusCode.OK,
            data: null,
            errors: new[] { new { message = "Field 'invalid' doesn't exist" } });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com/") };
        this._httpClientFactory.CreateClient("GitHubGraphQL").Returns(httpClient);

        var query = "query { invalid }";

        // Act
        var result = await this._sut.ExecuteAsync<ViewerResponse>(query);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("github_graphql_error");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRetry_WhenReceiving503Error()
    {
        // Arrange - First call returns 503, second succeeds
        var attemptCount = 0;
        var responseData = new { viewer = new { login = "testuser" } };
        var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            return attemptCount == 1
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : CreateJsonResponse(HttpStatusCode.OK, new { data = responseData });
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com/") };
        this._httpClientFactory.CreateClient("GitHubGraphQL").Returns(httpClient);

        var query = "query { viewer { login } }";

        // Act
        var result = await this._sut.ExecuteAsync<ViewerResponse>(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        attemptCount.Should().Be(2); // Verify retry occurred
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRetry_WhenReceiving429RateLimitError()
    {
        // Arrange - First 2 calls return 429, third succeeds
        var attemptCount = 0;
        var responseData = new { viewer = new { login = "testuser" } };
        var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            return attemptCount <= 2
                ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                : CreateJsonResponse(HttpStatusCode.OK, new { data = responseData });
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com/") };
        this._httpClientFactory.CreateClient("GitHubGraphQL").Returns(httpClient);

        var query = "query { viewer { login } }";

        // Act
        var result = await this._sut.ExecuteAsync<ViewerResponse>(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        attemptCount.Should().Be(3); // Verify retries occurred
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotRetry_On4xxErrors()
    {
        // Arrange - Return 401 Unauthorized
        var attemptCount = 0;
        var handler = new DelegatingHandler(req =>
        {
            attemptCount++;
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com/") };
        this._httpClientFactory.CreateClient("GitHubGraphQL").Returns(httpClient);

        var query = "query { viewer { login } }";

        // Act
        var result = await this._sut.ExecuteAsync<ViewerResponse>(query);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("github_graphql_http_error");
        attemptCount.Should().Be(1); // No retries for 401
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenHttpRequestExceptionOccurs()
    {
        // Arrange - Simulate network error
        var handler = new DelegatingHandler(req =>
            throw new HttpRequestException("Network error"));

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com/") };
        this._httpClientFactory.CreateClient("GitHubGraphQL").Returns(httpClient);

        var query = "query { viewer { login } }";

        // Act
        var result = await this._sut.ExecuteAsync<ViewerResponse>(query);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("github_graphql_error");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenResponseIsNotValidJson()
    {
        // Arrange
        var handler = new DelegatingHandler(req =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent("Not valid JSON", Encoding.UTF8, "application/json");
            return response;
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com/") };
        this._httpClientFactory.CreateClient("GitHubGraphQL").Returns(httpClient);

        var query = "query { viewer { login } }";

        // Act
        var result = await this._sut.ExecuteAsync<ViewerResponse>(query);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("github_graphql_deserialization_error");
    }

    /// <summary>
    /// Test response type for GraphQL viewer query.
    /// </summary>
    private sealed record ViewerResponse(ViewerData Viewer);

    private sealed record ViewerData(string Login);

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
    /// Mock handler for GraphQL responses.
    /// </summary>
    private sealed class MockGraphQlHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly object? _data;
        private readonly object[]? _errors;

        public MockGraphQlHandler(HttpStatusCode statusCode, object? data = null, object[]? errors = null)
        {
            this._statusCode = statusCode;
            this._data = data;
            this._errors = errors;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(CreateJsonResponse(this._statusCode, new
            {
                data = this._data,
                errors = this._errors,
            }));
        }
    }

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, object content)
    {
        var response = new HttpResponseMessage(statusCode);
        response.Content = new StringContent(
            JsonSerializer.Serialize(content, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            Encoding.UTF8,
            "application/json");
        return response;
    }
}
