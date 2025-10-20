// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

/// <summary>
/// Represents information about a loaded ProbotSharp application.
/// </summary>
/// <param name="AppId">The GitHub App ID.</param>
/// <param name="AppName">The GitHub App name.</param>
/// <param name="LoadedAppPaths">The paths of loaded app modules.</param>
/// <param name="IsSetupMode">Indicates whether the app is in setup mode.</param>
public sealed record class AppInfo(
    GitHubAppId? AppId,
    string? AppName,
    string[] LoadedAppPaths,
    bool IsSetupMode);
