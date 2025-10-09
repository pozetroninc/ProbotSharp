// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

public sealed record class GetManifestCommand(
    string BaseUrl,
    string? AppName = null,
    string? Description = null,
    string? HomepageUrl = null,
    string? WebhookProxyUrl = null,
    bool IsPublic = true)
{
    public string BaseUrl { get; } = !string.IsNullOrWhiteSpace(BaseUrl)
        ? BaseUrl
        : throw new ArgumentException("Base URL cannot be null or whitespace.", nameof(BaseUrl));
}
