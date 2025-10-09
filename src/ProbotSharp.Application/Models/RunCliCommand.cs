// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

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
