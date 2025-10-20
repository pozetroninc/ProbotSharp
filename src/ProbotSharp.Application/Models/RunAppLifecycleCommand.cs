// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

#pragma warning disable CA1054 // URI parameters should be strings for JSON serialization compatibility
#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility

/// <summary>
/// Represents a command to run the app lifecycle with GitHub App credentials.
/// </summary>
/// <param name="AppId">The GitHub App ID.</param>
/// <param name="PrivateKey">The private key for authentication.</param>
/// <param name="WebhookSecret">The webhook secret for signature validation.</param>
/// <param name="BaseUrl">The optional base URL for GitHub Enterprise.</param>
/// <param name="Port">The port to listen on.</param>
/// <param name="Host">The host to bind to.</param>
public sealed record class RunAppLifecycleCommand(
    GitHubAppId AppId,
    PrivateKeyPem PrivateKey,
    string WebhookSecret,
    string? BaseUrl = null,
    int Port = 3000,
    string Host = "localhost");
#pragma warning restore CA1056
#pragma warning restore CA1054
