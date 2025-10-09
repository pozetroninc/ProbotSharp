// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Services;

namespace ProbotSharp.Application.Tests.Services;

public class HttpRouteConfiguratorTests
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IEndpointRouteBuilder _endpoints = Substitute.For<IEndpointRouteBuilder>();
    private readonly ILogger<HttpRouteConfigurator> _logger = Substitute.For<ILogger<HttpRouteConfigurator>>();
    private readonly List<IProbotApp> _apps = new();

    private HttpRouteConfigurator CreateSut()
    {
        return new HttpRouteConfigurator(_apps, _serviceProvider, _logger);
    }

    [Fact]
    public async Task ConfigureRoutesAsync_WithNoApps_ShouldNotThrow()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = async () => await sut.ConfigureRoutesAsync(_endpoints);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ConfigureRoutesAsync_WithSingleApp_ShouldInvokeConfigureRoutesAsync()
    {
        // Arrange
        var app = Substitute.For<IProbotApp>();
        _apps.Add(app);
        var sut = CreateSut();

        // Act
        await sut.ConfigureRoutesAsync(_endpoints);

        // Assert
        await app.Received(1).ConfigureRoutesAsync(_endpoints, _serviceProvider);
    }

    [Fact]
    public async Task ConfigureRoutesAsync_WithMultipleApps_ShouldInvokeAllApps()
    {
        // Arrange
        var app1 = Substitute.For<IProbotApp>();
        var app2 = Substitute.For<IProbotApp>();
        var app3 = Substitute.For<IProbotApp>();
        _apps.AddRange(new[] { app1, app2, app3 });
        var sut = CreateSut();

        // Act
        await sut.ConfigureRoutesAsync(_endpoints);

        // Assert
        await app1.Received(1).ConfigureRoutesAsync(_endpoints, _serviceProvider);
        await app2.Received(1).ConfigureRoutesAsync(_endpoints, _serviceProvider);
        await app3.Received(1).ConfigureRoutesAsync(_endpoints, _serviceProvider);
    }

    [Fact]
    public async Task ConfigureRoutesAsync_ShouldCallAppsInOrder()
    {
        // Arrange
        var callOrder = new List<string>();
        var app1 = Substitute.For<IProbotApp>();
        var app2 = Substitute.For<IProbotApp>();

        app1.ConfigureRoutesAsync(Arg.Any<IEndpointRouteBuilder>(), Arg.Any<IServiceProvider>())
            .Returns(callInfo =>
            {
                callOrder.Add("app1");
                return Task.CompletedTask;
            });

        app2.ConfigureRoutesAsync(Arg.Any<IEndpointRouteBuilder>(), Arg.Any<IServiceProvider>())
            .Returns(callInfo =>
            {
                callOrder.Add("app2");
                return Task.CompletedTask;
            });

        _apps.AddRange(new[] { app1, app2 });
        var sut = CreateSut();

        // Act
        await sut.ConfigureRoutesAsync(_endpoints);

        // Assert
        callOrder.Should().ContainInOrder("app1", "app2");
    }

    [Fact]
    public async Task ConfigureRoutesAsync_WhenAppThrows_ShouldPropagateException()
    {
        // Arrange
        var app1 = Substitute.For<IProbotApp>();
        var app2 = Substitute.For<IProbotApp>();
        var app3 = Substitute.For<IProbotApp>();

        app1.Name.Returns("TestApp1");
        app2.Name.Returns("TestApp2");
        app3.Name.Returns("TestApp3");

        app2.ConfigureRoutesAsync(Arg.Any<IEndpointRouteBuilder>(), Arg.Any<IServiceProvider>())
            .Returns(Task.FromException(new InvalidOperationException("App2 failed")));

        _apps.AddRange(new[] { app1, app2, app3 });
        var sut = CreateSut();

        // Act
        var act = async () => await sut.ConfigureRoutesAsync(_endpoints);

        // Assert - should propagate the exception wrapped
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Contain("TestApp2");
        exception.Which.InnerException.Should().NotBeNull();
        exception.Which.InnerException!.Message.Should().Be("App2 failed");

        // Verify app1 was called, but app3 was not (due to exception in app2)
        await app1.Received(1).ConfigureRoutesAsync(_endpoints, _serviceProvider);
        await app3.Received(0).ConfigureRoutesAsync(_endpoints, _serviceProvider);
    }

    [Fact]
    public async Task ConfigureRoutesAsync_WithRealApp_ShouldBeAbleToRegisterRoutes()
    {
        // Arrange
        var app = new TestProbotApp();
        _apps.Add(app);

        // Use a real WebApplication builder to test actual route registration
        var builder = WebApplication.CreateBuilder();
        var webApp = builder.Build();

        var logger = Substitute.For<ILogger<HttpRouteConfigurator>>();
        var sut = new HttpRouteConfigurator(_apps, webApp.Services, logger);

        // Act
        await sut.ConfigureRoutesAsync(webApp);

        // Assert
        app.ConfigureRoutesWasCalled.Should().BeTrue();
    }

    private class TestProbotApp : IProbotApp
    {
        public string Name => "test-app";
        public string Version => "1.0.0";
        public bool ConfigureRoutesWasCalled { get; private set; }

        public Task ConfigureAsync(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            return Task.CompletedTask;
        }

        public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }

        public Task ConfigureRoutesAsync(IEndpointRouteBuilder endpoints, IServiceProvider serviceProvider)
        {
            ConfigureRoutesWasCalled = true;

            // Register a simple test endpoint
            endpoints.MapGet("/test-app/ping", () => Results.Ok(new { status = "ok" }));

            return Task.CompletedTask;
        }
    }
}
