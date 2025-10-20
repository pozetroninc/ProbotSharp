// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

#pragma warning disable CA1054 // URI parameters should be strings for JSON serialization compatibility
#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility

/// <summary>
/// Represents a command to run a ProbotSharp application.
/// </summary>
/// <param name="AppPath">The optional path to the app to load.</param>
/// <param name="AppId">The optional GitHub App ID.</param>
/// <param name="PrivateKey">The optional private key.</param>
/// <param name="WebhookSecret">The optional webhook secret.</param>
/// <param name="Host">The optional host to bind to.</param>
/// <param name="Port">The optional port to listen on.</param>
/// <param name="WebhookPath">The optional webhook endpoint path.</param>
/// <param name="WebhookProxyUrl">The optional webhook proxy URL.</param>
/// <param name="BaseUrl">The optional base URL for GitHub Enterprise.</param>
/// <param name="RedisConfig">The optional Redis configuration.</param>
/// <param name="LogLevel">The optional log level.</param>
/// <param name="LogFormat">The optional log format.</param>
/// <param name="SentryDsn">The optional Sentry DSN for error tracking.</param>
public sealed record class RunCommand(
    string? AppPath = null,
    GitHubAppId? AppId = null,
    PrivateKeyPem? PrivateKey = null,
    string? WebhookSecret = null,
    string? Host = null,
    int? Port = null,
    string? WebhookPath = null,
    string? WebhookProxyUrl = null,
    string? BaseUrl = null,
    string? RedisConfig = null,
    string? LogLevel = null,
    string? LogFormat = null,
    string? SentryDsn = null);
#pragma warning restore CA1056
#pragma warning restore CA1054
