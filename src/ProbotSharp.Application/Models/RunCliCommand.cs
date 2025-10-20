// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

#pragma warning disable CA1054 // URI parameters should be strings for JSON serialization compatibility
#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility

/// <summary>
/// Represents a command to run the CLI with full configuration options.
/// </summary>
/// <param name="AppPaths">The paths to the apps to load.</param>
/// <param name="Port">The port to listen on.</param>
/// <param name="Host">The host to bind to.</param>
/// <param name="WebhookPath">The webhook endpoint path.</param>
/// <param name="WebhookProxy">The webhook proxy URL.</param>
/// <param name="AppId">The optional GitHub App ID.</param>
/// <param name="PrivateKey">The optional private key.</param>
/// <param name="Secret">The optional webhook secret.</param>
/// <param name="BaseUrl">The optional base URL for GitHub Enterprise.</param>
/// <param name="RedisUrl">The optional Redis URL.</param>
/// <param name="LogLevel">The log level.</param>
/// <param name="LogFormat">The log format.</param>
/// <param name="LogLevelInString">Whether to include log level in string format.</param>
/// <param name="LogMessageKey">The log message key.</param>
/// <param name="SentryDsn">The optional Sentry DSN for error tracking.</param>
public sealed record class RunCliCommand(
    string[] AppPaths,
    int Port = 3000,
    string Host = "localhost",
    string? WebhookPath = null,
    string? WebhookProxy = null,
    GitHubAppId? AppId = null,
    PrivateKeyPem? PrivateKey = null,
    string? Secret = null,
    string? BaseUrl = null,
    string? RedisUrl = null,
    string? LogLevel = "info",
    string? LogFormat = "pretty",
    bool LogLevelInString = false,
    string? LogMessageKey = "msg",
    string? SentryDsn = null);
#pragma warning restore CA1056
#pragma warning restore CA1054
