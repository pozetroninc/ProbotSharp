// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

#pragma warning disable CA1054 // URI parameters should be strings for JSON serialization compatibility
#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility

/// <summary>
/// Represents a command to receive and process a webhook event with full configuration.
/// </summary>
/// <param name="EventName">The webhook event name.</param>
/// <param name="PayloadPath">The path to the payload file.</param>
/// <param name="AppPath">The path to the app to load.</param>
/// <param name="Token">The optional GitHub token.</param>
/// <param name="AppId">The optional GitHub App ID.</param>
/// <param name="PrivateKey">The optional private key.</param>
/// <param name="BaseUrl">The optional base URL for GitHub Enterprise.</param>
/// <param name="LogLevel">The log level.</param>
/// <param name="LogFormat">The log format.</param>
/// <param name="LogLevelInString">Whether to include log level in string format.</param>
/// <param name="LogMessageKey">The log message key.</param>
/// <param name="SentryDsn">The optional Sentry DSN for error tracking.</param>
public sealed record class ReceiveEventCommand(
    WebhookEventName EventName,
    string PayloadPath,
    string AppPath,
    string? Token = null,
    GitHubAppId? AppId = null,
    PrivateKeyPem? PrivateKey = null,
    string? BaseUrl = null,
    string? LogLevel = "info",
    string? LogFormat = "pretty",
    bool LogLevelInString = false,
    string? LogMessageKey = "msg",
    string? SentryDsn = null);
#pragma warning restore CA1056
#pragma warning restore CA1054
