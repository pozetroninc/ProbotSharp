// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Infrastructure.Adapters.GitHub;

/// <summary>
/// GitHub OAuth client adapter for creating and caching installation access tokens.
/// </summary>
public sealed partial class GitHubOAuthClient : IGitHubOAuthPort
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAccessTokenCachePort _cache;
    private readonly ILogger<GitHubOAuthClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubOAuthClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory for GitHub API requests.</param>
    /// <param name="cache">The cache for storing installation access tokens.</param>
    /// <param name="logger">The logger instance.</param>
    public GitHubOAuthClient(
        IHttpClientFactory httpClientFactory,
        IAccessTokenCachePort cache,
        ILogger<GitHubOAuthClient> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(logger);

        this._httpClientFactory = httpClientFactory;
        this._cache = cache;
        this._logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<InstallationAccessToken>> CreateInstallationTokenAsync(InstallationId installationId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(installationId);

        try
        {
            var cached = await this._cache.GetAsync(installationId, cancellationToken).ConfigureAwait(false);
            if (cached is not null && !cached.IsExpired(DateTimeOffset.UtcNow))
            {
                return Result<InstallationAccessToken>.Success(cached);
            }

            var client = this._httpClientFactory.CreateClient("GitHubOAuth");
            using var request = new HttpRequestMessage(HttpMethod.Post, $"app/installations/{installationId.Value}/access_tokens");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                LogMessages.InstallationTokenRequestFailed(this._logger, response.StatusCode, body);
                return Result<InstallationAccessToken>.Failure("github_installation_token_failed", $"GitHub returned {response.StatusCode}: {body}");
            }

            var payload = await response.Content.ReadFromJsonAsync<InstallationTokenResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (payload is null || string.IsNullOrWhiteSpace(payload.Token))
            {
                LogMessages.InstallationTokenInvalidResponse(this._logger);
                return Result<InstallationAccessToken>.Failure("github_installation_token_invalid", "GitHub response was empty.");
            }

            var token = InstallationAccessToken.Create(payload.Token, payload.ExpiresAt);
            await this._cache.SetAsync(installationId, token, cancellationToken).ConfigureAwait(false);
            return Result<InstallationAccessToken>.Success(token);
        }
        catch (JsonException ex)
        {
            LogMessages.InstallationTokenInvalidJson(this._logger, ex);
            return Result<InstallationAccessToken>.Failure("github_installation_token_invalid_json", ex.Message);
        }
        catch (Exception ex)
        {
            LogMessages.InstallationTokenUnexpectedError(this._logger, ex);
            return Result<InstallationAccessToken>.Failure("github_installation_token_exception", ex.Message);
        }
    }

    [SuppressMessage("Performance", "CA1812", Justification = "Instantiated via System.Text.Json serialization")]
    private sealed record InstallationTokenResponse(
        [property: JsonPropertyName("token")] string Token,
        [property: JsonPropertyName("expires_at")] DateTimeOffset ExpiresAt);
}
