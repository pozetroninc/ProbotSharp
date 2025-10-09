// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

public sealed record class AppManifest(
    string Name,
    Uri Url,
    string HookAttributes,
    Uri RedirectUrl,
    Uri SetupUrl,
    string Description,
    string[] PublicPermissions,
    string DefaultEvents);
