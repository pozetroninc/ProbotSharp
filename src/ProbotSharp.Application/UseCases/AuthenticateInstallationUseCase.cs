// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.UseCases;

/// <summary>
/// Use case for authenticating GitHub App installations.
/// Retrieves installation access tokens from cache or creates new ones via OAuth.
/// Implements hexagonal architecture by orchestrating token retrieval through ports.
/// </summary>
public sealed class AuthenticateInstallationUseCase : IInstallationAuthenticationPort
{
    private readonly IGitHubOAuthPort _gitHubOAuth;
    private readonly IAccessTokenCachePort _tokenCache;
    private readonly IClockPort _clock;

    public AuthenticateInstallationUseCase(
        IGitHubOAuthPort gitHubOAuth,
        IAccessTokenCachePort tokenCache,
        IClockPort clock)
    {
        _gitHubOAuth = gitHubOAuth;
        _tokenCache = tokenCache;
        _clock = clock;
    }

    public async Task<Result<InstallationAccessToken>> AuthenticateAsync(
        AuthenticateInstallationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Step 1: Check cache for existing valid token
        var cachedToken = await _tokenCache.GetAsync(command.InstallationId, cancellationToken).ConfigureAwait(false);
        if (cachedToken is not null && !cachedToken.IsExpired(_clock.UtcNow))
        {
            return Result<InstallationAccessToken>.Success(cachedToken);
        }

        // Step 2: Token not in cache or expired - request new token from GitHub
        var tokenResult = await _gitHubOAuth.CreateInstallationTokenAsync(
            command.InstallationId,
            cancellationToken).ConfigureAwait(false);

        if (!tokenResult.IsSuccess)
        {
            return tokenResult.Error is null
                ? Result<InstallationAccessToken>.Failure(
                    "token_creation_failed",
                    "Unable to create installation access token")
                : Result<InstallationAccessToken>.Failure(tokenResult.Error.Value);
        }

        var token = tokenResult.Value;
        if (token is null)
        {
            return Result<InstallationAccessToken>.Failure(
                "token_null",
                "Installation access token was null after successful creation");
        }

        // Step 3: Cache the new token
        await _tokenCache.SetAsync(command.InstallationId, token, cancellationToken).ConfigureAwait(false);

        return Result<InstallationAccessToken>.Success(token);
    }
}
