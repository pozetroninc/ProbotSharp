// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using System.Text;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using ProbotSharp.Infrastructure.Adapters.GitHub;

using RichardSzalay.MockHttp;

using Xunit;

namespace ProbotSharp.Infrastructure.Tests.Adapters.GitHub;

/// <summary>
/// Contract tests for <see cref="GitHubRestHttpAdapter"/> to verify correct handling of GitHub REST API responses.
/// These tests ensure the adapter correctly processes various GitHub API response scenarios including
/// success responses, error responses, rate limiting, and transient failures.
/// </summary>
/// <remarks>
/// Uses MockHttp to simulate GitHub API responses based on real GitHub API contracts.
/// Test fixtures are stored in Fixtures/github-api-responses/ directory.
/// </remarks>
public class GitHubRestHttpAdapterContractTests
{
    private readonly MockHttpMessageHandler _mockHttp = new();
    private readonly ILogger<GitHubRestHttpAdapter> _logger = Substitute.For<ILogger<GitHubRestHttpAdapter>>();

    /// <summary>
    /// Verifies that the adapter successfully processes a valid GitHub App info response (GET /app).
    /// </summary>
    [Fact]
    public async Task SendAsync_ShouldReturnSuccess_WhenGetAppInfoReturns200()
    {
        // Arrange
        var clientFactory = CreateFactory();
        var appInfoJson = await File.ReadAllTextAsync(
            "Fixtures/github-api-responses/app-info-200.json");
        _mockHttp
            .When("https://api.github.com/app")
            .Respond("application/json", appInfoJson);

        var adapter = new GitHubRestHttpAdapter(clientFactory, _logger);

        // Act
        var result = await adapter.SendAsync(client => client.GetAsync("https://api.github.com/app"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await result.Value.Content.ReadAsStringAsync();
        content.Should().Contain("my-test-app");
        content.Should().Contain("\"id\": 123456");
    }

    /// <summary>
    /// Verifies that the adapter successfully processes a valid installations list response (GET /app/installations).
    /// </summary>
    [Fact]
    public async Task SendAsync_ShouldReturnSuccess_WhenGetInstallationsReturns200()
    {
        // Arrange
        var clientFactory = CreateFactory();
        var installationsJson = await File.ReadAllTextAsync(
            "Fixtures/github-api-responses/installations-200.json");
        _mockHttp
            .When("https://api.github.com/app/installations")
            .Respond("application/json", installationsJson);

        var adapter = new GitHubRestHttpAdapter(clientFactory, _logger);

        // Act
        var result = await adapter.SendAsync(client => client.GetAsync("https://api.github.com/app/installations"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await result.Value.Content.ReadAsStringAsync();
        content.Should().Contain("octocat");
        content.Should().Contain("acme-org");
    }

    /// <summary>
    /// Verifies that the adapter successfully processes a valid repository response (GET /repos/{owner}/{repo}).
    /// </summary>
    [Fact]
    public async Task SendAsync_ShouldReturnSuccess_WhenGetRepositoryReturns200()
    {
        // Arrange
        var clientFactory = CreateFactory();
        var repositoryJson = await File.ReadAllTextAsync(
            "Fixtures/github-api-responses/repository-200.json");
        _mockHttp
            .When("https://api.github.com/repos/octocat/test-repo")
            .Respond("application/json", repositoryJson);

        var adapter = new GitHubRestHttpAdapter(clientFactory, _logger);

        // Act
        var result = await adapter.SendAsync(client => client.GetAsync("https://api.github.com/repos/octocat/test-repo"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await result.Value.Content.ReadAsStringAsync();
        content.Should().Contain("test-repo");
        content.Should().Contain("\"full_name\": \"octocat/test-repo\"");
    }

    /// <summary>
    /// Verifies that the adapter returns success result with 401 status when authentication fails.
    /// The adapter wraps HTTP responses in Result type, so even error responses are "successful" Results.
    /// </summary>
    [Fact]
    public async Task SendAsync_ShouldReturnSuccessWithStatus401_WhenUnauthorized()
    {
        // Arrange
        var clientFactory = CreateFactory();
        var errorJson = await File.ReadAllTextAsync(
            "Fixtures/github-api-responses/error-401.json");
        _mockHttp
            .When("https://api.github.com/app")
            .Respond(HttpStatusCode.Unauthorized, "application/json", errorJson);

        var adapter = new GitHubRestHttpAdapter(clientFactory, _logger);

        // Act
        var result = await adapter.SendAsync(client => client.GetAsync("https://api.github.com/app"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await result.Value.Content.ReadAsStringAsync();
        content.Should().Contain("Bad credentials");
    }

    /// <summary>
    /// Verifies that the adapter returns success result with 403 status when resource is not accessible.
    /// This typically happens when the GitHub App doesn't have sufficient permissions.
    /// </summary>
    [Fact]
    public async Task SendAsync_ShouldReturnSuccessWithStatus403_WhenForbidden()
    {
        // Arrange
        var clientFactory = CreateFactory();
        var errorJson = await File.ReadAllTextAsync(
            "Fixtures/github-api-responses/error-403.json");
        _mockHttp
            .When("https://api.github.com/app/installations")
            .Respond(HttpStatusCode.Forbidden, "application/json", errorJson);

        var adapter = new GitHubRestHttpAdapter(clientFactory, _logger);

        // Act
        var result = await adapter.SendAsync(client => client.GetAsync("https://api.github.com/app/installations"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await result.Value.Content.ReadAsStringAsync();
        content.Should().Contain("Resource not accessible by integration");
    }

    /// <summary>
    /// Verifies that the adapter returns success result with 404 status when resource is not found.
    /// </summary>
    [Fact]
    public async Task SendAsync_ShouldReturnSuccessWithStatus404_WhenNotFound()
    {
        // Arrange
        var clientFactory = CreateFactory();
        var errorJson = await File.ReadAllTextAsync(
            "Fixtures/github-api-responses/error-404.json");
        _mockHttp
            .When("https://api.github.com/repos/octocat/nonexistent")
            .Respond(HttpStatusCode.NotFound, "application/json", errorJson);

        var adapter = new GitHubRestHttpAdapter(clientFactory, _logger);

        // Act
        var result = await adapter.SendAsync(client => client.GetAsync("https://api.github.com/repos/octocat/nonexistent"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await result.Value.Content.ReadAsStringAsync();
        content.Should().Contain("Not Found");
    }

    /// <summary>
    /// Verifies that the adapter returns success result with 422 status when validation fails.
    /// This occurs when the request body doesn't meet GitHub's validation requirements.
    /// </summary>
    [Fact]
    public async Task SendAsync_ShouldReturnSuccessWithStatus422_WhenUnprocessableEntity()
    {
        // Arrange
        var clientFactory = CreateFactory();
        var errorJson = await File.ReadAllTextAsync(
            "Fixtures/github-api-responses/error-422.json");
        _mockHttp
            .When("https://api.github.com/repos/octocat/test-repo/issues")
            .Respond(HttpStatusCode.UnprocessableEntity, "application/json", errorJson);

        var adapter = new GitHubRestHttpAdapter(clientFactory, _logger);

        // Act
        var requestBody = new StringContent("{\"body\":\"Test issue\"}", Encoding.UTF8, "application/json");
        var result = await adapter.SendAsync(client =>
            client.PostAsync("https://api.github.com/repos/octocat/test-repo/issues", requestBody));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var content = await result.Value.Content.ReadAsStringAsync();
        content.Should().Contain("Validation Failed");
        content.Should().Contain("missing_field");
    }

    /// <summary>
    /// Verifies that the adapter retries on 500 Internal Server Error and eventually returns failure result.
    /// 500 errors are considered transient and trigger the retry policy.
    /// </summary>
    [Fact]
    public async Task SendAsync_ShouldRetry_WhenInternalServerError()
    {
        // Arrange
        var clientFactory = CreateFactory();
        var errorJson = await File.ReadAllTextAsync(
            "Fixtures/github-api-responses/error-500.json");

        var callCount = 0;
        _mockHttp
            .When("https://api.github.com/app")
            .Respond(_ =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(errorJson, Encoding.UTF8, "application/json"),
                };
            });

        var adapter = new GitHubRestHttpAdapter(clientFactory, _logger);

        // Act
        var result = await adapter.SendAsync(client => client.GetAsync("https://api.github.com/app"));

        // Assert - After 3 retries (initial + 3 retries = 4 total calls), should return the error response
        callCount.Should().Be(4);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    /// <summary>
    /// Verifies that the adapter handles rate limiting (429) by retrying with exponential backoff.
    /// GitHub's rate limit responses should trigger retry behavior.
    /// </summary>
    [Fact]
    public async Task SendAsync_ShouldRetry_WhenRateLimitExceeded()
    {
        // Arrange
        var clientFactory = CreateFactory();
        var rateLimitJson = await File.ReadAllTextAsync(
            "Fixtures/github-api-responses/rate-limit-429.json");

        var callCount = 0;
        _mockHttp
            .When("https://api.github.com/app/installations")
            .Respond(_ =>
            {
                callCount++;
                return new HttpResponseMessage((HttpStatusCode)429)
                {
                    Content = new StringContent(rateLimitJson, Encoding.UTF8, "application/json"),
                    Headers =
                    {
                        { "X-RateLimit-Limit", "5000" },
                        { "X-RateLimit-Remaining", "0" },
                        { "X-RateLimit-Reset", DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds().ToString() },
                    },
                };
            });

        var adapter = new GitHubRestHttpAdapter(clientFactory, _logger);

        // Act
        var result = await adapter.SendAsync(client => client.GetAsync("https://api.github.com/app/installations"));

        // Assert - Should retry 3 times (initial + 3 retries = 4 total calls)
        callCount.Should().Be(4);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StatusCode.Should().Be((HttpStatusCode)429);
        result.Value.Headers.Should().ContainKey("X-RateLimit-Remaining");
    }

    /// <summary>
    /// Verifies that the adapter retries on 503 Service Unavailable and succeeds after recovery.
    /// This tests transient failure handling with eventual success.
    /// </summary>
    [Fact]
    public async Task SendAsync_ShouldRetryAndSucceed_WhenServiceUnavailableThenRecovered()
    {
        // Arrange
        var clientFactory = CreateFactory();
        var errorJson = await File.ReadAllTextAsync(
            "Fixtures/github-api-responses/error-503.json");
        var successJson = await File.ReadAllTextAsync(
            "Fixtures/github-api-responses/app-info-200.json");

        var callCount = 0;
        _mockHttp
            .When("https://api.github.com/app")
            .Respond(_ =>
            {
                callCount++;
                if (callCount <= 2)
                {
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    {
                        Content = new StringContent(errorJson, Encoding.UTF8, "application/json"),
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(successJson, Encoding.UTF8, "application/json"),
                };
            });

        var adapter = new GitHubRestHttpAdapter(clientFactory, _logger);

        // Act
        var result = await adapter.SendAsync(client => client.GetAsync("https://api.github.com/app"));

        // Assert - Should succeed after 2 failures
        callCount.Should().Be(3);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await result.Value.Content.ReadAsStringAsync();
        content.Should().Contain("my-test-app");
    }

    /// <summary>
    /// Verifies that the adapter eventually fails after exhausting all retries on persistent 503 errors.
    /// </summary>
    [Fact]
    public async Task SendAsync_ShouldRetryAndFail_WhenPersistentServiceUnavailable()
    {
        // Arrange
        var clientFactory = CreateFactory();
        var errorJson = await File.ReadAllTextAsync(
            "Fixtures/github-api-responses/error-503.json");

        var callCount = 0;
        _mockHttp
            .When("https://api.github.com/app")
            .Respond(_ =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent(errorJson, Encoding.UTF8, "application/json"),
                };
            });

        var adapter = new GitHubRestHttpAdapter(clientFactory, _logger);

        // Act
        var result = await adapter.SendAsync(client => client.GetAsync("https://api.github.com/app"));

        // Assert - Should retry 3 times (initial + 3 retries = 4 total calls)
        callCount.Should().Be(4);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    /// Verifies that POST requests work correctly with success responses.
    /// </summary>
    [Fact]
    public async Task SendAsync_ShouldHandlePostRequest_WhenCreatingResource()
    {
        // Arrange
        var clientFactory = CreateFactory();
        var responseJson = "{\"id\": 123, \"title\": \"Test Issue\", \"state\": \"open\"}";
        _mockHttp
            .When(HttpMethod.Post, "https://api.github.com/repos/octocat/test-repo/issues")
            .Respond("application/json", responseJson);

        var adapter = new GitHubRestHttpAdapter(clientFactory, _logger);

        // Act
        var requestBody = new StringContent("{\"title\":\"Test Issue\",\"body\":\"Test body\"}", Encoding.UTF8, "application/json");
        var result = await adapter.SendAsync(client =>
            client.PostAsync("https://api.github.com/repos/octocat/test-repo/issues", requestBody));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await result.Value.Content.ReadAsStringAsync();
        content.Should().Contain("Test Issue");
    }

    /// <summary>
    /// Verifies that PATCH requests work correctly with success responses.
    /// </summary>
    [Fact]
    public async Task SendAsync_ShouldHandlePatchRequest_WhenUpdatingResource()
    {
        // Arrange
        var clientFactory = CreateFactory();
        var responseJson = "{\"id\": 123, \"title\": \"Updated Issue\", \"state\": \"closed\"}";
        _mockHttp
            .When(HttpMethod.Patch, "https://api.github.com/repos/octocat/test-repo/issues/123")
            .Respond("application/json", responseJson);

        var adapter = new GitHubRestHttpAdapter(clientFactory, _logger);

        // Act
        var requestBody = new StringContent("{\"state\":\"closed\"}", Encoding.UTF8, "application/json");
        var result = await adapter.SendAsync(client =>
            client.PatchAsync("https://api.github.com/repos/octocat/test-repo/issues/123", requestBody));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await result.Value.Content.ReadAsStringAsync();
        content.Should().Contain("Updated Issue");
        content.Should().Contain("closed");
    }

    /// <summary>
    /// Verifies that DELETE requests work correctly with success responses.
    /// </summary>
    [Fact]
    public async Task SendAsync_ShouldHandleDeleteRequest_WhenDeletingResource()
    {
        // Arrange
        var clientFactory = CreateFactory();
        _mockHttp
            .When(HttpMethod.Delete, "https://api.github.com/repos/octocat/test-repo/issues/123")
            .Respond(HttpStatusCode.NoContent);

        var adapter = new GitHubRestHttpAdapter(clientFactory, _logger);

        // Act
        var result = await adapter.SendAsync(client =>
            client.DeleteAsync("https://api.github.com/repos/octocat/test-repo/issues/123"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Verifies that the adapter correctly includes GitHub-specific headers in responses.
    /// </summary>
    [Fact]
    public async Task SendAsync_ShouldPreserveGitHubHeaders_InResponse()
    {
        // Arrange
        var clientFactory = CreateFactory();
        var appInfoJson = await File.ReadAllTextAsync(
            "Fixtures/github-api-responses/app-info-200.json");

        _mockHttp
            .When("https://api.github.com/app")
            .Respond(request =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(appInfoJson, Encoding.UTF8, "application/json"),
                };
                response.Headers.Add("X-GitHub-Request-Id", "ABC123:5678:9ABCDEF:1234567:507F1F77");
                response.Headers.Add("X-RateLimit-Limit", "5000");
                response.Headers.Add("X-RateLimit-Remaining", "4999");
                response.Headers.Add("X-RateLimit-Reset", "1234567890");
                return response;
            });

        var adapter = new GitHubRestHttpAdapter(clientFactory, _logger);

        // Act
        var result = await adapter.SendAsync(client => client.GetAsync("https://api.github.com/app"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Headers.Should().ContainKey("X-GitHub-Request-Id");
        result.Value.Headers.Should().ContainKey("X-RateLimit-Limit");
        result.Value.Headers.Should().ContainKey("X-RateLimit-Remaining");
        result.Value.Headers.Should().ContainKey("X-RateLimit-Reset");
    }

    /// <summary>
    /// Verifies that the adapter handles network errors gracefully by returning a failure result.
    /// </summary>
    [Fact]
    public async Task SendAsync_ShouldReturnFailure_WhenNetworkError()
    {
        // Arrange
        var clientFactory = CreateFactory();
        _mockHttp
            .When("https://api.github.com/app")
            .Throw(new HttpRequestException("Network error"));

        var adapter = new GitHubRestHttpAdapter(clientFactory, _logger);

        // Act
        var result = await adapter.SendAsync(client => client.GetAsync("https://api.github.com/app"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("github_rest_error");
        result.Error.Value.Message.Should().Contain("Network error");
    }

    /// <summary>
    /// Verifies that the adapter passes through cancellation tokens correctly.
    /// </summary>
    [Fact]
    public async Task SendAsync_ShouldRespectCancellation_WhenTokenIsCancelled()
    {
        // Arrange
        var clientFactory = CreateFactory();
        _mockHttp
            .When("https://api.github.com/app")
            .Respond(async request =>
            {
                await Task.Delay(1000);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var adapter = new GitHubRestHttpAdapter(clientFactory, _logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await adapter.SendAsync(
            client => client.GetAsync("https://api.github.com/app"),
            cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("github_rest_error");
    }

    /// <summary>
    /// Creates a mock HTTP client factory configured with the mock HTTP handler.
    /// </summary>
    /// <returns>A configured HTTP client factory.</returns>
    private IHttpClientFactory CreateFactory()
    {
        var client = new HttpClient(_mockHttp) { BaseAddress = new Uri("https://api.github.com/") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("GitHubRest").Returns(client);
        return factory;
    }
}
