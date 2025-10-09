// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Entities;

namespace ProbotSharp.Domain.Specifications;

/// <summary>
/// Specification to determine if an Installation is active (has one or more repositories).
/// An installation is considered active when it has at least one repository associated with it.
/// </summary>
public sealed class ActiveInstallationSpecification : Specification<Installation>
{
    public override bool IsSatisfiedBy(Installation candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return candidate.Repositories.Count > 0;
    }
}
