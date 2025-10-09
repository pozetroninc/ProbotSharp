// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

public sealed record class CreateInstallationTokenCommand(
    InstallationId InstallationId,
    string[]? Repositories = null,
    Dictionary<string, string>? Permissions = null);
