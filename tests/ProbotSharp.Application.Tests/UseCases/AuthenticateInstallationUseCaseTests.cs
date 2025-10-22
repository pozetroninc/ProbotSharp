// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Application.UseCases;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Application.Tests.UseCases;

/// <summary>
/// Tests for <see cref="AuthenticateInstallationUseCase"/>.
/// Verifies token caching, retrieval, and error handling for installation authentication.
/// </summary>
public sealed class AuthenticateInstallationUseCaseTests
{
    private readonly IGitHubOAuthPort _gitHubOAuth = Substitute.For<IGitHubOAuthPort>();
    private readonly IAccessTokenCachePort _tokenCache = Substitute.For<IAccessTokenCachePort>();
    private readonly IClockPort _clock = Substitute.For<IClockPort>();

    private AuthenticateInstallationUseCase CreateSut()
        => new(_gitHubOAuth, _tokenCache, _clock);

    [Fact]
    public async Task AuthenticateAsync_WhenCommandIsNull_ShouldThrowArgumentNullException()
    {
        var sut = CreateSut();

        var act = () => sut.AuthenticateAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AuthenticateAsync_WhenCachedTokenIsValid_ShouldReturnCachedToken()
    {
        var command = new AuthenticateInstallationCommand(InstallationId.Create(123));
        var cachedToken = CreateValidToken();
        var now = DateTimeOffset.UtcNow;

        _clock.UtcNow.Returns(now);
        _tokenCache.GetAsync(command.InstallationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InstallationAccessToken?>(cachedToken));

        var result = await CreateSut().AuthenticateAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(cachedToken);
        await _gitHubOAuth.DidNotReceive().CreateInstallationTokenAsync(
            Arg.Any<InstallationId>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthenticateAsync_WhenCachedTokenIsExpired_ShouldRequestNewToken()
    {
        var command = new AuthenticateInstallationCommand(InstallationId.Create(123));
        var expiredToken = CreateExpiredToken();
        var newToken = CreateValidToken();
        var now = DateTimeOffset.UtcNow;

        _clock.UtcNow.Returns(now);
        _tokenCache.GetAsync(command.InstallationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InstallationAccessToken?>(expiredToken));
        _gitHubOAuth.CreateInstallationTokenAsync(command.InstallationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<InstallationAccessToken>.Success(newToken)));

        var result = await CreateSut().AuthenticateAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(newToken);
        await _tokenCache.Received(1).SetAsync(
            command.InstallationId,
            newToken,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthenticateAsync_WhenNoTokenInCache_ShouldRequestNewToken()
    {
        var command = new AuthenticateInstallationCommand(InstallationId.Create(123));
        var newToken = CreateValidToken();

        _tokenCache.GetAsync(command.InstallationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InstallationAccessToken?>(null));
        _gitHubOAuth.CreateInstallationTokenAsync(command.InstallationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<InstallationAccessToken>.Success(newToken)));

        var result = await CreateSut().AuthenticateAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(newToken);
        await _tokenCache.Received(1).SetAsync(
            command.InstallationId,
            newToken,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthenticateAsync_WhenTokenCreationFails_ShouldReturnFailure()
    {
        var command = new AuthenticateInstallationCommand(InstallationId.Create(123));
        var error = new Error("token_creation_error", "GitHub API error");

        _tokenCache.GetAsync(command.InstallationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InstallationAccessToken?>(null));
        _gitHubOAuth.CreateInstallationTokenAsync(command.InstallationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<InstallationAccessToken>.Failure(error)));

        var result = await CreateSut().AuthenticateAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("token_creation_error");
        await _tokenCache.DidNotReceive().SetAsync(
            Arg.Any<InstallationId>(),
            Arg.Any<InstallationAccessToken>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthenticateAsync_WhenTokenCreationFailsWithoutError_ShouldReturnDefaultFailure()
    {
        var command = new AuthenticateInstallationCommand(InstallationId.Create(123));
        var failure = new Result<InstallationAccessToken>(false, null, null);

        _tokenCache.GetAsync(command.InstallationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InstallationAccessToken?>(null));
        _gitHubOAuth.CreateInstallationTokenAsync(command.InstallationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(failure));

        var result = await CreateSut().AuthenticateAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("token_creation_failed");
        result.Error!.Value.Message.Should().Be("Unable to create installation access token");
    }

    [Fact]
    public async Task AuthenticateAsync_WhenTokenCreationReturnsNull_ShouldReturnFailure()
    {
        var command = new AuthenticateInstallationCommand(InstallationId.Create(123));

        _tokenCache.GetAsync(command.InstallationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InstallationAccessToken?>(null));
        _gitHubOAuth.CreateInstallationTokenAsync(command.InstallationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<InstallationAccessToken>.Success(null!)));

        var result = await CreateSut().AuthenticateAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("token_null");
        result.Error!.Value.Message.Should().Be("Installation access token was null after successful creation");
    }

    [Fact]
    public async Task AuthenticateAsync_WhenNewTokenCreated_ShouldCacheToken()
    {
        var command = new AuthenticateInstallationCommand(InstallationId.Create(123));
        var newToken = CreateValidToken();

        _tokenCache.GetAsync(command.InstallationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InstallationAccessToken?>(null));
        _gitHubOAuth.CreateInstallationTokenAsync(command.InstallationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<InstallationAccessToken>.Success(newToken)));

        await CreateSut().AuthenticateAsync(command);

        await _tokenCache.Received(1).SetAsync(
            command.InstallationId,
            newToken,
            Arg.Any<CancellationToken>());
    }

    private static InstallationAccessToken CreateValidToken()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        return InstallationAccessToken.Create("ghs_validtoken123", expiresAt);
    }

    private static InstallationAccessToken CreateExpiredToken()
    {
        // Create a token that will expire very soon
        // We'll use reflection to set past expiration for testing
        var validToken = InstallationAccessToken.Create(
            "ghs_testTokenThatWillExpire1234567890123456789012",
            DateTimeOffset.UtcNow.AddHours(1));

        // Use reflection to set expiration to the past
        // For record types, the backing field is named <PropertyName>k__BackingField
        var expiresAtField = typeof(InstallationAccessToken)
            .GetField("<ExpiresAt>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (expiresAtField != null)
        {
            expiresAtField.SetValue(validToken, DateTimeOffset.UtcNow.AddHours(-1));
        }

        return validToken;
    }
}
