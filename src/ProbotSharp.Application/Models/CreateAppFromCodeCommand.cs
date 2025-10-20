// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Represents a command to create a GitHub App from an authorization code.
/// </summary>
/// <param name="Code">The authorization code from GitHub.</param>
/// <param name="GitHubEnterpriseHost">The optional GitHub Enterprise host.</param>
public sealed record class CreateAppFromCodeCommand(
    string Code,
    string? GitHubEnterpriseHost = null)
{
    /// <summary>
    /// Gets the authorization code from GitHub.
    /// </summary>
    public string Code { get; } = !string.IsNullOrWhiteSpace(Code)
        ? Code
        : throw new ArgumentException("Code cannot be null or whitespace.", nameof(Code));
}
