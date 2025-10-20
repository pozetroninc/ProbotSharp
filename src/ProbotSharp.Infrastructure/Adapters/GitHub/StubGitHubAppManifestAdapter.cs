// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

#pragma warning disable CA1848 // Performance: LoggerMessage delegates - not performance-critical for this codebase

namespace ProbotSharp.Infrastructure.Adapters.GitHub;

/// <summary>
/// Development-only GitHub App manifest adapter that simulates the OAuth exchange locally.
/// </summary>
public sealed class StubGitHubAppManifestAdapter : IGitHubAppManifestPort
{
    private readonly ILogger<StubGitHubAppManifestAdapter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubGitHubAppManifestAdapter"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public StubGitHubAppManifestAdapter(ILogger<StubGitHubAppManifestAdapter> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc />
    public Task<Result<CreateAppFromCodeResponse>> CreateAppFromCodeAsync(string code, string? baseUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Task.FromResult(Result<CreateAppFromCodeResponse>.Failure(
                "code_required",
                "OAuth code must be provided."));
        }

        try
        {
            var appId = GitHubAppId.Create(Math.Abs(code.GetHashCode()) + 1000);
            var clientId = $"Iv{Math.Abs(code.GetHashCode()):X}";
            var clientSecret = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(code)))[..32];
            var webhookSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            var privateKey = PrivateKeyPem.Create(GeneratePrivateKeyPem());

            var htmlUrlBase = string.IsNullOrWhiteSpace(baseUrl)
                ? "https://github.com/apps"
                : baseUrl.Trim().TrimEnd('/') + "/settings/apps";

            var response = new CreateAppFromCodeResponse(
                appId,
                clientId,
                clientSecret,
                webhookSecret,
                privateKey,
                $"{htmlUrlBase}/probot-sharp-{appId.Value}");

            this._logger.LogInformation("Generated development GitHub App credentials for code {Code}", code);
            return Task.FromResult(Result<CreateAppFromCodeResponse>.Success(response));
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to generate development GitHub App credentials");
            return Task.FromResult(Result<CreateAppFromCodeResponse>.Failure(
                "manifest_stub_failure",
                ex.Message));
        }
    }

    private static string GeneratePrivateKeyPem()
    {
        using var rsa = RSA.Create(2048);
        var pkcs8 = rsa.ExportPkcs8PrivateKey();
        var base64 = Convert.ToBase64String(pkcs8);

        var builder = new StringBuilder();
        builder.AppendLine("-----BEGIN PRIVATE KEY-----");

        for (var offset = 0; offset < base64.Length; offset += 64)
        {
            var length = Math.Min(64, base64.Length - offset);
            builder.AppendLine(base64.Substring(offset, length));
        }

        builder.Append("-----END PRIVATE KEY-----");
        return builder.ToString();
    }
}

#pragma warning restore CA1848
