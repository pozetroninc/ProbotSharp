// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using NSubstitute;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Bootstrap.Api.Controllers;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Bootstrap.Api.Tests.Controllers;

public class SetupControllerTests : IDisposable
{
    private readonly ISetupWizardPort _setupWizard;
    private readonly IManifestPersistencePort _manifestPort;
    private readonly IEnvironmentConfigurationPort _envConfig;
    private readonly ILogger<SetupController> _logger;
    private readonly SetupController _controller;
    private bool _disposed;

    public SetupControllerTests()
    {
        _setupWizard = Substitute.For<ISetupWizardPort>();
        _manifestPort = Substitute.For<IManifestPersistencePort>();
        _envConfig = Substitute.For<IEnvironmentConfigurationPort>();
        _logger = Substitute.For<ILogger<SetupController>>();

        _controller = new SetupController(_setupWizard, _manifestPort, _envConfig, _logger);

        // Setup HttpContext
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.ControllerContext.HttpContext.Request.Scheme = "http";
        _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost:3000");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _controller?.Dispose();
            _disposed = true;
        }
    }

    [Fact]
    public async Task Index_ReturnsViewWithNotConfigured_WhenManifestDoesNotExist()
    {
        // Arrange
        _manifestPort.GetAsync(Arg.Any<CancellationToken>())
            .Returns(Result<string?>.Success(null));

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False((bool)(viewResult.ViewData["IsConfigured"] ?? true));
    }

    [Fact]
    public async Task Index_ReturnsViewWithConfigured_WhenManifestExists()
    {
        // Arrange
        _manifestPort.GetAsync(Arg.Any<CancellationToken>())
            .Returns(Result<string?>.Success("{\"name\":\"test\"}"));

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.True((bool)(viewResult.ViewData["IsConfigured"] ?? false));
    }

    [Fact]
    public void New_ReturnsViewWithDefaults()
    {
        // Act
        var result = _controller.New();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult.ViewData["AppName"]);
        Assert.NotNull(viewResult.ViewData["Description"]);
        Assert.NotNull(viewResult.ViewData["BaseUrl"]);
    }

    [Fact]
    public async Task CreateManifest_ReturnsViewWithError_WhenAppNameIsEmpty()
    {
        // Arrange
        var request = new ManifestRequest
        {
            AppName = string.Empty,
            Description = "Test description"
        };

        // Act
        var result = await _controller.CreateManifest(request);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("New", viewResult.ViewName);
        Assert.False(_controller.ModelState.IsValid);
    }

    [Fact]
    public async Task CreateManifest_ReturnsManifestView_WhenSuccessful()
    {
        // Arrange
        var request = new ManifestRequest
        {
            AppName = "Test App",
            Description = "Test description"
        };

        var manifestJson = "{\"name\":\"Test App\"}";
        var createAppUrl = new Uri("https://github.com/settings/apps/new");
        var response = new GetManifestResponse(manifestJson, createAppUrl);

        _setupWizard.GetManifestAsync(Arg.Any<GetManifestCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<GetManifestResponse>.Success(response));

        // Act
        var result = await _controller.CreateManifest(request);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Manifest", viewResult.ViewName);
        Assert.Equal(manifestJson, viewResult.ViewData["ManifestJson"]);
        Assert.Equal(createAppUrl.ToString(), viewResult.ViewData["CreateAppUrl"]);
    }

    [Fact]
    public async Task CreateManifest_ReturnsErrorView_WhenManifestGenerationFails()
    {
        // Arrange
        var request = new ManifestRequest
        {
            AppName = "Test App",
            Description = "Test description"
        };

        _setupWizard.GetManifestAsync(Arg.Any<GetManifestCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<GetManifestResponse>.Failure("error", "Failed to create manifest"));

        // Act
        var result = await _controller.CreateManifest(request);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("New", viewResult.ViewName);
        Assert.False(_controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Callback_ReturnsBadRequest_WhenCodeIsEmpty()
    {
        // Act
        var result = await _controller.Callback(string.Empty);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Callback_ReturnsCompleteView_WhenSuccessful()
    {
        // Arrange
        var code = "test_code";
        var appUrl = new Uri("https://github.com/apps/test-app");

        _setupWizard.CompleteSetupAsync(Arg.Any<CompleteSetupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success(appUrl.ToString()));

        // Act
        var result = await _controller.Callback(code);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Complete", viewResult.ViewName);
        Assert.Equal(appUrl.ToString(), viewResult.ViewData["AppUrl"]);
    }

    [Fact]
    public async Task Callback_ReturnsErrorView_WhenSetupFails()
    {
        // Arrange
        var code = "test_code";

        _setupWizard.CompleteSetupAsync(Arg.Any<CompleteSetupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Failure("error", "Setup failed"));

        // Act
        var result = await _controller.Callback(code);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Error", viewResult.ViewName);
        Assert.NotNull(viewResult.ViewData["Error"]);
    }

    [Fact]
    public void Complete_ReturnsView()
    {
        // Act
        var result = _controller.Complete();

        // Assert
        Assert.IsType<ViewResult>(result);
    }
}
