// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Entities;

namespace ProbotSharp.Domain.Specifications;

/// <summary>
/// Specification to determine if a Repository belongs to a specific Installation.
/// Validates the relationship between a repository and its parent installation.
/// </summary>
public sealed class RepositoryBelongsToInstallationSpecification : Specification<Repository>
{
    private readonly Installation _installation;

    /// <summary>
    /// Initializes a new instance for the specified installation.
    /// </summary>
    /// <param name="installation">The installation to validate against</param>
    public RepositoryBelongsToInstallationSpecification(Installation installation)
    {
        _installation = installation ?? throw new ArgumentNullException(nameof(installation));
    }

    public override bool IsSatisfiedBy(Repository candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return _installation.Repositories.Any(r => r.Id == candidate.Id);
    }
}
