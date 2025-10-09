// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Entities;

namespace ProbotSharp.Domain.Specifications;

/// <summary>
/// Specification to determine if an Installation is inactive (has no repositories).
/// An installation is considered inactive when it has no repositories associated with it.
/// </summary>
public sealed class InactiveInstallationSpecification : Specification<Installation>
{
    public override bool IsSatisfiedBy(Installation candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return candidate.Repositories.Count == 0;
    }
}
