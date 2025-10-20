// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

/// <summary>
/// Command to authenticate a GitHub App installation and obtain an access token.
/// </summary>
/// <param name="InstallationId">The installation ID to authenticate.</param>
public sealed record class AuthenticateInstallationCommand(
    InstallationId InstallationId);
