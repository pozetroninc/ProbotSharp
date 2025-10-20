// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

#pragma warning disable CA1054 // URI parameters should be strings for JSON serialization compatibility
#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility

/// <summary>
/// Represents a command to start a ProbotSharp application.
/// </summary>
/// <param name="AppId">The GitHub App ID.</param>
/// <param name="PrivateKey">The private key for authentication.</param>
/// <param name="WebhookSecret">The optional webhook secret.</param>
/// <param name="Host">The optional host to bind to.</param>
/// <param name="Port">The optional port to listen on.</param>
/// <param name="WebhookPath">The optional webhook endpoint path.</param>
/// <param name="WebhookProxyUrl">The optional webhook proxy URL.</param>
/// <param name="BaseUrl">The optional base URL for GitHub Enterprise.</param>
/// <param name="RedisConfig">The optional Redis configuration.</param>
public sealed record class StartAppCommand(
    GitHubAppId AppId,
    PrivateKeyPem PrivateKey,
    string? WebhookSecret = null,
    string? Host = null,
    int? Port = null,
    string? WebhookPath = null,
    string? WebhookProxyUrl = null,
    string? BaseUrl = null,
    string? RedisConfig = null);
#pragma warning restore CA1056
#pragma warning restore CA1054
