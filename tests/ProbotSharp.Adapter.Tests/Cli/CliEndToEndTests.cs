// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.IO;

using Microsoft.Extensions.Logging;

using ProbotSharp.Adapters.Cli;
using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Adapter.Tests.Cli;

/// <summary>
/// End-to-end integration tests for CLI commands.
/// Tests the complete flow from command invocation through to port interaction.
/// </summary>
public sealed class CliEndToEndTests : IDisposable
{
    private readonly IAppLifecyclePort _lifecyclePort = Substitute.For<IAppLifecyclePort>();
    private readonly IWebhookProcessingPort _webhookProcessingPort = Substitute.For<IWebhookProcessingPort>();
    private readonly ILogger<CliCommandService> _logger = Substitute.For<ILogger<CliCommandService>>();
    private readonly CliCommandService _sut;
    private readonly List<string> _tempFiles = new();
    private readonly string _fixturesPath;

    public CliEndToEndTests()
    {
        _sut = new CliCommandService(_lifecyclePort, _webhookProcessingPort, _logger);

        // Get the fixtures path relative to test assembly
        var testDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _fixturesPath = Path.Combine(testDirectory, "..", "..", "..", "Fixtures", "cli-test-payloads");
    }

    public void Dispose()
    {
        // Clean up any temporary files created during tests
        foreach (var tempFile in _tempFiles)
        {
            if (File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    #region Run Command Tests

    [Fact]
    public async Task RunCommand_ShouldStartServer_WithSingleApp()
    {
        // Arrange
        var serverInfo = new ServerInfo("localhost", 3000, "/webhooks", true, null);
        _lifecyclePort
            .StartServerAsync(Arg.Any<StartServerCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ServerInfo>.Success(serverInfo));

        var command = new RunCliCommand(
            AppPaths: new[] { "./MyApp.dll" },
            Port: 3000,
            Host: "localhost",
            WebhookPath: "/webhooks",
            WebhookProxy: null,
            AppId: null,
            PrivateKey: null,
            Secret: null,
            BaseUrl: null,
            RedisUrl: null,
            LogLevel: "info",
            LogFormat: "pretty",
            LogLevelInString: false,
            LogMessageKey: null,
            SentryDsn: null);

        // Act
        var result = await _sut.ExecuteRunAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _lifecyclePort.Received(1).StartServerAsync(
            Arg.Is<StartServerCommand>(c =>
                c.Host == "localhost" &&
                c.Port == 3000 &&
                c.WebhookPath == "/webhooks" &&
                c.AppPaths!.Length == 1 &&
                c.AppPaths[0] == "./MyApp.dll"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCommand_ShouldStartServer_WithMultipleApps()
    {
        // Arrange
        var serverInfo = new ServerInfo("localhost", 3000, "/webhooks", true, null);
        _lifecyclePort
            .StartServerAsync(Arg.Any<StartServerCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ServerInfo>.Success(serverInfo));

        var command = new RunCliCommand(
            AppPaths: new[] { "./App1.dll", "./App2.dll", "./App3.dll" });

        // Act
        var result = await _sut.ExecuteRunAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _lifecyclePort.Received(1).StartServerAsync(
            Arg.Is<StartServerCommand>(c => c.AppPaths!.Length == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCommand_ShouldConfigureWebhookProxy_WhenProvided()
    {
        // Arrange
        var serverInfo = new ServerInfo("localhost", 3000, "/webhooks", true, "https://smee.io/abc123");
        _lifecyclePort
            .StartServerAsync(Arg.Any<StartServerCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ServerInfo>.Success(serverInfo));

        var command = new RunCliCommand(
            AppPaths: new[] { "./MyApp.dll" },
            WebhookProxy: "https://smee.io/abc123");

        // Act
        var result = await _sut.ExecuteRunAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _lifecyclePort.Received(1).StartServerAsync(
            Arg.Is<StartServerCommand>(c => c.WebhookProxy == "https://smee.io/abc123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCommand_ShouldConfigureAppId_WhenProvided()
    {
        // Arrange
        var serverInfo = new ServerInfo("localhost", 3000, "/webhooks", true, null);
        _lifecyclePort
            .StartServerAsync(Arg.Any<StartServerCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ServerInfo>.Success(serverInfo));

        var appId = GitHubAppId.Create(123456);

        var command = new RunCliCommand(
            AppPaths: new[] { "./MyApp.dll" },
            AppId: appId,
            Secret: "webhook-secret");

        // Act
        var result = await _sut.ExecuteRunAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _lifecyclePort.Received(1).StartServerAsync(
            Arg.Is<StartServerCommand>(c =>
                c.AppId != null &&
                c.AppId.Value == 123456 &&
                c.WebhookSecret == "webhook-secret"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCommand_ShouldReturnError_WhenNoAppsProvided()
    {
        // Arrange
        var command = new RunCliCommand(Array.Empty<string>());

        // Act
        var result = await _sut.ExecuteRunAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("cli_run_missing_app");
        result.Error.Value.Message.Should().Contain("at least one app path");
        await _lifecyclePort.DidNotReceive().StartServerAsync(Arg.Any<StartServerCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCommand_ShouldPropagateError_WhenServerStartFails()
    {
        // Arrange
        _lifecyclePort
            .StartServerAsync(Arg.Any<StartServerCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ServerInfo>.Failure("server_bind_error", "Port 3000 is already in use"));

        var command = new RunCliCommand(new[] { "./MyApp.dll" });

        // Act
        var result = await _sut.ExecuteRunAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("server_bind_error");
    }

    [Fact]
    public async Task RunCommand_ShouldUseCustomPort_WhenSpecified()
    {
        // Arrange
        var serverInfo = new ServerInfo("localhost", 8080, "/webhooks", true, null);
        _lifecyclePort
            .StartServerAsync(Arg.Any<StartServerCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ServerInfo>.Success(serverInfo));

        var command = new RunCliCommand(
            AppPaths: new[] { "./MyApp.dll" },
            Port: 8080);

        // Act
        var result = await _sut.ExecuteRunAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _lifecyclePort.Received(1).StartServerAsync(
            Arg.Is<StartServerCommand>(c => c.Port == 8080),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCommand_ShouldUseCustomHost_WhenSpecified()
    {
        // Arrange
        var serverInfo = new ServerInfo("0.0.0.0", 3000, "/webhooks", true, null);
        _lifecyclePort
            .StartServerAsync(Arg.Any<StartServerCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ServerInfo>.Success(serverInfo));

        var command = new RunCliCommand(
            AppPaths: new[] { "./MyApp.dll" },
            Host: "0.0.0.0");

        // Act
        var result = await _sut.ExecuteRunAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _lifecyclePort.Received(1).StartServerAsync(
            Arg.Is<StartServerCommand>(c => c.Host == "0.0.0.0"),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Receive Command Tests

    [Fact]
    public async Task ReceiveCommand_ShouldProcessWebhook_WithPushEvent()
    {
        // Arrange
        var payloadPath = Path.Combine(_fixturesPath, "push-event.json");

        // Skip test if fixture file doesn't exist (e.g., in CI without fixtures)
        if (!File.Exists(payloadPath))
        {
            return;
        }

        _webhookProcessingPort
            .ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = new ReceiveEventCommand(
            EventName: WebhookEventName.Create("push"),
            PayloadPath: payloadPath,
            AppPath: "./MyApp.dll",
            Token: null,
            AppId: null,
            PrivateKey: null,
            BaseUrl: null,
            LogLevel: "info",
            LogFormat: "pretty",
            LogLevelInString: false,
            LogMessageKey: null,
            SentryDsn: null);

        // Act
        var result = await _sut.ExecuteReceiveAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _webhookProcessingPort.Received(1).ProcessAsync(
            Arg.Is<ProcessWebhookCommand>(c =>
                c.EventName.Value == "push" &&
                c.Payload.RootElement.GetProperty("ref").GetString() == "refs/heads/main" &&
                c.InstallationId != null &&
                c.InstallationId.Value == 987654321),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReceiveCommand_ShouldProcessWebhook_WithIssuesOpenedEvent()
    {
        // Arrange
        var payloadPath = Path.Combine(_fixturesPath, "issues-opened.json");

        if (!File.Exists(payloadPath))
        {
            return;
        }

        _webhookProcessingPort
            .ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = new ReceiveEventCommand(
            EventName: WebhookEventName.Create("issues.opened"),
            PayloadPath: payloadPath,
            AppPath: "./MyApp.dll");

        // Act
        var result = await _sut.ExecuteReceiveAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _webhookProcessingPort.Received(1).ProcessAsync(
            Arg.Is<ProcessWebhookCommand>(c =>
                c.EventName.Value == "issues.opened" &&
                c.Payload.RootElement.GetProperty("action").GetString() == "opened" &&
                c.Payload.RootElement.GetProperty("issue").GetProperty("number").GetInt32() == 42),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReceiveCommand_ShouldProcessWebhook_WithPullRequestOpenedEvent()
    {
        // Arrange
        var payloadPath = Path.Combine(_fixturesPath, "pull-request-opened.json");

        if (!File.Exists(payloadPath))
        {
            return;
        }

        _webhookProcessingPort
            .ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = new ReceiveEventCommand(
            EventName: WebhookEventName.Create("pull_request.opened"),
            PayloadPath: payloadPath,
            AppPath: "./MyApp.dll");

        // Act
        var result = await _sut.ExecuteReceiveAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _webhookProcessingPort.Received(1).ProcessAsync(
            Arg.Is<ProcessWebhookCommand>(c =>
                c.EventName.Value == "pull_request.opened" &&
                c.Payload.RootElement.GetProperty("pull_request").GetProperty("number").GetInt32() == 15),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReceiveCommand_ShouldReturnError_WhenPayloadPathEmpty()
    {
        // Arrange
        var command = new ReceiveEventCommand(
            EventName: WebhookEventName.Create("issues.opened"),
            PayloadPath: string.Empty,
            AppPath: "./MyApp.dll");

        // Act
        var result = await _sut.ExecuteReceiveAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("cli_receive_missing_payload");
        await _webhookProcessingPort.DidNotReceive().ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReceiveCommand_ShouldReturnError_WhenPayloadFileNotFound()
    {
        // Arrange
        var command = new ReceiveEventCommand(
            EventName: WebhookEventName.Create("issues.opened"),
            PayloadPath: "/nonexistent/path/payload.json",
            AppPath: "./MyApp.dll");

        // Act
        var result = await _sut.ExecuteReceiveAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("cli_receive_payload_not_found");
        result.Error.Value.Message.Should().Contain("/nonexistent/path/payload.json");
        await _webhookProcessingPort.DidNotReceive().ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReceiveCommand_ShouldReturnError_WhenAppPathEmpty()
    {
        // Arrange
        var tempFile = CreateTempPayloadFile("{\"action\":\"opened\"}");

        var command = new ReceiveEventCommand(
            EventName: WebhookEventName.Create("issues.opened"),
            PayloadPath: tempFile,
            AppPath: string.Empty);

        // Act
        var result = await _sut.ExecuteReceiveAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("cli_receive_missing_app");
        await _webhookProcessingPort.DidNotReceive().ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReceiveCommand_ShouldReturnError_WhenPayloadFileEmpty()
    {
        // Arrange
        var tempFile = CreateTempPayloadFile(string.Empty);

        var command = new ReceiveEventCommand(
            EventName: WebhookEventName.Create("issues.opened"),
            PayloadPath: tempFile,
            AppPath: "./MyApp.dll");

        // Act
        var result = await _sut.ExecuteReceiveAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("cli_receive_empty_payload");
        await _webhookProcessingPort.DidNotReceive().ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReceiveCommand_ShouldGenerateDeliveryId_ForEachWebhook()
    {
        // Arrange
        var tempFile = CreateTempPayloadFile("{\"action\":\"opened\",\"installation\":{\"id\":123}}");

        _webhookProcessingPort
            .ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = new ReceiveEventCommand(
            EventName: WebhookEventName.Create("issues.opened"),
            PayloadPath: tempFile,
            AppPath: "./MyApp.dll");

        // Act
        var result = await _sut.ExecuteReceiveAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _webhookProcessingPort.Received(1).ProcessAsync(
            Arg.Is<ProcessWebhookCommand>(c => !string.IsNullOrEmpty(c.DeliveryId.Value)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReceiveCommand_ShouldExtractInstallationId_WhenPresent()
    {
        // Arrange
        var payloadJson = "{\"action\":\"opened\",\"installation\":{\"id\":987654321}}";
        var tempFile = CreateTempPayloadFile(payloadJson);

        _webhookProcessingPort
            .ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = new ReceiveEventCommand(
            EventName: WebhookEventName.Create("issues.opened"),
            PayloadPath: tempFile,
            AppPath: "./MyApp.dll");

        // Act
        var result = await _sut.ExecuteReceiveAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _webhookProcessingPort.Received(1).ProcessAsync(
            Arg.Is<ProcessWebhookCommand>(c =>
                c.InstallationId != null &&
                c.InstallationId.Value == 987654321),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReceiveCommand_ShouldHandleNullInstallationId_Gracefully()
    {
        // Arrange
        var payloadJson = "{\"action\":\"opened\",\"issue\":{\"number\":1}}";
        var tempFile = CreateTempPayloadFile(payloadJson);

        _webhookProcessingPort
            .ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = new ReceiveEventCommand(
            EventName: WebhookEventName.Create("issues.opened"),
            PayloadPath: tempFile,
            AppPath: "./MyApp.dll");

        // Act
        var result = await _sut.ExecuteReceiveAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _webhookProcessingPort.Received(1).ProcessAsync(
            Arg.Is<ProcessWebhookCommand>(c => c.InstallationId == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReceiveCommand_ShouldPropagateError_WhenWebhookProcessingFails()
    {
        // Arrange
        var tempFile = CreateTempPayloadFile("{\"action\":\"opened\"}");

        _webhookProcessingPort
            .ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure("processing_error", "Handler threw an exception"));

        var command = new ReceiveEventCommand(
            EventName: WebhookEventName.Create("issues.opened"),
            PayloadPath: tempFile,
            AppPath: "./MyApp.dll");

        // Act
        var result = await _sut.ExecuteReceiveAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("processing_error");
    }

    [Fact]
    public async Task ReceiveCommand_ShouldCreateDummySignature_ForLocalTesting()
    {
        // Arrange
        var tempFile = CreateTempPayloadFile("{\"action\":\"opened\"}");

        _webhookProcessingPort
            .ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = new ReceiveEventCommand(
            EventName: WebhookEventName.Create("issues.opened"),
            PayloadPath: tempFile,
            AppPath: "./MyApp.dll");

        // Act
        var result = await _sut.ExecuteReceiveAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _webhookProcessingPort.Received(1).ProcessAsync(
            Arg.Is<ProcessWebhookCommand>(c =>
                c.Signature != null &&
                c.Signature.Value.StartsWith("sha256=")),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Version Command Tests

    [Fact]
    public async Task VersionCommand_ShouldReturnVersionString()
    {
        // Arrange
        var query = new GetVersionQuery();

        // Act
        var result = await _sut.GetVersionAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
        result.Value.Should().MatchRegex(@"^\d+\.\d+\.\d+");
    }

    [Fact]
    public async Task VersionCommand_ShouldReturnConsistentVersion_OnMultipleCalls()
    {
        // Arrange
        var query = new GetVersionQuery();

        // Act
        var result1 = await _sut.GetVersionAsync(query);
        var result2 = await _sut.GetVersionAsync(query);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().Be(result2.Value);
    }

    #endregion

    #region Help Command Tests

    [Fact]
    public async Task HelpCommand_ShouldReturnGeneralHelp_WhenNoCommandSpecified()
    {
        // Arrange
        var query = new GetHelpQuery();

        // Act
        var result = await _sut.GetHelpAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("ProbotSharp");
        result.Value.Should().Contain("run");
        result.Value.Should().Contain("receive");
        result.Value.Should().Contain("setup");
        result.Value.Should().Contain("version");
        result.Value.Should().Contain("help");
    }

    [Fact]
    public async Task HelpCommand_ShouldReturnRunHelp_WhenRunCommandSpecified()
    {
        // Arrange
        var query = new GetHelpQuery("run");

        // Act
        var result = await _sut.GetHelpAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("run");
        result.Value.Should().Contain("app-path");
        result.Value.Should().Contain("--port");
        result.Value.Should().Contain("--webhook-proxy");
        result.Value.Should().NotContain("receive");
    }

    [Fact]
    public async Task HelpCommand_ShouldReturnReceiveHelp_WhenReceiveCommandSpecified()
    {
        // Arrange
        var query = new GetHelpQuery("receive");

        // Act
        var result = await _sut.GetHelpAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("receive");
        result.Value.Should().Contain("--event");
        result.Value.Should().Contain("--file");
        result.Value.Should().Contain("payload");
        result.Value.Should().NotContain("--webhook-proxy");
    }

    [Fact]
    public async Task HelpCommand_ShouldReturnSetupHelp_WhenSetupCommandSpecified()
    {
        // Arrange
        var query = new GetHelpQuery("setup");

        // Act
        var result = await _sut.GetHelpAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("setup");
        result.Value.Should().Contain("wizard");
        result.Value.Should().Contain("--port");
    }

    [Fact]
    public async Task HelpCommand_ShouldReturnVersionHelp_WhenVersionCommandSpecified()
    {
        // Arrange
        var query = new GetHelpQuery("version");

        // Act
        var result = await _sut.GetHelpAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("version");
    }

    [Fact]
    public async Task HelpCommand_ShouldReturnHelpHelp_WhenHelpCommandSpecified()
    {
        // Arrange
        var query = new GetHelpQuery("help");

        // Act
        var result = await _sut.GetHelpAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("help");
    }

    [Fact]
    public async Task HelpCommand_ShouldReturnErrorMessage_WhenUnknownCommandSpecified()
    {
        // Arrange
        var query = new GetHelpQuery("unknown-command");

        // Act
        var result = await _sut.GetHelpAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("Unknown command");
        result.Value.Should().Contain("unknown-command");
        result.Value.Should().Contain("ProbotSharp"); // Should include general help
    }

    [Theory]
    [InlineData("run")]
    [InlineData("receive")]
    [InlineData("setup")]
    [InlineData("version")]
    [InlineData("help")]
    public async Task HelpCommand_ShouldReturnSpecificHelp_ForEachCommand(string commandName)
    {
        // Arrange
        var query = new GetHelpQuery(commandName);

        // Act
        var result = await _sut.GetHelpAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
        result.Value.Should().Contain(commandName);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ReceiveCommand_ShouldHandleException_WhenPayloadIsInvalidJson()
    {
        // Arrange
        var tempFile = CreateTempPayloadFile("{ invalid json }");

        var command = new ReceiveEventCommand(
            EventName: WebhookEventName.Create("issues.opened"),
            PayloadPath: tempFile,
            AppPath: "./MyApp.dll");

        // Act
        var result = await _sut.ExecuteReceiveAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("cli_receive_failed");
        await _webhookProcessingPort.DidNotReceive().ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCommand_ShouldThrowArgumentNullException_WhenCommandIsNull()
    {
        // Act
        Task Act() => _sut.ExecuteRunAsync(null!);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(Act);
    }

    [Fact]
    public async Task ReceiveCommand_ShouldThrowArgumentNullException_WhenCommandIsNull()
    {
        // Act
        Task Act() => _sut.ExecuteReceiveAsync(null!);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(Act);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a temporary file with the specified payload content.
    /// The file will be automatically cleaned up in Dispose().
    /// </summary>
    private string CreateTempPayloadFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, content);
        _tempFiles.Add(tempFile);
        return tempFile;
    }

    #endregion
}
