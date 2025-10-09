// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Bootstrap.Api.Middleware;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Bootstrap.Api.Tests.Middleware;

public sealed class SetupRedirectMiddlewareTests : IDisposable
{
    private readonly RequestDelegate _next = Substitute.For<RequestDelegate>();
    private readonly ILogger<SetupRedirectMiddleware> _logger = Substitute.For<ILogger<SetupRedirectMiddleware>>();
    private readonly IManifestPersistencePort _manifestPort = Substitute.For<IManifestPersistencePort>();
    private readonly DefaultHttpContext _httpContext = new();
    private readonly SetupRedirectMiddleware _middleware;
    private readonly string? _originalSkipSetup;

    public SetupRedirectMiddlewareTests()
    {
        _middleware = new SetupRedirectMiddleware(_next, _logger);
        _originalSkipSetup = Environment.GetEnvironmentVariable("PROBOTSHARP_SKIP_SETUP");
    }

    public void Dispose()
    {
        // Restore original environment variable
        if (_originalSkipSetup != null)
        {
            Environment.SetEnvironmentVariable("PROBOTSHARP_SKIP_SETUP", _originalSkipSetup);
        }
        else
        {
            Environment.SetEnvironmentVariable("PROBOTSHARP_SKIP_SETUP", null);
        }
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenPathIsSetup()
    {
        // Arrange
        _httpContext.Request.Path = "/setup";

        // Act
        await _middleware.InvokeAsync(_httpContext, _manifestPort);

        // Assert
        await _next.Received(1).Invoke(_httpContext);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenPathStartsWithSetup()
    {
        // Arrange
        _httpContext.Request.Path = "/setup/manifest";

        // Act
        await _middleware.InvokeAsync(_httpContext, _manifestPort);

        // Assert
        await _next.Received(1).Invoke(_httpContext);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenSkipSetupIsTrue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("PROBOTSHARP_SKIP_SETUP", "true");
        _httpContext.Request.Path = "/some-path";

        // Act
        await _middleware.InvokeAsync(_httpContext, _manifestPort);

        // Assert
        await _next.Received(1).Invoke(_httpContext);
        await _manifestPort.DidNotReceive().GetAsync();
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenSkipSetupIsTrueIgnoreCase()
    {
        // Arrange
        Environment.SetEnvironmentVariable("PROBOTSHARP_SKIP_SETUP", "TRUE");
        _httpContext.Request.Path = "/some-path";

        // Act
        await _middleware.InvokeAsync(_httpContext, _manifestPort);

        // Assert
        await _next.Received(1).Invoke(_httpContext);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenPathIsHealth()
    {
        // Arrange
        _httpContext.Request.Path = "/health";

        // Act
        await _middleware.InvokeAsync(_httpContext, _manifestPort);

        // Assert
        await _next.Received(1).Invoke(_httpContext);
        await _manifestPort.DidNotReceive().GetAsync();
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenPathIsWebhooks()
    {
        // Arrange
        _httpContext.Request.Path = "/webhooks";

        // Act
        await _middleware.InvokeAsync(_httpContext, _manifestPort);

        // Assert
        await _next.Received(1).Invoke(_httpContext);
        await _manifestPort.DidNotReceive().GetAsync();
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenPathIsRoot()
    {
        // Arrange
        _httpContext.Request.Path = "/";

        // Act
        await _middleware.InvokeAsync(_httpContext, _manifestPort);

        // Assert
        await _next.Received(1).Invoke(_httpContext);
        await _manifestPort.DidNotReceive().GetAsync();
    }

    [Fact]
    public async Task InvokeAsync_ShouldRedirect_WhenNotConfigured()
    {
        // Arrange
        Environment.SetEnvironmentVariable("PROBOTSHARP_SKIP_SETUP", null);
        _httpContext.Request.Path = "/some-path";
        _manifestPort.GetAsync().Returns(Result<string>.Success(string.Empty));

        // Act
        await _middleware.InvokeAsync(_httpContext, _manifestPort);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(302);
        _httpContext.Response.Headers.Location.ToString().Should().Be("/setup");
        await _next.DidNotReceive().Invoke(_httpContext);
    }

    [Fact]
    public async Task InvokeAsync_ShouldRedirect_WhenManifestResultFails()
    {
        // Arrange
        Environment.SetEnvironmentVariable("PROBOTSHARP_SKIP_SETUP", null);
        _httpContext.Request.Path = "/some-path";
        _manifestPort.GetAsync().Returns(Result<string>.Failure("error", "Manifest not found"));

        // Act
        await _middleware.InvokeAsync(_httpContext, _manifestPort);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(302);
        _httpContext.Response.Headers.Location.ToString().Should().Be("/setup");
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenConfigured()
    {
        // Arrange
        Environment.SetEnvironmentVariable("PROBOTSHARP_SKIP_SETUP", null);
        _httpContext.Request.Path = "/some-path";
        _manifestPort.GetAsync().Returns(Result<string>.Success("{\"app_id\":123}"));

        // Act
        await _middleware.InvokeAsync(_httpContext, _manifestPort);

        // Assert
        await _next.Received(1).Invoke(_httpContext);
        _httpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenExceptionOccurs()
    {
        // Arrange
        Environment.SetEnvironmentVariable("PROBOTSHARP_SKIP_SETUP", null);
        _httpContext.Request.Path = "/some-path";
        _manifestPort.GetAsync().Returns<Result<string>>(x => throw new InvalidOperationException("Test exception"));

        // Act
        await _middleware.InvokeAsync(_httpContext, _manifestPort);

        // Assert
        await _next.Received(1).Invoke(_httpContext);
    }
}
