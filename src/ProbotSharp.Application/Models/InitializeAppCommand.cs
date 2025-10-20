// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

/// <summary>
/// Command to initialize a GitHub App with required configuration.
/// This is used during the application bootstrap phase to load and validate app credentials.
/// </summary>
#pragma warning disable CA1054 // URI parameters should be strings for JSON serialization compatibility
#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility
public sealed record class InitializeAppCommand(
    GitHubAppId AppId,
    PrivateKeyPem PrivateKey,
    string? WebhookSecret = null,
    string? BaseUrl = null,
    string? GitHubEnterpriseUrl = null,
    Dictionary<string, string>? AdditionalSettings = null);
#pragma warning restore CA1056
#pragma warning restore CA1054
