// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Inbound;

public interface IInstallationAuthenticationPort
{
    Task<Result<InstallationAccessToken>> AuthenticateAsync(
        AuthenticateInstallationCommand command,
        CancellationToken cancellationToken = default);
}
