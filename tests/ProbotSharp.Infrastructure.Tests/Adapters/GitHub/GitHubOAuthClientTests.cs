// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using NSubstitute;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Infrastructure.Adapters.GitHub;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Infrastructure.Tests.Adapters.GitHub;

public class GitHubOAuthClientTests : IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly IAccessTokenCachePort _cache = Substitute.For<IAccessTokenCachePort>();
    private readonly ILogger<GitHubOAuthClient> _logger = Substitute.For<ILogger<GitHubOAuthClient>>();
    private readonly GitHubOAuthClient _sut;
    private readonly HttpClient _client;
    private readonly HttpMessageHandler _handler;
    private bool _disposed;

    public GitHubOAuthClientTests()
    {
        var messageHandler = new TestHttpMessageHandler();
        _handler = messageHandler;
        _client = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("https://api.github.com/")
        };
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("ProbotSharp/1.0");
        _client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        _httpClientFactory.CreateClient("GitHubOAuth").Returns(_client);
        _httpClientFactory.CreateClient("GitHubRest").Returns(_client);
        _sut = new GitHubOAuthClient(_httpClientFactory, _cache, _logger);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _client?.Dispose();
            _handler?.Dispose();
            _disposed = true;
        }
    }

    [Fact]
    public async Task CreateInstallationTokenAsync_ShouldCacheAndReturnToken()
    {
        var installationId = InstallationId.Create(123);
        _cache.GetAsync(installationId, Arg.Any<CancellationToken>()).Returns((InstallationAccessToken?)null);

        ((TestHttpMessageHandler)_handler).ConfigureResponse(request =>
        {
            request.RequestUri!.AbsolutePath.Should().Be($"/app/installations/{installationId.Value}/access_tokens");
            request.Method.Should().Be(HttpMethod.Post);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $"{{\"token\":\"token\",\"expires_at\":\"{DateTimeOffset.UtcNow.AddHours(1):O}\"}}",
                    Encoding.UTF8,
                    "application/json")
            };
        });

        var result = await _sut.CreateInstallationTokenAsync(installationId);

        result.IsSuccess.Should().BeTrue($"GitHubOAuthClient should return success when GitHub responds with 200 but returned {result.Error?.Code}:{result.Error?.Message}");
        result.Value.Should().NotBeNull();
        await _cache.Received(1).SetAsync(installationId, Arg.Any<InstallationAccessToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateInstallationTokenAsync_WhenResponseFails_ShouldReturnFailure()
    {
        var installationId = InstallationId.Create(123);
        _cache.GetAsync(installationId, Arg.Any<CancellationToken>()).Returns((InstallationAccessToken?)null);

        ((TestHttpMessageHandler)_handler).ConfigureResponse(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("oops")
        });

        var result = await _sut.CreateInstallationTokenAsync(installationId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        await _cache.DidNotReceive().SetAsync(Arg.Any<InstallationId>(), Arg.Any<InstallationAccessToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateInstallationTokenAsync_WhenUnauthorized_ShouldReturnFailure()
    {
        var installationId = InstallationId.Create(123);
        _cache.GetAsync(installationId, Arg.Any<CancellationToken>()).Returns((InstallationAccessToken?)null);

        ((TestHttpMessageHandler)_handler).ConfigureResponse(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"message\":\"Bad credentials\"}", Encoding.UTF8, "application/json")
        });

        var result = await _sut.CreateInstallationTokenAsync(installationId);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("github_installation_token_failed");
    }

    [Fact]
    public async Task CreateInstallationTokenAsync_WhenNotFound_ShouldReturnFailure()
    {
        var installationId = InstallationId.Create(999);
        _cache.GetAsync(installationId, Arg.Any<CancellationToken>()).Returns((InstallationAccessToken?)null);

        ((TestHttpMessageHandler)_handler).ConfigureResponse(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"message\":\"Installation not found\"}", Encoding.UTF8, "application/json")
        });

        var result = await _sut.CreateInstallationTokenAsync(installationId);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("github_installation_token_failed");
    }

    [Fact]
    public async Task CreateInstallationTokenAsync_WhenInvalidJson_ShouldReturnFailure()
    {
        var installationId = InstallationId.Create(123);
        _cache.GetAsync(installationId, Arg.Any<CancellationToken>()).Returns((InstallationAccessToken?)null);

        ((TestHttpMessageHandler)_handler).ConfigureResponse(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json", Encoding.UTF8, "application/json")
        });

        var result = await _sut.CreateInstallationTokenAsync(installationId);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateInstallationTokenAsync_WhenCachedTokenExists_ShouldReturnCachedToken()
    {
        var installationId = InstallationId.Create(123);
        var cachedToken = InstallationAccessToken.Create("cached-token", DateTimeOffset.UtcNow.AddHours(1));
        _cache.GetAsync(installationId, Arg.Any<CancellationToken>()).Returns(cachedToken);

        var result = await _sut.CreateInstallationTokenAsync(installationId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(cachedToken);
        // Should not make HTTP request when cached token exists
    }

    [Fact]
    public async Task CreateInstallationTokenAsync_WhenHttpRequestThrows_ShouldReturnFailure()
    {
        var installationId = InstallationId.Create(123);
        _cache.GetAsync(installationId, Arg.Any<CancellationToken>()).Returns((InstallationAccessToken?)null);

        ((TestHttpMessageHandler)_handler).ConfigureResponse(_ => throw new HttpRequestException("Network error"));

        var result = await _sut.CreateInstallationTokenAsync(installationId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private Func<HttpRequestMessage, HttpResponseMessage>? _responseFactory;

        public void ConfigureResponse(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_responseFactory is null)
            {
                throw new InvalidOperationException("Response factory not configured.");
            }

            return Task.FromResult(_responseFactory(request));
        }
    }
}
