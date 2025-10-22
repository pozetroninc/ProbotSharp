// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Application.UseCases;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Application.Tests.UseCases;

/// <summary>
/// Tests for <see cref="RunAppLifecycleUseCase"/>.
/// Verifies server lifecycle management, app loading, and state transitions.
/// </summary>
public sealed class RunAppLifecycleUseCaseTests
{
    private const string ValidTestPrivateKey = @"-----BEGIN PRIVATE KEY-----
MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQC1m3nMS8xt/ERVSf1s/f7E7mvF
HOhBc7RvEompx23VSKVcgq8gjBmsyHZO4zCXyaRuUc8nqhv5v7OpnvwZbHu8v48EGyM28n1nfj7G
5/vcu/k5qESkT3JCEzX2lecAImSxVPmZNSH9KagHBhZz5aBCrDHQM1LQHZQzmErk6FdXJb3VTnNL
f1S2/7fESqJfVyIPBNZHVYiGm8q9YQA4QXXblwNMet4XCbilrDw7Ag/9WdLvC/hJOw+YlxZqES7U
u40bzFf7gSstj3yKiKtVFFYan9tQj9ly+vR/RpqPbVYUBqT9l6YdHfvLbwXRpDVCruzjRBMI7B3K
p68mGmQUkB/HAgMBAAECggEAS59UVofxtpLneYR37RzPK/4EDm59IFQn/GCBxpru13OKuD0K48iz
IEnRSgQ8xdgHipCybFffkl4LWESFwah01iIGUbVjMoxDQAdH6NfM/xufKi0xeCJE99K8E9RT/rBT
sawqy2tenebsuxAD5XrnAxxlGcdlYpgOkHWIKk29oOG4JtoeZ4sYDO1IsnO0Cdf6Yqazq5kVTAMm
0t5e721I1UWrCOyBz26oDYOURS0QF1A3yZOoj/xasVqhmtuDwvRKfHu0fnQAN6oL/nfZUOvquSgP
mFHvO7QZou5IVguVgDrB3LeV3u6WSeTAs61u5PHVF4JDfraFbMl2H/ZD5wyrUQKBgQDrhkLFiLyk
5l5HoRgkQpbL/ZZ0r8Kq04xUftjNJ1iabbyBrigSLRCiGONYlol7GfdZk0sEynvcmy2EoRenDHWE
ODugieoZhvA33LYDbQYX6Och6atyh1/E0PW7Y1j8QhNiMB4P7VSfTxdoRiVce65HKDkLWO+cdGKL
cXatgr8xawKBgQDFZUGzL55/73/byw1b7uXul8DCff6pMdPK6hzxLX//OIKRipc2cSnkNhw6yEYt
K+BCXtrD/pQsPQ68faytfQonRnSCSoFf2gt4AWgXxhX52t4SO9ubaxPnU7yDWb7gwRTOOVIp0M/S
kNa3+cr05bB/hQ29s66k1xPwOse8hf22FQKBgQDjjbF/lo/kpmqavCtOBUQazOSL0rC7SW8AyBmB
hS1W7wU9Kd2vSfkTFAa7tZ4Y4MqZsfS+KUAosYj15oqqrB/yYj5B6l3S0gvPfSAiCTjk6vI9Ur2C
Bwdhah6xMNhtyQ1fRWwWKVAkJ09PQM6iHyEPwO30Z7YBrHT1kud91qARBwKBgECmIRZmiiqsbdu2
bPGnFHdIDEGmsjkHsK3Mbx4uILWd9GVmVo/mECpF7ojx/sncjN4v4sY+Ipk6hhEFuAA565Fhadci
P6z85LSxAT6ICbqDDCg8ongMYcBplFwQkItbsUy5SUeYs/fqp+gpT2dVsrCRCycOKiyipE0Rc0kz
ORzhAoGBAMtLvQguQ7IxajBcOUEn/YwUbt3gZXHCAKUgeM/5Qn9jMGAC/ALE7l2MvjI+6D45oVbU
hmBrHpWGa/MiFVeKGed/AbLVKyt/FEYe0kWSzz9H2L3p9LNFRHiPv+4g6ZS/5+CHRDl3qlMzxefk
TCrIbpdZCDBI9Gq7rF1uZL5Raik6
-----END PRIVATE KEY-----";

