// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

public sealed record class CreateInstallationTokenResponse(
    InstallationAccessToken Token,
    DateTimeOffset ExpiresAt,
    string[]? Repositories = null,
    Dictionary<string, string>? Permissions = null);
