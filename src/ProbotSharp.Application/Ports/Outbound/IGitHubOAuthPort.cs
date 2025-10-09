// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

public interface IGitHubOAuthPort
{
    Task<Result<InstallationAccessToken>> CreateInstallationTokenAsync(InstallationId installationId, CancellationToken cancellationToken = default);
}