    private readonly ILoggingPort _logger = Substitute.For<ILoggingPort>();

    private RunAppLifecycleUseCase CreateSut() => new(_logger);

    [Fact]
    public async Task StartServerAsync_WhenCommandIsNull_ShouldThrowArgumentNullException()
    {
        var sut = CreateSut();

        var act = () => sut.StartServerAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StartServerAsync_WhenServerNotRunning_ShouldStartSuccessfully()
    {
        var command = new StartServerCommand(
            Host: "localhost",
            Port: 3000,
            WebhookPath: "/api/github/webhooks",
            WebhookProxy: null,
            AppId: null,
            PrivateKey: null,
            WebhookSecret: null,
            BaseUrl: null,
            AppPaths: null);

        var result = await CreateSut().StartServerAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Host.Should().Be("localhost");
        result.Value.Port.Should().Be(3000);
        result.Value.IsRunning.Should().BeTrue();
    }

    [Fact]
    public async Task StartServerAsync_WhenServerAlreadyRunning_ShouldReturnFailure()
    {
        var command = new StartServerCommand(
            Host: "localhost",
            Port: 3000,
            WebhookPath: "/api/github/webhooks",
            WebhookProxy: null,
            AppId: null,
            PrivateKey: null,
            WebhookSecret: null,
            BaseUrl: null,
            AppPaths: null);

        var sut = CreateSut();
        await sut.StartServerAsync(command);

        var result = await sut.StartServerAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("server_already_running");
    }

    [Fact]
    public async Task StartServerAsync_WhenAppIdAndPrivateKeyProvided_ShouldInitializeApp()
    {
        var appId = GitHubAppId.Create(123);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var command = new StartServerCommand(
            Host: "localhost",
            Port: 3000,
            WebhookPath: "/api/github/webhooks",
            WebhookProxy: null,
            AppId: appId,
            PrivateKey: privateKey,
            WebhookSecret: "my-webhook-secret",
            BaseUrl: null,
            AppPaths: null);

        var result = await CreateSut().StartServerAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsRunning.Should().BeTrue();
    }

    [Fact]
    public async Task StartServerAsync_WhenAppIdProvidedWithoutWebhookSecret_ShouldReturnFailure()
    {
        var appId = GitHubAppId.Create(123);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var command = new StartServerCommand(
            Host: "localhost",
            Port: 3000,
            WebhookPath: "/api/github/webhooks",
            WebhookProxy: null,
            AppId: appId,
            PrivateKey: privateKey,
            WebhookSecret: null,
            BaseUrl: null,
            AppPaths: null);

        var result = await CreateSut().StartServerAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("webhook_secret_required");
    }

    [Fact]
    public async Task StartServerAsync_WhenAppIdProvidedWithEmptyWebhookSecret_ShouldReturnFailure()
    {
        var appId = GitHubAppId.Create(123);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var command = new StartServerCommand(
            Host: "localhost",
            Port: 3000,
            WebhookPath: "/api/github/webhooks",
            WebhookProxy: null,
            AppId: appId,
            PrivateKey: privateKey,
            WebhookSecret: "   ",
            BaseUrl: null,
            AppPaths: null);

        var result = await CreateSut().StartServerAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("webhook_secret_required");
    }

    [Fact]
    public async Task StartServerAsync_WhenAppIdProvidedWithoutPrivateKey_ShouldStartInSetupMode()
    {
        var appId = GitHubAppId.Create(123);
        var command = new StartServerCommand(
            Host: "localhost",
            Port: 3000,
            WebhookPath: "/api/github/webhooks",
            WebhookProxy: null,
            AppId: appId,
            PrivateKey: null,
            WebhookSecret: null,
            BaseUrl: null,
            AppPaths: null);

        var result = await CreateSut().StartServerAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsRunning.Should().BeTrue();
    }

    [Fact]
    public async Task StopServerAsync_WhenCommandIsNull_ShouldThrowArgumentNullException()
    {
        var sut = CreateSut();

        var act = () => sut.StopServerAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StopServerAsync_WhenServerRunning_ShouldStopSuccessfully()
    {
        var sut = CreateSut();
        var startCommand = new StartServerCommand(
            Host: "localhost",
            Port: 3000,
            WebhookPath: "/api/github/webhooks",
            WebhookProxy: null,
            AppId: null,
            PrivateKey: null,
            WebhookSecret: null,
            BaseUrl: null,
            AppPaths: null);
        await sut.StartServerAsync(startCommand);

        var stopCommand = new StopServerCommand(Graceful: false);
        var result = await sut.StopServerAsync(stopCommand);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task StopServerAsync_WhenServerNotRunning_ShouldReturnFailure()
    {
        var stopCommand = new StopServerCommand(Graceful: false);

        var result = await CreateSut().StopServerAsync(stopCommand);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("server_not_running");
    }

    [Fact]
    public async Task StopServerAsync_WhenGracefulShutdownRequested_ShouldStopSuccessfully()
    {
        var sut = CreateSut();
        var startCommand = new StartServerCommand(
            Host: "localhost",
            Port: 3000,
            WebhookPath: "/api/github/webhooks",
            WebhookProxy: null,
            AppId: null,
            PrivateKey: null,
            WebhookSecret: null,
            BaseUrl: null,
            AppPaths: null);
        await sut.StartServerAsync(startCommand);

        var stopCommand = new StopServerCommand(Graceful: true);
        var result = await sut.StopServerAsync(stopCommand);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAppAsync_WhenCommandIsNull_ShouldThrowArgumentNullException()
    {
        var sut = CreateSut();

        var act = () => sut.LoadAppAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task LoadAppAsync_WhenAppPathIsNull_ShouldReturnFailure()
    {
        var command = new LoadAppCommand(AppPath: null);

        var result = await CreateSut().LoadAppAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("app_path_required");
    }

    [Fact]
    public async Task LoadAppAsync_WhenAppPathIsEmpty_ShouldReturnFailure()
    {
        var command = new LoadAppCommand(AppPath: "");

        var result = await CreateSut().LoadAppAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("app_path_required");
    }

    [Fact]
    public async Task LoadAppAsync_WhenAppPathIsWhitespace_ShouldReturnFailure()
    {
        var command = new LoadAppCommand(AppPath: "   ");

        var result = await CreateSut().LoadAppAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("app_path_required");
    }

    [Fact]
    public async Task LoadAppAsync_WhenAppPathIsValid_ShouldReturnSuccess()
    {
        var command = new LoadAppCommand(AppPath: "/path/to/app");

        var result = await CreateSut().LoadAppAsync(command);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetAppInfoAsync_WhenNoAppLoaded_ShouldReturnSetupMode()
    {
        var result = await CreateSut().GetAppInfoAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsSetupMode.Should().BeTrue();
        result.Value.AppId.Should().BeNull();
        result.Value.AppName.Should().BeNull();
    }

    [Fact]
    public async Task GetAppInfoAsync_WhenAppLoaded_ShouldReturnAppInfo()
    {
        var sut = CreateSut();
        var appId = GitHubAppId.Create(123);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var startCommand = new StartServerCommand(
            Host: "localhost",
            Port: 3000,
            WebhookPath: "/api/github/webhooks",
            WebhookProxy: null,
            AppId: appId,
            PrivateKey: privateKey,
            WebhookSecret: "secret",
            BaseUrl: null,
            AppPaths: null);
        await sut.StartServerAsync(startCommand);

        var result = await sut.GetAppInfoAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsSetupMode.Should().BeFalse();
        result.Value.AppId.Should().NotBeNull();
    }

    [Fact]
    public async Task ServerLifecycle_StartThenStop_ShouldTransitionCorrectly()
    {
        var sut = CreateSut();
        var startCommand = new StartServerCommand(
            Host: "localhost",
            Port: 3000,
            WebhookPath: "/api/github/webhooks",
            WebhookProxy: null,
            AppId: null,
            PrivateKey: null,
            WebhookSecret: null,
            BaseUrl: null,
            AppPaths: null);

        var startResult = await sut.StartServerAsync(startCommand);
        startResult.IsSuccess.Should().BeTrue();

        var stopCommand = new StopServerCommand(Graceful: false);
        var stopResult = await sut.StopServerAsync(stopCommand);
        stopResult.IsSuccess.Should().BeTrue();

        var infoResult = await sut.GetAppInfoAsync();
        infoResult.Value!.IsSetupMode.Should().BeTrue();
    }
}
