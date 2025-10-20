// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Represents the response containing a GitHub App manifest.
/// </summary>
/// <param name="ManifestJson">The JSON representation of the manifest.</param>
/// <param name="CreateAppUrl">The URL to create the app on GitHub.</param>
public sealed record class GetManifestResponse(
    string ManifestJson,
    Uri CreateAppUrl);
