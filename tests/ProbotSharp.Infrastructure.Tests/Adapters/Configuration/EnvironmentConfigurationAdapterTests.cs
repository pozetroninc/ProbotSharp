// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Hosting;

using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Infrastructure.Adapters.Configuration;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Configuration;

/// <summary>
/// Tests for <see cref="EnvironmentConfigurationAdapter"/> covering file I/O and error scenarios.
/// </summary>
public sealed class EnvironmentConfigurationAdapterTests : IDisposable
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

    private readonly string _testDirectory;
    private readonly string _envFilePath;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<EnvironmentConfigurationAdapter> _logger;

    public EnvironmentConfigurationAdapterTests()
    {
        this._testDirectory = Path.Combine(Path.GetTempPath(), $"probot-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(this._testDirectory);
        this._envFilePath = Path.Combine(this._testDirectory, ".env.local");

        this._hostEnvironment = Substitute.For<IHostEnvironment>();
        this._hostEnvironment.ContentRootPath.Returns(this._testDirectory);

        this._logger = Substitute.For<ILogger<EnvironmentConfigurationAdapter>>();
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_ShouldWriteToEnvFile()
    {
        // Arrange
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        File.Exists(this._envFilePath).Should().BeTrue();

        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("APP_ID=12345");
        content.Should().Contain("WEBHOOK_SECRET=test-webhook-secret");
        content.Should().Contain("PRIVATE_KEY_BASE64=");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_ShouldIncludeClientCredentials_WhenProvided()
    {
        // Arrange
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";
        var clientId = "test-client-id";
        var clientSecret = "test-client-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(
            appId,
            privateKey,
            webhookSecret,
            clientId,
            clientSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("GITHUB_APP_CLIENT_ID=test-client-id");
        content.Should().Contain("GITHUB_APP_CLIENT_SECRET=test-client-secret");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_ShouldNotIncludeClientCredentials_WhenNotProvided()
    {
        // Arrange
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().NotContain("GITHUB_APP_CLIENT_ID");
        content.Should().NotContain("GITHUB_APP_CLIENT_SECRET");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_ShouldPreserveUnmanagedEntries()
    {
        // Arrange
        await File.WriteAllTextAsync(this._envFilePath, "CUSTOM_KEY=custom_value\nANOTHER_KEY=another_value");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("CUSTOM_KEY=custom_value");
        content.Should().Contain("ANOTHER_KEY=another_value");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_ShouldUpdateExistingCredentials()
    {
        // Arrange
        await File.WriteAllTextAsync(this._envFilePath, "APP_ID=99999\nWEBHOOK_SECRET=old-secret");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "new-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("APP_ID=12345");
        content.Should().Contain("WEBHOOK_SECRET=new-webhook-secret");
        content.Should().NotContain("99999");
        content.Should().NotContain("old-secret");
    }

    [Fact]
    public async Task SaveWebhookProxyUrlAsync_ShouldWriteToEnvFile()
    {
        // Arrange
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var webhookProxyUrl = "https://smee.io/abc123";

        // Act
        var result = await adapter.SaveWebhookProxyUrlAsync(webhookProxyUrl);

        // Assert
        result.IsSuccess.Should().BeTrue();
        File.Exists(this._envFilePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("WEBHOOK_PROXY_URL=https://smee.io/abc123");
    }

    [Fact]
    public async Task SaveWebhookProxyUrlAsync_ShouldPreserveExistingEntries()
    {
        // Arrange
        await File.WriteAllTextAsync(this._envFilePath, "APP_ID=12345\nWEBHOOK_SECRET=secret");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var webhookProxyUrl = "https://smee.io/abc123";

        // Act
        var result = await adapter.SaveWebhookProxyUrlAsync(webhookProxyUrl);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("APP_ID=12345");
        content.Should().Contain("WEBHOOK_SECRET=secret");
        content.Should().Contain("WEBHOOK_PROXY_URL=https://smee.io/abc123");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_ShouldReturnFailure_WhenDirectoryIsReadOnly()
    {
        // Arrange
        var readOnlyDir = Path.Combine(Path.GetTempPath(), $"readonly-{Guid.NewGuid()}");
        Directory.CreateDirectory(readOnlyDir);
        var readOnlyEnv = Substitute.For<IHostEnvironment>();
        readOnlyEnv.ContentRootPath.Returns(readOnlyDir);

        try
        {
            // Make directory read-only (platform-specific)
            if (OperatingSystem.IsWindows())
            {
                var dirInfo = new DirectoryInfo(readOnlyDir);
                dirInfo.Attributes |= FileAttributes.ReadOnly;
            }
            else
            {
                // On Unix, remove write permissions
                File.SetUnixFileMode(readOnlyDir, UnixFileMode.UserRead | UnixFileMode.UserExecute);
            }

            var adapter = new EnvironmentConfigurationAdapter(readOnlyEnv, this._logger);
            var appId = GitHubAppId.Create(12345);
            var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
            var webhookSecret = "test-webhook-secret";

            // Act
            var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Value.Code.Should().Be("environment_write_failed");
        }
        finally
        {
            // Cleanup
            if (OperatingSystem.IsWindows())
            {
                var dirInfo = new DirectoryInfo(readOnlyDir);
                dirInfo.Attributes &= ~FileAttributes.ReadOnly;
            }
            else
            {
                File.SetUnixFileMode(readOnlyDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }

            Directory.Delete(readOnlyDir, true);
        }
    }

    [Fact]
    public async Task SaveWebhookProxyUrlAsync_ShouldReturnFailure_OnIOError()
    {
        // Arrange
        var invalidEnv = Substitute.For<IHostEnvironment>();
        invalidEnv.ContentRootPath.Returns("/invalid/nonexistent/path/that/cannot/be/created/\0/");

        var adapter = new EnvironmentConfigurationAdapter(invalidEnv, this._logger);

        // Act
        var result = await adapter.SaveWebhookProxyUrlAsync("https://smee.io/abc123");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("environment_write_failed");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenHostEnvironmentIsNull()
    {
        var act = () => new EnvironmentConfigurationAdapter(null!, this._logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        var act = () => new EnvironmentConfigurationAdapter(this._hostEnvironment, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_ShouldThrowArgumentNullException_WhenAppIdIsNull()
    {
        // Arrange
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);

        // Act
        var act = async () => await adapter.SaveAppCredentialsAsync(null!, privateKey, "secret");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_ShouldThrowArgumentNullException_WhenPrivateKeyIsNull()
    {
        // Arrange
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);

        // Act
        var act = async () => await adapter.SaveAppCredentialsAsync(appId, null!, "secret");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_ShouldThrowArgumentException_WhenWebhookSecretIsEmpty()
    {
        // Arrange
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);

        // Act
        var act = async () => await adapter.SaveAppCredentialsAsync(appId, privateKey, string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SaveWebhookProxyUrlAsync_ShouldThrowArgumentException_WhenUrlIsEmpty()
    {
        // Arrange
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);

        // Act
        var act = async () => await adapter.SaveWebhookProxyUrlAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_ShouldHandleCommentsInExistingFile()
    {
        // Arrange
        await File.WriteAllTextAsync(this._envFilePath, "# This is a comment\nCUSTOM_KEY=value\n# Another comment");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("CUSTOM_KEY=value");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_ShouldHandleEmptyLinesInExistingFile()
    {
        // Arrange
        await File.WriteAllTextAsync(this._envFilePath, "CUSTOM_KEY=value\n\n\nANOTHER_KEY=value2");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("CUSTOM_KEY=value");
        content.Should().Contain("ANOTHER_KEY=value2");
    }

    // ==================== File System Exception Tests ====================

    [Fact]
    public async Task SaveAppCredentialsAsync_WhenPathTooLong_ShouldReturnFailure()
    {
        // Arrange
        var longPath = new string('x', 300); // Create a path that's too long
        var longPathEnv = Substitute.For<IHostEnvironment>();
        longPathEnv.ContentRootPath.Returns($"/tmp/{longPath}/{longPath}/{longPath}");

        var adapter = new EnvironmentConfigurationAdapter(longPathEnv, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("environment_write_failed");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_WhenDirectoryNotFound_ShouldCreateDirectory()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"test-dir-{Guid.NewGuid()}", "nested", "deep");
        var nonExistentEnv = Substitute.For<IHostEnvironment>();
        nonExistentEnv.ContentRootPath.Returns(nonExistentDir);

        try
        {
            var adapter = new EnvironmentConfigurationAdapter(nonExistentEnv, this._logger);
            var appId = GitHubAppId.Create(12345);
            var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
            var webhookSecret = "test-webhook-secret";

            // Act
            var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

            // Assert
            result.IsSuccess.Should().BeTrue();
            Directory.Exists(nonExistentDir).Should().BeTrue();
            File.Exists(Path.Combine(nonExistentDir, ".env.local")).Should().BeTrue();
        }
        finally
        {
            // Cleanup
            var parentDir = Path.Combine(Path.GetTempPath(), Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(nonExistentDir))!));
            if (Directory.Exists(parentDir))
            {
                try
                {
                    Directory.Delete(parentDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    [Fact]
    public async Task SaveWebhookProxyUrlAsync_WhenFileAccessDenied_ShouldReturnFailure()
    {
        // Arrange
        var readOnlyDir = Path.Combine(Path.GetTempPath(), $"readonly-webhook-{Guid.NewGuid()}");
        Directory.CreateDirectory(readOnlyDir);
        var readOnlyEnv = Substitute.For<IHostEnvironment>();
        readOnlyEnv.ContentRootPath.Returns(readOnlyDir);

        try
        {
            // Make directory read-only
            if (OperatingSystem.IsWindows())
            {
                var dirInfo = new DirectoryInfo(readOnlyDir);
                dirInfo.Attributes |= FileAttributes.ReadOnly;
            }
            else
            {
                File.SetUnixFileMode(readOnlyDir, UnixFileMode.UserRead | UnixFileMode.UserExecute);
            }

            var adapter = new EnvironmentConfigurationAdapter(readOnlyEnv, this._logger);

            // Act
            var result = await adapter.SaveWebhookProxyUrlAsync("https://smee.io/test");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Value.Code.Should().Be("environment_write_failed");
        }
        finally
        {
            // Cleanup
            if (OperatingSystem.IsWindows())
            {
                var dirInfo = new DirectoryInfo(readOnlyDir);
                dirInfo.Attributes &= ~FileAttributes.ReadOnly;
            }
            else
            {
                File.SetUnixFileMode(readOnlyDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }

            Directory.Delete(readOnlyDir, true);
        }
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_WhenFileInUse_ShouldReturnFailure()
    {
        // Arrange
        var lockedDir = Path.Combine(Path.GetTempPath(), $"locked-{Guid.NewGuid()}");
        Directory.CreateDirectory(lockedDir);
        var lockedFile = Path.Combine(lockedDir, ".env.local");
        var lockedEnv = Substitute.For<IHostEnvironment>();
        lockedEnv.ContentRootPath.Returns(lockedDir);

        try
        {
            // Create and lock the file
            await File.WriteAllTextAsync(lockedFile, "EXISTING=value");
            using var lockedStream = new FileStream(lockedFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var adapter = new EnvironmentConfigurationAdapter(lockedEnv, this._logger);
            var appId = GitHubAppId.Create(12345);
            var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
            var webhookSecret = "test-webhook-secret";

            // Act
            var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Value.Code.Should().Be("environment_write_failed");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(lockedDir))
            {
                try
                {
                    Directory.Delete(lockedDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    [Fact]
    public async Task SaveWebhookProxyUrlAsync_WhenInvalidPath_ShouldReturnFailure()
    {
        // Arrange - Use null character which is invalid in paths
        var invalidEnv = Substitute.For<IHostEnvironment>();
        invalidEnv.ContentRootPath.Returns($"/tmp/invalid\0path/test");

        var adapter = new EnvironmentConfigurationAdapter(invalidEnv, this._logger);

        // Act
        var result = await adapter.SaveWebhookProxyUrlAsync("https://smee.io/test");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("environment_write_failed");
    }

    // ==================== Malformed Content Tests ====================

    [Fact]
    public async Task SaveAppCredentialsAsync_WithMalformedLine_ShouldSkipAndContinue()
    {
        // Arrange - Lines without '=' delimiter should be skipped
        await File.WriteAllTextAsync(this._envFilePath, "VALID_KEY=value\nMALFORMED_LINE_WITHOUT_EQUALS\nANOTHER_KEY=another_value");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("VALID_KEY=value");
        content.Should().Contain("ANOTHER_KEY=another_value");
        content.Should().NotContain("MALFORMED_LINE_WITHOUT_EQUALS");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_WithLineStartingWithEquals_ShouldSkipLine()
    {
        // Arrange - Lines starting with '=' have no key and should be skipped
        await File.WriteAllTextAsync(this._envFilePath, "VALID_KEY=value\n=value_without_key\nANOTHER_KEY=value2");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("VALID_KEY=value");
        content.Should().Contain("ANOTHER_KEY=value2");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_WithQuotedValues_ShouldPreserveQuotes()
    {
        // Arrange - Quoted values should be preserved as-is
        await File.WriteAllTextAsync(this._envFilePath, "QUOTED_VALUE=\"hello world\"\nSINGLE_QUOTED='test'");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("QUOTED_VALUE=\"hello world\"");
        content.Should().Contain("SINGLE_QUOTED='test'");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_WithEscapeSequences_ShouldPreserveAsLiteral()
    {
        // Arrange - Escape sequences should be preserved literally
        await File.WriteAllTextAsync(this._envFilePath, "ESCAPED_VALUE=hello\\nworld\\t\\r");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("ESCAPED_VALUE=hello\\nworld\\t\\r");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_WithEmptyValue_ShouldAllowEmptyString()
    {
        // Arrange - Empty values should be allowed
        await File.WriteAllTextAsync(this._envFilePath, "EMPTY_KEY=\nANOTHER_KEY=value");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("EMPTY_KEY=");
        content.Should().Contain("ANOTHER_KEY=value");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_WithLeadingTrailingWhitespace_ShouldTrim()
    {
        // Arrange - Whitespace should be trimmed from keys and values
        await File.WriteAllTextAsync(this._envFilePath, "  SPACED_KEY  =  spaced value  \n\t\tTABBED_KEY\t\t=\t\ttabbed value\t\t");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("SPACED_KEY=spaced value");
        content.Should().Contain("TABBED_KEY=tabbed value");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_WithMultipleEqualsInValue_ShouldPreserveAll()
    {
        // Arrange - Values can contain '=' characters
        await File.WriteAllTextAsync(this._envFilePath, "BASE64_KEY=YWJjZGVmZ2hpams=\nURL_WITH_PARAMS=https://example.com?a=1&b=2");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("BASE64_KEY=YWJjZGVmZ2hpams=");
        content.Should().Contain("URL_WITH_PARAMS=https://example.com?a=1&b=2");
    }

    // ==================== Special Characters Tests ====================

    [Fact]
    public async Task SaveAppCredentialsAsync_WithUnicodeCharacters_ShouldHandleCorrectly()
    {
        // Arrange - Unicode characters including emoji should be preserved
        await File.WriteAllTextAsync(this._envFilePath, "UNICODE_KEY=Hello 世界 🌍 مرحبا");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("UNICODE_KEY=Hello 世界 🌍 مرحبا");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_WithUrlEncodedValues_ShouldPreserveEncoding()
    {
        // Arrange - URL encoding should be preserved
        await File.WriteAllTextAsync(this._envFilePath, "ENCODED_URL=https%3A%2F%2Fexample.com%2Fpath%20with%20spaces");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("ENCODED_URL=https%3A%2F%2Fexample.com%2Fpath%20with%20spaces");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_WithBase64Values_ShouldPreserveEncoding()
    {
        // Arrange - Base64 encoded values should be preserved
        await File.WriteAllTextAsync(this._envFilePath, "BASE64_SECRET=SGVsbG8gV29ybGQhIFRoaXMgaXMgYSBiYXNlNjQgZW5jb2RlZCBzdHJpbmc=");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("BASE64_SECRET=SGVsbG8gV29ybGQhIFRoaXMgaXMgYSBiYXNlNjQgZW5jb2RlZCBzdHJpbmc=");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_WithJsonValues_ShouldPreserveJson()
    {
        // Arrange - JSON as value should be preserved
        await File.WriteAllTextAsync(this._envFilePath, "JSON_CONFIG={\"key\":\"value\",\"nested\":{\"array\":[1,2,3]}}");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("JSON_CONFIG={\"key\":\"value\",\"nested\":{\"array\":[1,2,3]}}");
    }

    // ==================== Concurrent Access Tests ====================

    [Fact]
    public async Task SaveAppCredentialsAsync_WhenCalledConcurrently_ShouldHandleSafely()
    {
        // Arrange
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);

        // Act - Execute 5 concurrent writes
        var tasks = Enumerable.Range(1, 5).Select(i =>
            adapter.SaveAppCredentialsAsync(appId, privateKey, $"webhook-secret-{i}"));
        var results = await Task.WhenAll(tasks);

        // Assert - All operations should complete (some may fail due to file locking, which is acceptable)
        results.Should().NotBeNull();
        results.Should().HaveCount(5);
        results.Should().Contain(r => r.IsSuccess); // At least one should succeed
    }

    [Fact]
    public async Task SaveWebhookProxyUrlAsync_WhenCalledConcurrently_ShouldHandleSafely()
    {
        // Arrange
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);

        // Act - Execute 5 concurrent writes
        var tasks = Enumerable.Range(1, 5).Select(i =>
            adapter.SaveWebhookProxyUrlAsync($"https://smee.io/test-{i}"));
        var results = await Task.WhenAll(tasks);

        // Assert - All operations should complete
        results.Should().NotBeNull();
        results.Should().HaveCount(5);
        results.Should().Contain(r => r.IsSuccess); // At least one should succeed
    }

    // ==================== Edge Cases Tests ====================

    [Fact]
    public async Task SaveAppCredentialsAsync_WithVeryLargeFile_ShouldHandleCorrectly()
    {
        // Arrange - Create a file with 1000 lines
        var largeContent = string.Join("\n", Enumerable.Range(1, 1000).Select(i => $"KEY_{i}=value_{i}"));
        await File.WriteAllTextAsync(this._envFilePath, largeContent);

        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("KEY_1=value_1");
        content.Should().Contain("KEY_500=value_500");
        content.Should().Contain("KEY_1000=value_1000");
        content.Should().Contain("APP_ID=12345");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_WithDuplicateKeys_ShouldUseLastValue()
    {
        // Arrange - Duplicate keys should use the last value
        await File.WriteAllTextAsync(this._envFilePath, "DUPLICATE_KEY=first_value\nDUPLICATE_KEY=second_value\nDUPLICATE_KEY=third_value");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("DUPLICATE_KEY=third_value");
        content.Should().NotContain("first_value");
        content.Should().NotContain("second_value");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_WithVariableExpansion_ShouldNotExpand()
    {
        // Arrange - Variable references should be treated as literal strings
        await File.WriteAllTextAsync(this._envFilePath, "VAR_REF=$HOME/path\nBRACED_VAR=${USER}/data");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("VAR_REF=$HOME/path");
        content.Should().Contain("BRACED_VAR=${USER}/data");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_WithOnlyComments_ShouldPreserveNone()
    {
        // Arrange - File with only comments should be treated as empty for unmanaged keys
        await File.WriteAllTextAsync(this._envFilePath, "# Comment 1\n# Comment 2\n# Comment 3");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("APP_ID=12345");
        content.Should().NotContain("Comment 1");
        content.Should().NotContain("Comment 2");
        content.Should().NotContain("Comment 3");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_WithCaseVariationsInKeys_ShouldBeCaseInsensitive()
    {
        // Arrange - Keys should be case-insensitive (APP_ID and app_id are the same)
        await File.WriteAllTextAsync(this._envFilePath, "custom_key=lowercase\nCUSTOM_KEY=uppercase");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        // Should only have one entry for CUSTOM_KEY (last value wins due to case-insensitive comparison)
        var keyCount = content.Split('\n').Count(line => line.StartsWith("CUSTOM_KEY=", StringComparison.OrdinalIgnoreCase));
        keyCount.Should().Be(1);
    }

    [Fact]
    public async Task SaveWebhookProxyUrlAsync_WithSpecialCharactersInUrl_ShouldPreserve()
    {
        // Arrange
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var specialUrl = "https://smee.io/abc-123_456.789~test";

        // Act
        var result = await adapter.SaveWebhookProxyUrlAsync(specialUrl);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain($"WEBHOOK_PROXY_URL={specialUrl}");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_WithVeryLongValue_ShouldHandleCorrectly()
    {
        // Arrange - Test with a very long value (10KB)
        var longValue = new string('x', 10240);
        await File.WriteAllTextAsync(this._envFilePath, $"LONG_VALUE={longValue}");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain($"LONG_VALUE={longValue}");
    }

    [Fact]
    public async Task SaveAppCredentialsAsync_WithMixedLineEndings_ShouldHandleCorrectly()
    {
        // Arrange - Mix of \r\n (Windows) and \n (Unix) line endings
        await File.WriteAllTextAsync(this._envFilePath, "KEY1=value1\r\nKEY2=value2\nKEY3=value3\r\n");
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(ValidTestPrivateKey);
        var webhookSecret = "test-webhook-secret";

        // Act
        var result = await adapter.SaveAppCredentialsAsync(appId, privateKey, webhookSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("KEY1=value1");
        content.Should().Contain("KEY2=value2");
        content.Should().Contain("KEY3=value3");
    }

    [Fact]
    public async Task SaveWebhookProxyUrlAsync_WhenFileDoesNotExist_ShouldCreateNewFile()
    {
        // Arrange
        var adapter = new EnvironmentConfigurationAdapter(this._hostEnvironment, this._logger);
        // Ensure file doesn't exist
        if (File.Exists(this._envFilePath))
        {
            File.Delete(this._envFilePath);
        }

        // Act
        var result = await adapter.SaveWebhookProxyUrlAsync("https://smee.io/new");

        // Assert
        result.IsSuccess.Should().BeTrue();
        File.Exists(this._envFilePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(this._envFilePath);
        content.Should().Contain("WEBHOOK_PROXY_URL=https://smee.io/new");
    }

    public void Dispose()
    {
        if (Directory.Exists(this._testDirectory))
        {
            try
            {
                Directory.Delete(this._testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
