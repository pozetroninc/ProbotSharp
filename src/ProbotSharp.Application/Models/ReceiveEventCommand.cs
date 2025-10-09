// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

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
