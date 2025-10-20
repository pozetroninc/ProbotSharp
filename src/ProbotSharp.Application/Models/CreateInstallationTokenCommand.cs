// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

/// <summary>
/// Represents a command to create an installation access token.
/// </summary>
/// <param name="InstallationId">The installation ID to authenticate.</param>
/// <param name="Repositories">The optional list of repositories to scope the token to.</param>
/// <param name="Permissions">The optional permissions to request.</param>
public sealed record class CreateInstallationTokenCommand(
    InstallationId InstallationId,
    string[]? Repositories = null,
    Dictionary<string, string>? Permissions = null);
