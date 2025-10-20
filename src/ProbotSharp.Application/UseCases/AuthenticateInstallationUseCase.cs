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

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticateInstallationUseCase"/> class.
    /// </summary>
    /// <param name="gitHubOAuth">The GitHub OAuth port for creating installation tokens.</param>
    /// <param name="tokenCache">The access token cache port for caching tokens.</param>
    /// <param name="clock">The clock port for checking token expiration.</param>
    public AuthenticateInstallationUseCase(
        IGitHubOAuthPort gitHubOAuth,
        IAccessTokenCachePort tokenCache,
        IClockPort clock)
    {
        this._gitHubOAuth = gitHubOAuth;
        this._tokenCache = tokenCache;
        this._clock = clock;
    }

    /// <summary>
    /// Authenticates a GitHub App installation and retrieves an access token.
    /// </summary>
    /// <param name="command">The command containing installation authentication information.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A result containing the installation access token if successful.</returns>
    public async Task<Result<InstallationAccessToken>> AuthenticateAsync(
        AuthenticateInstallationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Step 1: Check cache for existing valid token
        var cachedToken = await this._tokenCache.GetAsync(command.InstallationId, cancellationToken).ConfigureAwait(false);
        if (cachedToken is not null && !cachedToken.IsExpired(this._clock.UtcNow))
        {
            return Result<InstallationAccessToken>.Success(cachedToken);
        }

        // Step 2: Token not in cache or expired - request new token from GitHub
        var tokenResult = await this._gitHubOAuth.CreateInstallationTokenAsync(
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
        await this._tokenCache.SetAsync(command.InstallationId, token, cancellationToken).ConfigureAwait(false);

        return Result<InstallationAccessToken>.Success(token);
    }
}
