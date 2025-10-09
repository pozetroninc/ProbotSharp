// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Application.UseCases;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Tests.UseCases;

/// <summary>
/// Tests for <see cref="RunSetupWizardUseCase"/>.
/// Verifies GitHub App setup wizard operations including manifest creation, OAuth flow, and credential management.
/// </summary>
public sealed class RunSetupWizardUseCaseTests
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

    private readonly IManifestPersistencePort _manifestPersistence = Substitute.For<IManifestPersistencePort>();
    private readonly IGitHubAppManifestPort _appManifest = Substitute.For<IGitHubAppManifestPort>();
    private readonly IEnvironmentConfigurationPort _envConfig = Substitute.For<IEnvironmentConfigurationPort>();
    private readonly IWebhookChannelPort _webhookChannel = Substitute.For<IWebhookChannelPort>();
    private readonly ILoggingPort _logger = Substitute.For<ILoggingPort>();

    private RunSetupWizardUseCase CreateSut()
        => new(_manifestPersistence, _appManifest, _envConfig, _webhookChannel, _logger);

    #region CreateManifestAsync Tests

    [Fact]
    public async Task CreateManifestAsync_WhenCommandIsNull_ShouldThrowArgumentNullException()
    {
        var sut = CreateSut();

        var act = () => sut.CreateManifestAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateManifestAsync_WhenAppNameIsNull_ShouldReturnFailure()
    {
        var command = new CreateManifestCommand(
            AppName: null,
            Description: "Test app",
            BaseUrl: "https://example.com",
            WebhookProxyUrl: null,
            Homepage: null,
            IsPublic: false);

        var result = await CreateSut().CreateManifestAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("app_name_required");
    }

    [Fact]
    public async Task CreateManifestAsync_WhenAppNameIsEmpty_ShouldReturnFailure()
    {
        var command = new CreateManifestCommand(
            AppName: "",
            Description: "Test app",
            BaseUrl: "https://example.com",
            WebhookProxyUrl: null,
            Homepage: null,
            IsPublic: false);

        var result = await CreateSut().CreateManifestAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("app_name_required");
    }

    [Fact]
    public async Task CreateManifestAsync_WhenBaseUrlIsNull_ShouldReturnFailure()
    {
        var command = new CreateManifestCommand(
            AppName: "Test App",
            Description: "Test app",
            BaseUrl: null,
            WebhookProxyUrl: null,
            Homepage: null,
            IsPublic: false);

        var result = await CreateSut().CreateManifestAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("base_url_required");
    }

    [Fact]
    public async Task CreateManifestAsync_WhenBaseUrlIsEmpty_ShouldReturnFailure()
    {
        var command = new CreateManifestCommand(
            AppName: "Test App",
            Description: "Test app",
            BaseUrl: "   ",
            WebhookProxyUrl: null,
            Homepage: null,
            IsPublic: false);

        var result = await CreateSut().CreateManifestAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("base_url_required");
    }

    [Fact]
    public async Task CreateManifestAsync_WhenValidCommand_ShouldCreateAndSaveManifest()
    {
        var command = new CreateManifestCommand(
            AppName: "Test App",
            Description: "Test app",
            BaseUrl: "https://example.com",
            WebhookProxyUrl: null,
            Homepage: null,
            IsPublic: false);

        _manifestPersistence.SaveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await CreateSut().CreateManifestAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
        result.Value.Should().Contain("Test App");
        await _manifestPersistence.Received(1).SaveAsync(
            Arg.Is<string>(json => json.Contains("Test App")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateManifestAsync_WhenSaveFails_ShouldReturnFailure()
    {
        var command = new CreateManifestCommand(
            AppName: "Test App",
            Description: "Test app",
            BaseUrl: "https://example.com",
            WebhookProxyUrl: null,
            Homepage: null,
            IsPublic: false);

        _manifestPersistence.SaveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure("save_error", "Failed to save"));

        var result = await CreateSut().CreateManifestAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("save_error");
    }

    [Fact]
    public async Task CreateManifestAsync_WhenSaveFailsWithoutError_ShouldReturnDefaultFailure()
    {
        var command = new CreateManifestCommand(
            AppName: "Test App",
            Description: "Test app",
            BaseUrl: "https://example.com",
            WebhookProxyUrl: null,
            Homepage: null,
            IsPublic: false);

        _manifestPersistence.SaveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result(false, null));

        var result = await CreateSut().CreateManifestAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("manifest_save_failed");
    }

    #endregion

    #region GetManifestAsync Tests

    [Fact]
    public async Task GetManifestAsync_WhenCommandIsNull_ShouldThrowArgumentNullException()
    {
        var sut = CreateSut();

        var act = () => sut.GetManifestAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void GetManifestCommand_WhenBaseUrlIsNull_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new GetManifestCommand(
                BaseUrl: null!,
                AppName: "Test App",
                Description: "Test Description",
                HomepageUrl: "https://example.com",
                WebhookProxyUrl: null,
                IsPublic: false));

        exception.ParamName.Should().Be("BaseUrl");
    }

    [Fact]
    public async Task GetManifestAsync_WhenManifestExists_ShouldReturnExisting()
    {
        var existingManifest = "{\"name\":\"Existing App\"}";
        var command = new GetManifestCommand(
            BaseUrl: "https://example.com",
            AppName: "Test",
            Description: null,
            WebhookProxyUrl: null,
            HomepageUrl: null,
            IsPublic: false);

        _manifestPersistence.GetAsync(Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success(existingManifest));

        var result = await CreateSut().GetManifestAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ManifestJson.Should().Be(existingManifest);
        result.Value.CreateAppUrl.Should().BeOfType<Uri>();
        result.Value.CreateAppUrl.Host.Should().Be("github.com");
    }

    [Fact]
    public async Task GetManifestAsync_WhenManifestDoesNotExist_ShouldCreateNew()
    {
        var command = new GetManifestCommand(
            BaseUrl: "https://example.com",
            AppName: "Test",
            Description: "Test app",
            WebhookProxyUrl: null,
            HomepageUrl: null,
            IsPublic: false);

        _manifestPersistence.GetAsync(Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success(null!));
        _manifestPersistence.SaveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await CreateSut().GetManifestAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ManifestJson.Should().NotBeNullOrEmpty();
        result.Value.CreateAppUrl.Should().BeOfType<Uri>();
        await _manifestPersistence.Received(1).SaveAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetManifestAsync_WhenCreatingNewFails_ShouldReturnFailure()
    {
        var command = new GetManifestCommand(
            BaseUrl: "https://example.com",
            AppName: "Test",
            Description: null,
            WebhookProxyUrl: null,
            HomepageUrl: null,
            IsPublic: false);

        _manifestPersistence.GetAsync(Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success(""));
        _manifestPersistence.SaveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure("creation_error", "Failed"));

        var result = await CreateSut().GetManifestAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("creation_error");
    }

    #endregion

    #region CompleteSetupAsync Tests

    [Fact]
    public async Task CompleteSetupAsync_WhenCommandIsNull_ShouldThrowArgumentNullException()
    {
        var sut = CreateSut();

        var act = () => sut.CompleteSetupAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CompleteSetupAsync_WhenCodeIsNull_ShouldReturnFailure()
    {
        var command = new CompleteSetupCommand(Code: null, BaseUrl: "https://example.com");

        var result = await CreateSut().CompleteSetupAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("code_required");
    }

    [Fact]
    public async Task CompleteSetupAsync_WhenCodeIsEmpty_ShouldReturnFailure()
    {
        var command = new CompleteSetupCommand(Code: "  ", BaseUrl: "https://example.com");

        var result = await CreateSut().CompleteSetupAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("code_required");
    }

    [Fact]
    public async Task CompleteSetupAsync_WhenValidCode_ShouldCompleteSetup()
    {
        var command = new CompleteSetupCommand(Code: "oauth_code_123", BaseUrl: "https://example.com");
        var credentials = new CreateAppFromCodeResponse(
            GitHubAppId.Create(123),
            "client_id",
            "client_secret",
            "webhook_secret",
            PrivateKeyPem.Create(ValidTestPrivateKey),
            "https://github.com/apps/test-app");

        _appManifest.CreateAppFromCodeAsync(command.Code, command.BaseUrl, Arg.Any<CancellationToken>())
            .Returns(Result<CreateAppFromCodeResponse>.Success(credentials));
        _envConfig.SaveAppCredentialsAsync(
                Arg.Any<GitHubAppId>(),
                Arg.Any<PrivateKeyPem>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await CreateSut().CompleteSetupAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("https://github.com/apps/test-app");
    }

    [Fact]
    public async Task CompleteSetupAsync_WhenCredentialExchangeFails_ShouldReturnFailure()
    {
        var command = new CompleteSetupCommand(Code: "oauth_code_123", BaseUrl: "https://example.com");

        _appManifest.CreateAppFromCodeAsync(command.Code, command.BaseUrl, Arg.Any<CancellationToken>())
            .Returns(Result<CreateAppFromCodeResponse>.Failure("exchange_error", "Failed to exchange"));

        var result = await CreateSut().CompleteSetupAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("exchange_error");
    }

    [Fact]
    public async Task CompleteSetupAsync_WhenCredentialExchangeReturnsNull_ShouldReturnFailure()
    {
        var command = new CompleteSetupCommand(Code: "oauth_code_123", BaseUrl: "https://example.com");

        _appManifest.CreateAppFromCodeAsync(command.Code, command.BaseUrl, Arg.Any<CancellationToken>())
            .Returns(Result<CreateAppFromCodeResponse>.Success(null!));

        var result = await CreateSut().CompleteSetupAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("credentials_null");
    }

    [Fact]
    public async Task CompleteSetupAsync_WhenCredentialSaveFails_ShouldReturnFailure()
    {
        var command = new CompleteSetupCommand(Code: "oauth_code_123", BaseUrl: "https://example.com");
        var credentials = new CreateAppFromCodeResponse(
            GitHubAppId.Create(123),
            "client_id",
            "client_secret",
            "webhook_secret",
            PrivateKeyPem.Create(ValidTestPrivateKey),
            "https://github.com/apps/test-app");

        _appManifest.CreateAppFromCodeAsync(command.Code, command.BaseUrl, Arg.Any<CancellationToken>())
            .Returns(Result<CreateAppFromCodeResponse>.Success(credentials));
        _envConfig.SaveAppCredentialsAsync(
                Arg.Any<GitHubAppId>(),
                Arg.Any<PrivateKeyPem>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Failure("save_error", "Failed to save"));

        var result = await CreateSut().CompleteSetupAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("save_error");
    }

    #endregion

    #region ImportAppCredentialsAsync Tests

    [Fact]
    public async Task ImportAppCredentialsAsync_WhenCommandIsNull_ShouldThrowArgumentNullException()
    {
        var sut = CreateSut();

        var act = () => sut.ImportAppCredentialsAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ImportAppCredentialsAsync_WhenValidCredentials_ShouldImportSuccessfully()
    {
        var command = new ImportAppCredentialsCommand(
            GitHubAppId.Create(123),
            PrivateKeyPem.Create(ValidTestPrivateKey),
            "webhook_secret");

        _envConfig.SaveAppCredentialsAsync(
                command.AppId,
                command.PrivateKey,
                command.WebhookSecret,
                null,
                null,
                Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await CreateSut().ImportAppCredentialsAsync(command);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ImportAppCredentialsAsync_WhenSaveFails_ShouldReturnFailure()
    {
        var command = new ImportAppCredentialsCommand(
            GitHubAppId.Create(123),
            PrivateKeyPem.Create(ValidTestPrivateKey),
            "webhook_secret");

        _envConfig.SaveAppCredentialsAsync(
                Arg.Any<GitHubAppId>(),
                Arg.Any<PrivateKeyPem>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Failure("import_error", "Failed to import"));

        var result = await CreateSut().ImportAppCredentialsAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("import_error");
    }

    #endregion

    #region CreateWebhookChannelAsync Tests

    [Fact]
    public async Task CreateWebhookChannelAsync_WhenCommandIsNull_ShouldThrowArgumentNullException()
    {
        var sut = CreateSut();

        var act = () => sut.CreateWebhookChannelAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateWebhookChannelAsync_WhenChannelCreationSucceeds_ShouldReturnChannel()
    {
        var command = new CreateWebhookChannelCommand();
        var channel = new CreateWebhookChannelResponse("https://smee.io/test-channel", DateTimeOffset.UtcNow);

        _webhookChannel.CreateChannelAsync(Arg.Any<CancellationToken>())
            .Returns(Result<CreateWebhookChannelResponse>.Success(channel));
        _envConfig.SaveWebhookProxyUrlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await CreateSut().CreateWebhookChannelAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(channel);
    }

    [Fact]
    public async Task CreateWebhookChannelAsync_WhenChannelCreationFails_ShouldReturnFailure()
    {
        var command = new CreateWebhookChannelCommand();

        _webhookChannel.CreateChannelAsync(Arg.Any<CancellationToken>())
            .Returns(Result<CreateWebhookChannelResponse>.Failure("channel_error", "Failed to create channel"));

        var result = await CreateSut().CreateWebhookChannelAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("channel_error");
    }

    [Fact]
    public async Task CreateWebhookChannelAsync_WhenChannelCreationReturnsNull_ShouldReturnFailure()
    {
        var command = new CreateWebhookChannelCommand();

        _webhookChannel.CreateChannelAsync(Arg.Any<CancellationToken>())
            .Returns(Result<CreateWebhookChannelResponse>.Success(null!));

        var result = await CreateSut().CreateWebhookChannelAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("webhook_channel_null");
    }

    [Fact]
    public async Task CreateWebhookChannelAsync_WhenSaveUrlFails_ShouldStillSucceed()
    {
        var command = new CreateWebhookChannelCommand();
        var channel = new CreateWebhookChannelResponse("https://smee.io/test-channel", DateTimeOffset.UtcNow);

        _webhookChannel.CreateChannelAsync(Arg.Any<CancellationToken>())
            .Returns(Result<CreateWebhookChannelResponse>.Success(channel));
        _envConfig.SaveWebhookProxyUrlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure("save_error", "Failed to save"));

        var result = await CreateSut().CreateWebhookChannelAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(channel);
    }

    #endregion
}
