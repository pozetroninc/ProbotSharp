// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Security.Cryptography;

using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.Specifications;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Tests.Specifications;

public class InstallationBelongsToAppSpecificationTests
{
    private static PrivateKeyPem CreatePem()
    {
        using var rsa = RSA.Create(2048);
        var pem = rsa.ExportPkcs8PrivateKeyPem();
        return PrivateKeyPem.Create(pem);
    }

    [Fact]
    public void Constructor_WithNullApp_ShouldThrow()
    {
        var act = () => new InstallationBelongsToAppSpecification(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsSatisfiedBy_WithInstallationBelongingToApp_ShouldReturnTrue()
    {
        var app = GitHubApp.Create(
            GitHubAppId.Create(1),
            "TestApp",
            CreatePem(),
            "secret");
        var installation = app.AddInstallation(InstallationId.Create(100), "octocat");
        var spec = new InstallationBelongsToAppSpecification(app);

        spec.IsSatisfiedBy(installation).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_WithInstallationNotBelongingToApp_ShouldReturnFalse()
    {
        var app = GitHubApp.Create(
            GitHubAppId.Create(1),
            "TestApp",
            CreatePem(),
            "secret");
        var installation = Installation.Create(InstallationId.Create(999), "other-user");
        var spec = new InstallationBelongsToAppSpecification(app);

        spec.IsSatisfiedBy(installation).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_WithAppHavingMultipleInstallations_ShouldWorkCorrectly()
    {
        var app = GitHubApp.Create(
            GitHubAppId.Create(1),
            "TestApp",
            CreatePem(),
            "secret");
        var installation1 = app.AddInstallation(InstallationId.Create(100), "octocat");
        var installation2 = app.AddInstallation(InstallationId.Create(101), "monalisa");
        var installation3 = Installation.Create(InstallationId.Create(102), "other");
        var spec = new InstallationBelongsToAppSpecification(app);

        spec.IsSatisfiedBy(installation1).Should().BeTrue();
        spec.IsSatisfiedBy(installation2).Should().BeTrue();
        spec.IsSatisfiedBy(installation3).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_WithNullCandidate_ShouldThrow()
    {
        var app = GitHubApp.Create(
            GitHubAppId.Create(1),
            "TestApp",
            CreatePem(),
            "secret");
        var spec = new InstallationBelongsToAppSpecification(app);

        var act = () => spec.IsSatisfiedBy(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
