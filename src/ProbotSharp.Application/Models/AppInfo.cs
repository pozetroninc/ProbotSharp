// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

public sealed record class AppInfo(
    GitHubAppId? AppId,
    string? AppName,
    string[] LoadedAppPaths,
    bool IsSetupMode);
