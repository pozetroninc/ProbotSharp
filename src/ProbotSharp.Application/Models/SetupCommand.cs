// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

public sealed record class SetupCommand(
    string? Host = null,
    int? Port = null,
    string? GitHubEnterpriseHost = null,
    string? GitHubOrganization = null,
    bool NoSmeeSetup = false);
