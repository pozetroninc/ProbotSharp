// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

#pragma warning disable CA1054 // URI parameters should be strings for JSON serialization compatibility
#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility
/// <summary>
/// Represents the response when an app starts successfully.
/// </summary>
/// <param name="AppId">The GitHub App ID.</param>
/// <param name="Host">The server host.</param>
/// <param name="Port">The server port.</param>
/// <param name="WebhookPath">The webhook endpoint path.</param>
/// <param name="WebhookProxyUrl">The webhook proxy URL if configured.</param>
/// <param name="StartedAt">The timestamp when the app started.</param>
public sealed record class AppStartedResponse(
    GitHubAppId AppId,
    string Host,
    int Port,
    string WebhookPath,
    string? WebhookProxyUrl,
    DateTimeOffset StartedAt);
#pragma warning restore CA1056
#pragma warning restore CA1054
