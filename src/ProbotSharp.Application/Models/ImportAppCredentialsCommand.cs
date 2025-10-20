// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

/// <summary>
/// Represents a command to import GitHub App credentials.
/// </summary>
/// <param name="AppId">The GitHub App ID.</param>
/// <param name="PrivateKey">The private key for authentication.</param>
/// <param name="WebhookSecret">The webhook secret for signature validation.</param>
public sealed record class ImportAppCredentialsCommand(
    GitHubAppId AppId,
    PrivateKeyPem PrivateKey,
    string WebhookSecret);
