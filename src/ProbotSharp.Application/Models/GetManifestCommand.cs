// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

#pragma warning disable CA1054 // URI parameters should be strings for JSON serialization compatibility
#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility

/// <summary>
/// Represents a command to get a GitHub App manifest.
/// </summary>
/// <param name="BaseUrl">The base URL where the app is hosted.</param>
/// <param name="AppName">The optional name of the GitHub App.</param>
/// <param name="Description">The optional description of the GitHub App.</param>
/// <param name="HomepageUrl">The optional homepage URL.</param>
/// <param name="WebhookProxyUrl">The optional webhook proxy URL.</param>
/// <param name="IsPublic">Whether the app is public.</param>
public sealed record class GetManifestCommand(
    string BaseUrl,
    string? AppName = null,
    string? Description = null,
    string? HomepageUrl = null,
    string? WebhookProxyUrl = null,
    bool IsPublic = true)
{
    /// <summary>
    /// Gets the base URL where the app is hosted.
    /// </summary>
    public string BaseUrl { get; } = !string.IsNullOrWhiteSpace(BaseUrl)
        ? BaseUrl
        : throw new ArgumentException("Base URL cannot be null or whitespace.", nameof(BaseUrl));
}
#pragma warning restore CA1056
#pragma warning restore CA1054
