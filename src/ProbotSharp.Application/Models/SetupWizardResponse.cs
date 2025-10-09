// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

/// <summary>
/// Response containing the result of the setup wizard process.
/// Includes the created app credentials and configuration details.
/// </summary>
public sealed record class SetupWizardResponse(
    bool IsSuccessful,
    GitHubAppId? AppId,
    string? ClientId,
    string? ClientSecret,
    string? WebhookSecret,
    PrivateKeyPem? PrivateKey,
    Uri? AppUrl,
    Uri? SetupUrl,
    string? ErrorMessage = null,
    Dictionary<string, string>? Configuration = null);
