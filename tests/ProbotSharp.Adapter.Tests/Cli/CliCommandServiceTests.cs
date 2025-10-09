// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.IO;

using Microsoft.Extensions.Logging;

using ProbotSharp.Adapters.Cli;
using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Adapter.Tests.Cli;

public sealed class CliCommandServiceTests
{
    private readonly IAppLifecyclePort _lifecyclePort = Substitute.For<IAppLifecyclePort>();
    private readonly IWebhookProcessingPort _webhookProcessingPort = Substitute.For<IWebhookProcessingPort>();
    private readonly ILogger<CliCommandService> _logger = Substitute.For<ILogger<CliCommandService>>();
    private readonly CliCommandService _sut;

    public CliCommandServiceTests()
    {
        _sut = new CliCommandService(_lifecyclePort, _webhookProcessingPort, _logger);
    }

    [Fact]
    public async Task ExecuteRunAsync_ShouldFail_WhenNoAppPaths()
    {
        var command = new RunCliCommand(Array.Empty<string>());

        var result = await _sut.ExecuteRunAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("cli_run_missing_app");
        await _lifecyclePort.DidNotReceive().StartServerAsync(Arg.Any<StartServerCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteRunAsync_ShouldDelegateToLifecyclePort_WhenAppsProvided()
    {
        var serverInfo = new ServerInfo("localhost", 3000, "/webhooks", true, null);
        _lifecyclePort
            .StartServerAsync(Arg.Any<StartServerCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ServerInfo>.Success(serverInfo));

        var command = new RunCliCommand(new[] { "app.js" }, Host: "localhost", Port: 3000);

        var result = await _sut.ExecuteRunAsync(command);

        result.IsSuccess.Should().BeTrue();
        await _lifecyclePort.Received(1).StartServerAsync(
            Arg.Is<StartServerCommand>(c => c.Host == "localhost" && c.Port == 3000 && c.AppPaths!.Length == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteRunAsync_ShouldReturnError_WhenLifecyclePortFails()
    {
        _lifecyclePort
            .StartServerAsync(Arg.Any<StartServerCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ServerInfo>.Failure("server_error", "Failed to start server"));

        var command = new RunCliCommand(new[] { "app.js" });

        var result = await _sut.ExecuteRunAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteReceiveAsync_ShouldFail_WhenPayloadPathMissing()
    {
        var command = new ReceiveEventCommand(
            WebhookEventName.Create("issues.opened"),
            string.Empty,
            "app.js");

        var result = await _sut.ExecuteReceiveAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("cli_receive_missing_payload");
    }

    [Fact]
    public async Task ExecuteReceiveAsync_ShouldFail_WhenPayloadFileNotFound()
    {
        var command = new ReceiveEventCommand(
            WebhookEventName.Create("issues.opened"),
            "/nonexistent/payload.json",
            "app.js");

        var result = await _sut.ExecuteReceiveAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("cli_receive_payload_not_found");
    }

    [Fact]
    public async Task ExecuteReceiveAsync_ShouldFail_WhenAppPathMissing()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "{}");

            var command = new ReceiveEventCommand(
                WebhookEventName.Create("issues.opened"),
                tempFile,
                string.Empty);

            var result = await _sut.ExecuteReceiveAsync(command);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error!.Value.Code.Should().Be("cli_receive_missing_app");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ExecuteReceiveAsync_ShouldSucceed_WhenValidPayloadAndApp()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "{\"action\":\"opened\",\"issue\":{}}");

            _webhookProcessingPort
                .ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success());

            var command = new ReceiveEventCommand(
                WebhookEventName.Create("issues.opened"),
                tempFile,
                "app.js");

            var result = await _sut.ExecuteReceiveAsync(command);

            result.IsSuccess.Should().BeTrue();
            await _webhookProcessingPort.Received(1).ProcessAsync(
                Arg.Is<ProcessWebhookCommand>(c => c.EventName.Value == "issues.opened"),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ExecuteReceiveAsync_ShouldReturnError_WhenWebhookProcessingFails()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "{\"action\":\"opened\",\"issue\":{}}");

            _webhookProcessingPort
                .ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>())
                .Returns(Result.Failure("webhook_error", "Failed to process webhook"));

            var command = new ReceiveEventCommand(
                WebhookEventName.Create("issues.opened"),
                tempFile,
                "app.js");

            var result = await _sut.ExecuteReceiveAsync(command);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            await _webhookProcessingPort.Received(1).ProcessAsync(
                Arg.Any<ProcessWebhookCommand>(),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task GetVersionAsync_ShouldReturnVersion()
    {
        var result = await _sut.GetVersionAsync(new GetVersionQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetHelpAsync_ShouldReturnGeneralHelp_WhenNoCommandSpecified()
    {
        var result = await _sut.GetHelpAsync(new GetHelpQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("ProbotSharp");
        result.Value.Should().Contain("run");
        result.Value.Should().Contain("receive");
        result.Value.Should().Contain("setup");
    }

    [Fact]
    public async Task GetHelpAsync_ShouldReturnCommandHelp_WhenCommandSpecified()
    {
        var result = await _sut.GetHelpAsync(new GetHelpQuery("run"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("run");
        result.Value.Should().Contain("app-path");
    }

    [Fact]
    public async Task GetHelpAsync_ShouldReturnHelpWithUnknownMessage_WhenUnknownCommand()
    {
        var result = await _sut.GetHelpAsync(new GetHelpQuery("invalid"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("Unknown command");
    }
}

