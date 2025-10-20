// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Specifications;

/// <summary>
/// Specification to determine if an Installation belongs to a specific GitHubApp.
/// Validates the relationship between an installation and its parent app.
/// </summary>
public sealed class InstallationBelongsToAppSpecification : Specification<Installation>
{
    private readonly GitHubApp _app;

    /// <summary>
    /// Initializes a new instance for the specified GitHub App.
    /// </summary>
    /// <param name="app">The GitHub App to validate against.</param>
    public InstallationBelongsToAppSpecification(GitHubApp app)
    {
        this._app = app ?? throw new ArgumentNullException(nameof(app));
    }

    /// <summary>
    /// Determines whether the specified installation belongs to the configured GitHub App.
    /// </summary>
    /// <param name="candidate">The installation to evaluate.</param>
    /// <returns>True if the installation belongs to the app; otherwise, false.</returns>
    public override bool IsSatisfiedBy(Installation candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return this._app.Installations.Any(i => i.Id.Equals(candidate.Id));
    }
}
