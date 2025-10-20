// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using ProbotSharp.Infrastructure.Adapters.GitHub;

using RichardSzalay.MockHttp;

namespace ProbotSharp.Infrastructure.Tests.Adapters.GitHub;

public class GitHubRestHttpAdapterTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttp = new();
    private readonly ILogger<GitHubRestHttpAdapter> _logger = Substitute.For<ILogger<GitHubRestHttpAdapter>>();
    private bool _disposed;

    [Fact]
    public async Task SendAsync_ShouldReturnSuccessResult_WhenRequestSucceeds()
    {
        var clientFactory = CreateFactory();
        _mockHttp.When("*").Respond(HttpStatusCode.OK);
        var adapter = new GitHubRestHttpAdapter(clientFactory, _logger);

        var result = await adapter.SendAsync(client => client.GetAsync("https://api.github.com/"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SendAsync_ShouldRetryOnTransientErrors()
    {
        var clientFactory = CreateFactory();
        var callCount = 0;
        _mockHttp.When("*").Respond(_ =>
        {
            callCount++;
            return callCount < 3
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : new HttpResponseMessage(HttpStatusCode.OK);
        });

        var adapter = new GitHubRestHttpAdapter(clientFactory, _logger);

        var result = await adapter.SendAsync(client => client.GetAsync("https://api.github.com/"));

        result.IsSuccess.Should().BeTrue();
        callCount.Should().Be(3);
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
        factory.CreateClient("GitHubRest").Returns(client);
        return factory;
    }
}
