// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Represents the status of the GitHub App setup.
/// </summary>
/// <param name="IsConfigured">Whether the app is fully configured.</param>
/// <param name="HasAppId">Whether the app ID is configured.</param>
/// <param name="HasPrivateKey">Whether the private key is configured.</param>
/// <param name="HasWebhookSecret">Whether the webhook secret is configured.</param>
/// <param name="SetupUrl">The optional setup URL to complete configuration.</param>
public sealed record class SetupStatusResponse(
    bool IsConfigured,
    bool HasAppId,
    bool HasPrivateKey,
    bool HasWebhookSecret,
    Uri? SetupUrl = null);
