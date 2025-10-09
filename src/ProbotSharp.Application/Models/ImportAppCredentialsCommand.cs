// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

public sealed record class ImportAppCredentialsCommand(
    GitHubAppId AppId,
    PrivateKeyPem PrivateKey,
    string WebhookSecret);
