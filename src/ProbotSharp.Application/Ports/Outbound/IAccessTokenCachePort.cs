// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Ports.Outbound;

public interface IAccessTokenCachePort
{
    Task<InstallationAccessToken?> GetAsync(InstallationId installationId, CancellationToken cancellationToken = default);

    Task SetAsync(InstallationId installationId, InstallationAccessToken token, CancellationToken cancellationToken = default);
}

