// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

#pragma warning disable CA1054 // URI parameters should be strings for JSON serialization compatibility
#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility

/// <summary>
/// Represents a command to complete the GitHub App setup wizard.
/// </summary>
/// <param name="Code">The authorization code received from GitHub.</param>
/// <param name="BaseUrl">The optional base URL for GitHub Enterprise.</param>
public sealed record class CompleteSetupCommand(
    string Code,
    string? BaseUrl = null);
#pragma warning restore CA1056
#pragma warning restore CA1054
