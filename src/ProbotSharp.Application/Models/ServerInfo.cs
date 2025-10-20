// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

#pragma warning disable CA1054 // URI parameters should be strings for JSON serialization compatibility
#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility

/// <summary>
/// Represents information about the running server.
/// </summary>
/// <param name="Host">The host the server is bound to.</param>
/// <param name="Port">The port the server is listening on.</param>
/// <param name="WebhookPath">The webhook endpoint path.</param>
/// <param name="IsRunning">Whether the server is currently running.</param>
/// <param name="WebhookProxyUrl">The webhook proxy URL if configured.</param>
public sealed record class ServerInfo(
    string Host,
    int Port,
    string WebhookPath,
    bool IsRunning,
    string? WebhookProxyUrl);
#pragma warning restore CA1056
#pragma warning restore CA1054
