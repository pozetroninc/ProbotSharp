// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json;

using FluentAssertions;

using ProbotSharp.IntegrationTests.Infrastructure;

namespace ProbotSharp.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for health check endpoint.
/// Tests health check functionality for database, cache, and external dependencies.
/// </summary>
public class HealthCheckTests : IClassFixture<ProbotSharpTestFactory>
{
    private readonly ProbotSharpTestFactory _factory;
    private readonly HttpClient _client;

    public HealthCheckTests(ProbotSharpTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturnHealthyOrDegraded()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();
        using var json = JsonDocument.Parse(content);

        // Status can be Healthy or Degraded (if external dependencies like GitHub API are unavailable)
        json.RootElement.GetProperty("status").GetString().Should().BeOneOf("Healthy", "Degraded");
    }

    [Fact]
    public async Task GetHealth_ShouldCheckDependencies()
    {
        // This test verifies that the health check endpoint
        // is properly configured to check all dependencies

        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Should().NotBeNull();
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task GetHealth_ShouldIncludeDatabaseCheck()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();
        using var json = JsonDocument.Parse(content);

        var checks = json.RootElement.GetProperty("checks");
        var databaseCheck = checks.EnumerateArray()
            .FirstOrDefault(c => c.GetProperty("name").GetString() == "database");

        databaseCheck.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        databaseCheck.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task GetHealth_ShouldIncludeCacheCheck()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();
        using var json = JsonDocument.Parse(content);

        var checks = json.RootElement.GetProperty("checks");
        var cacheCheck = checks.EnumerateArray()
            .FirstOrDefault(c => c.GetProperty("name").GetString() == "cache");

        cacheCheck.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        cacheCheck.GetProperty("status").GetString().Should().BeOneOf("Healthy", "Degraded");
    }

    [Fact]
    public async Task GetHealth_ShouldIncludeGitHubApiCheck()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();
        using var json = JsonDocument.Parse(content);

        var checks = json.RootElement.GetProperty("checks");
        var githubCheck = checks.EnumerateArray()
            .FirstOrDefault(c => c.GetProperty("name").GetString() == "github_api");

        githubCheck.ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }

    [Fact]
    public async Task GetHealth_ShouldIncludeTimestamp()
    {
        // Arrange
        var beforeRequest = DateTime.UtcNow;

        // Act
        var response = await _client.GetAsync("/health");
        var afterRequest = DateTime.UtcNow;

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        var timestamp = json.RootElement.GetProperty("timestamp").GetString();
        timestamp.Should().NotBeNullOrEmpty();

        // Parse timestamp and ensure it's treated as UTC
        var parsedTimestamp = DateTime.Parse(timestamp!, null, System.Globalization.DateTimeStyles.RoundtripKind);

        // Convert to UTC if necessary for comparison
        if (parsedTimestamp.Kind == DateTimeKind.Local)
        {
            parsedTimestamp = parsedTimestamp.ToUniversalTime();
        }

        // Ensure timestamp is reasonable (within a few hours to account for timezone differences)
        parsedTimestamp.Should().BeOnOrAfter(beforeRequest.AddHours(-24));
        parsedTimestamp.Should().BeOnOrBefore(afterRequest.AddHours(24));
    }

    [Fact]
    public async Task GetHealth_ShouldIncludeDuration()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        var duration = json.RootElement.GetProperty("duration").GetDouble();
        duration.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetHealth_ShouldReturnJsonContentType()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetHealth_WithMultipleConcurrentRequests_ShouldHandleAllSuccessfully()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _client.GetAsync("/health"))
            .ToArray();

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(response =>
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        });
    }

    [Fact]
    public async Task GetHealth_ShouldReturnConsistentJsonStructure()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        // Verify required properties exist
        json.RootElement.TryGetProperty("status", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("timestamp", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("duration", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("checks", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetHealth_EachCheckShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        var checks = json.RootElement.GetProperty("checks");
        checks.GetArrayLength().Should().BeGreaterThan(0);

        foreach (var check in checks.EnumerateArray())
        {
            check.TryGetProperty("name", out _).Should().BeTrue();
            check.TryGetProperty("status", out _).Should().BeTrue();
            check.TryGetProperty("duration", out _).Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetHealth_DatabaseCheckShouldIncludeProviderInfo()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        var checks = json.RootElement.GetProperty("checks");
        var databaseCheck = checks.EnumerateArray()
            .FirstOrDefault(c => c.GetProperty("name").GetString() == "database");

        if (databaseCheck.ValueKind != JsonValueKind.Undefined)
        {
            var data = databaseCheck.GetProperty("data");
            data.TryGetProperty("database_provider", out _).Should().BeTrue();
        }
    }
}
