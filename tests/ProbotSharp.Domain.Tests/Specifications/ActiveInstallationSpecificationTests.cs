// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.Specifications;

namespace ProbotSharp.Domain.Tests.Specifications;

public class ActiveInstallationSpecificationTests
{
    [Fact]
    public void IsSatisfiedBy_WithNoRepositories_ShouldReturnFalse()
    {
        var installation = Installation.Create(InstallationId.Create(1), "octocat");
        var spec = new ActiveInstallationSpecification();

        spec.IsSatisfiedBy(installation).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_WithOneRepository_ShouldReturnTrue()
    {
        var installation = Installation.Create(InstallationId.Create(1), "octocat");
        installation.AddRepository(100, "repo", "octocat/repo");
        var spec = new ActiveInstallationSpecification();

        spec.IsSatisfiedBy(installation).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_WithMultipleRepositories_ShouldReturnTrue()
    {
        var installation = Installation.Create(InstallationId.Create(1), "octocat");
        installation.AddRepository(100, "repo1", "octocat/repo1");
        installation.AddRepository(101, "repo2", "octocat/repo2");
        installation.AddRepository(102, "repo3", "octocat/repo3");
        var spec = new ActiveInstallationSpecification();

        spec.IsSatisfiedBy(installation).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_WithNullCandidate_ShouldThrow()
    {
        var spec = new ActiveInstallationSpecification();

        var act = () => spec.IsSatisfiedBy(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
