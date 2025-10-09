// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

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
