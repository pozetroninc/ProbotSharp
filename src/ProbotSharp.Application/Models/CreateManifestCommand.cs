// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

public sealed record class CreateManifestCommand(
    string AppName,
    string Description,
    string BaseUrl,
    string? WebhookProxyUrl = null,
    string? Homepage = null,
    bool IsPublic = true);
