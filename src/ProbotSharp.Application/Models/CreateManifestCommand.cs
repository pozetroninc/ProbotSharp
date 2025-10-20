// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

#pragma warning disable CA1054 // URI parameters should be strings for JSON serialization compatibility
#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility

/// <summary>
/// Represents a command to create a GitHub App manifest.
/// </summary>
/// <param name="AppName">The name of the GitHub App.</param>
/// <param name="Description">The description of the GitHub App.</param>
/// <param name="BaseUrl">The base URL where the app is hosted.</param>
/// <param name="WebhookProxyUrl">The optional webhook proxy URL for development.</param>
/// <param name="Homepage">The optional homepage URL for the app.</param>
/// <param name="IsPublic">Whether the app is public.</param>
public sealed record class CreateManifestCommand(
    string AppName,
    string Description,
    string BaseUrl,
    string? WebhookProxyUrl = null,
    string? Homepage = null,
    bool IsPublic = true);
#pragma warning restore CA1056
#pragma warning restore CA1054
