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
    /// <summary>
    /// Determines whether the specified installation is inactive.
    /// </summary>
    /// <param name="candidate">The installation to evaluate.</param>
    /// <returns>True if the installation has no repositories; otherwise, false.</returns>
    public override bool IsSatisfiedBy(Installation candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return candidate.Repositories.Count == 0;
    }
}
