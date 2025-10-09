// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

public sealed record class AppStartedResponse(
    GitHubAppId AppId,
    string Host,
    int Port,
    string WebhookPath,
    string? WebhookProxyUrl,
    DateTimeOffset StartedAt);
