// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

#pragma warning disable CA1054 // URI parameters should be strings for JSON serialization compatibility
#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility

/// <summary>
/// Represents the response after creating a GitHub App from an authorization code.
/// </summary>
/// <param name="AppId">The GitHub App ID.</param>
/// <param name="ClientId">The GitHub App client ID.</param>
/// <param name="ClientSecret">The GitHub App client secret.</param>
/// <param name="WebhookSecret">The webhook secret for signature validation.</param>
/// <param name="PrivateKey">The private key for authentication.</param>
/// <param name="HtmlUrl">The HTML URL of the created GitHub App.</param>
public sealed record class CreateAppFromCodeResponse(
    GitHubAppId AppId,
    string ClientId,
    string ClientSecret,
    string WebhookSecret,
    PrivateKeyPem PrivateKey,
    string HtmlUrl);
#pragma warning restore CA1056
#pragma warning restore CA1054
