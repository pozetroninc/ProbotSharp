// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.Specifications;

namespace ProbotSharp.Domain.Tests.Specifications;

public class RepositoryBelongsToInstallationSpecificationTests
{
    [Fact]
    public void Constructor_WithNullInstallation_ShouldThrow()
    {
        var act = () => new RepositoryBelongsToInstallationSpecification(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsSatisfiedBy_WithRepositoryBelongingToInstallation_ShouldReturnTrue()
    {
        var installation = Installation.Create(InstallationId.Create(1), "octocat");
        var repository = installation.AddRepository(100, "repo", "octocat/repo");
        var spec = new RepositoryBelongsToInstallationSpecification(installation);

        spec.IsSatisfiedBy(repository).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_WithRepositoryNotBelongingToInstallation_ShouldReturnFalse()
    {
        var installation = Installation.Create(InstallationId.Create(1), "octocat");
        var repository = Repository.Create(999, "other-repo", "other/repo");
        var spec = new RepositoryBelongsToInstallationSpecification(installation);

        spec.IsSatisfiedBy(repository).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_WithInstallationHavingMultipleRepositories_ShouldWorkCorrectly()
    {
        var installation = Installation.Create(InstallationId.Create(1), "octocat");
        var repo1 = installation.AddRepository(100, "repo1", "octocat/repo1");
        var repo2 = installation.AddRepository(101, "repo2", "octocat/repo2");
        var repo3 = Repository.Create(102, "repo3", "other/repo3");
        var spec = new RepositoryBelongsToInstallationSpecification(installation);

        spec.IsSatisfiedBy(repo1).Should().BeTrue();
        spec.IsSatisfiedBy(repo2).Should().BeTrue();
        spec.IsSatisfiedBy(repo3).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_WithNullCandidate_ShouldThrow()
    {
        var installation = Installation.Create(InstallationId.Create(1), "octocat");
        var spec = new RepositoryBelongsToInstallationSpecification(installation);

        var act = () => spec.IsSatisfiedBy(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
