// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

/// <summary>
/// Represents the response after creating an installation access token.
/// </summary>
/// <param name="Token">The generated installation access token.</param>
/// <param name="ExpiresAt">The expiration time of the token.</param>
/// <param name="Repositories">The repositories the token has access to.</param>
/// <param name="Permissions">The permissions granted to the token.</param>
public sealed record class CreateInstallationTokenResponse(
    InstallationAccessToken Token,
    DateTimeOffset ExpiresAt,
    string[]? Repositories = null,
    Dictionary<string, string>? Permissions = null);
