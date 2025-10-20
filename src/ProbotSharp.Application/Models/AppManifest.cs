// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Represents a GitHub App manifest for registration.
/// </summary>
/// <param name="Name">The app name.</param>
/// <param name="Url">The app homepage URL.</param>
/// <param name="HookAttributes">Webhook configuration attributes.</param>
/// <param name="RedirectUrl">OAuth redirect URL.</param>
/// <param name="SetupUrl">Setup wizard URL.</param>
/// <param name="Description">App description.</param>
/// <param name="PublicPermissions">Public API permissions.</param>
/// <param name="DefaultEvents">Default webhook events.</param>
public sealed record class AppManifest(
    string Name,
    Uri Url,
    string HookAttributes,
    Uri RedirectUrl,
    Uri SetupUrl,
    string Description,
    string[] PublicPermissions,
    string DefaultEvents);
