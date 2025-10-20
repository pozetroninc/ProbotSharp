// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

#pragma warning disable CA1054 // URI parameters should be strings for JSON serialization compatibility
#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility

/// <summary>
/// Represents a command to start the webhook server.
/// </summary>
/// <param name="Host">The host to bind to.</param>
/// <param name="Port">The port to listen on.</param>
/// <param name="WebhookPath">The webhook endpoint path.</param>
/// <param name="WebhookProxy">The optional webhook proxy URL.</param>
/// <param name="AppId">The optional GitHub App ID.</param>
/// <param name="PrivateKey">The optional private key.</param>
/// <param name="WebhookSecret">The optional webhook secret.</param>
/// <param name="BaseUrl">The optional base URL for GitHub Enterprise.</param>
/// <param name="AppPaths">The optional paths to apps to load.</param>
public sealed record class StartServerCommand(
    string Host,
    int Port,
    string WebhookPath,
    string? WebhookProxy,
    GitHubAppId? AppId,
    PrivateKeyPem? PrivateKey,
    string? WebhookSecret,
    string? BaseUrl,
    string[]? AppPaths);
#pragma warning restore CA1056
#pragma warning restore CA1054
