// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Represents a command to run the GitHub App setup wizard.
/// </summary>
/// <param name="Host">The optional host to use for the wizard.</param>
/// <param name="Port">The optional port to use for the wizard.</param>
/// <param name="GitHubEnterpriseHost">The optional GitHub Enterprise host.</param>
/// <param name="GitHubOrganization">The optional GitHub organization.</param>
/// <param name="NoSmeeSetup">Whether to skip Smee.io setup.</param>
public sealed record class SetupCommand(
    string? Host = null,
    int? Port = null,
    string? GitHubEnterpriseHost = null,
    string? GitHubOrganization = null,
    bool NoSmeeSetup = false);
