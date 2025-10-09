// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

public sealed record class RunAppLifecycleCommand(
    GitHubAppId AppId,
    PrivateKeyPem PrivateKey,
    string WebhookSecret,
    string? BaseUrl = null,
    int Port = 3000,
    string Host = "localhost");
