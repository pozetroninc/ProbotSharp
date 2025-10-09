// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using ProbotSharp.IntegrationTests.Infrastructure;

namespace ProbotSharp.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for root endpoint.
/// Tests application metadata and version information.
/// </summary>
public class RootEndpointTests : IClassFixture<ProbotSharpTestFactory>
{
    private readonly ProbotSharpTestFactory _factory;
    private readonly HttpClient _client;

    public RootEndpointTests(ProbotSharpTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetRoot_ShouldReturnApplicationMetadata()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        payload.Should().NotBeNull();
        payload!.Should().ContainKey("application");
        payload["application"].Should().Be("ProbotSharp");
    }

    [Fact]
    public async Task GetRoot_ShouldReturnVersion()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        payload.Should().NotBeNull();
        payload!.Should().ContainKey("version");
        payload["version"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetRoot_ShouldReturnJsonContentType()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }
}
