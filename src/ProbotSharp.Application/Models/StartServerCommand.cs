// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

public sealed record class StartServerCommand(
    string Host,
    int Port,
    string WebhookPath,
    string? WebhookProxy,
    GitHubAppId? AppId,
    PrivateKeyPem? PrivateKey,
    string? WebhookSecret,
    string? BaseUrl,
    string[]? AppPaths);
