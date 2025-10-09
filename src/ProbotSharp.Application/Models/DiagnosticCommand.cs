// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Command to run diagnostic checks on the ProbotSharp application.
/// Validates configuration, connectivity, and system health.
/// </summary>
public sealed record class DiagnosticCommand(
    bool CheckGitHubConnectivity = true,
    bool CheckWebhookConfiguration = true,
    bool CheckCredentials = true,
    bool CheckDependencies = true,
    bool Verbose = false,
    string? OutputFormat = "text");
