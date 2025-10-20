// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using ProbotSharp.Infrastructure.Adapters.GitHub;

using RichardSzalay.MockHttp;

namespace ProbotSharp.Infrastructure.Tests.Adapters.GitHub;

public class GitHubGraphQlClientAdapterTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttp = new();
    private readonly ILogger<GitHubGraphQlClientAdapter> _logger = Substitute.For<ILogger<GitHubGraphQlClientAdapter>>();
    private bool _disposed;

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccessResult_WhenRequestSucceeds()
    {
        var clientFactory = CreateFactory();
        var expectedData = new TestResponse { Value = "test-value" };
        var graphqlResponse = new
        {
            data = expectedData
        };
        _mockHttp.When("https://api.github.com/graphql")
            .Respond("application/json", JsonSerializer.Serialize(graphqlResponse));

        var adapter = new GitHubGraphQlClientAdapter(clientFactory, _logger);

        var result = await adapter.ExecuteAsync<TestResponse>("{ viewer { login } }");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Value.Should().Be("test-value");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccessResult_WithVariables()
    {
        var clientFactory = CreateFactory();
        var expectedData = new TestResponse { Value = "test-value" };
        var graphqlResponse = new
        {
            data = expectedData
        };
        _mockHttp.When("https://api.github.com/graphql")
            .Respond("application/json", JsonSerializer.Serialize(graphqlResponse));

        var adapter = new GitHubGraphQlClientAdapter(clientFactory, _logger);
        var variables = new { owner = "test-owner", name = "test-repo" };

        var result = await adapter.ExecuteAsync<TestResponse>(
            "query($owner: String!, $name: String!) { repository(owner: $owner, name: $name) { id } }",
            variables);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Value.Should().Be("test-value");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenHttpRequestFails()
    {
        var clientFactory = CreateFactory();
        _mockHttp.When("https://api.github.com/graphql")
            .Respond(HttpStatusCode.InternalServerError);

        var adapter = new GitHubGraphQlClientAdapter(clientFactory, _logger);

        var result = await adapter.ExecuteAsync<TestResponse>("{ viewer { login } }");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("github_graphql_http_error");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenGraphQLReturnsErrors()
    {
        var clientFactory = CreateFactory();
        var graphqlResponse = new
        {
            data = (TestResponse?)null,
            errors = new[]
            {
                new { message = "Field 'invalid' doesn't exist on type 'Query'" }
            }
        };
        _mockHttp.When("https://api.github.com/graphql")
            .Respond("application/json", JsonSerializer.Serialize(graphqlResponse));

        var adapter = new GitHubGraphQlClientAdapter(clientFactory, _logger);

        var result = await adapter.ExecuteAsync<TestResponse>("{ invalid }");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("github_graphql_error");
        result.Error.Value.Message.Should().Be("GraphQL query returned errors");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenResponseContainsNoData()
    {
        var clientFactory = CreateFactory();
        var graphqlResponse = new
        {
            data = (TestResponse?)null
        };
        _mockHttp.When("https://api.github.com/graphql")
            .Respond("application/json", JsonSerializer.Serialize(graphqlResponse));

        var adapter = new GitHubGraphQlClientAdapter(clientFactory, _logger);

        var result = await adapter.ExecuteAsync<TestResponse>("{ viewer { login } }");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("github_graphql_no_data");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenDeserializationFails()
    {
        var clientFactory = CreateFactory();
        _mockHttp.When("https://api.github.com/graphql")
            .Respond("application/json", "invalid json");

        var adapter = new GitHubGraphQlClientAdapter(clientFactory, _logger);

        var result = await adapter.ExecuteAsync<TestResponse>("{ viewer { login } }");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("github_graphql_deserialization_error");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowArgumentException_WhenQueryIsNullOrEmpty()
    {
        var clientFactory = CreateFactory();
        var adapter = new GitHubGraphQlClientAdapter(clientFactory, _logger);

        var act = async () => await adapter.ExecuteAsync<TestResponse>("");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRetryOnTransientErrors()
    {
        var clientFactory = CreateFactory();
        var callCount = 0;
        var expectedData = new TestResponse { Value = "test-value" };
        var graphqlResponse = new
        {
            data = expectedData
        };

        _mockHttp.When("https://api.github.com/graphql").Respond(_ =>
        {
            callCount++;
            return callCount < 3
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(graphqlResponse))
                };
        });

        var adapter = new GitHubGraphQlClientAdapter(clientFactory, _logger);

        var result = await adapter.ExecuteAsync<TestResponse>("{ viewer { login } }");

        result.IsSuccess.Should().BeTrue();
        callCount.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRetryOnRateLimit()
    {
        var clientFactory = CreateFactory();
        var callCount = 0;
        var expectedData = new TestResponse { Value = "test-value" };
        var graphqlResponse = new
        {
            data = expectedData
        };

        _mockHttp.When("https://api.github.com/graphql").Respond(_ =>
        {
            callCount++;
            return callCount < 2
                ? new HttpResponseMessage((HttpStatusCode)429)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(graphqlResponse))
                };
        });

        var adapter = new GitHubGraphQlClientAdapter(clientFactory, _logger);

        var result = await adapter.ExecuteAsync<TestResponse>("{ viewer { login } }");

        result.IsSuccess.Should().BeTrue();
        callCount.Should().Be(2);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _mockHttp?.Dispose();
            _disposed = true;
        }
    }

    private IHttpClientFactory CreateFactory()
    {
#pragma warning disable CA2000 // HttpClient is intentionally not disposed - used by mock factory for multiple test calls
        var client = new HttpClient(this._mockHttp) { BaseAddress = new Uri("https://api.github.com/") };
#pragma warning restore CA2000
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("GitHubGraphQL").Returns(client);
        return factory;
    }

    private sealed class TestResponse
    {
        public string Value { get; set; } = string.Empty;
    }
}
