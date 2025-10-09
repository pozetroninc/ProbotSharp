// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.Specifications;

namespace ProbotSharp.Domain.Tests.Specifications;

public class InactiveInstallationSpecificationTests
{
    [Fact]
    public void IsSatisfiedBy_WithNoRepositories_ShouldReturnTrue()
    {
        var installation = Installation.Create(InstallationId.Create(1), "octocat");
        var spec = new InactiveInstallationSpecification();

        spec.IsSatisfiedBy(installation).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_WithRepositories_ShouldReturnFalse()
    {
        var installation = Installation.Create(InstallationId.Create(1), "octocat");
        installation.AddRepository(100, "repo", "octocat/repo");
        var spec = new InactiveInstallationSpecification();

        spec.IsSatisfiedBy(installation).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_WithNullCandidate_ShouldThrow()
    {
        var spec = new InactiveInstallationSpecification();

        var act = () => spec.IsSatisfiedBy(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
