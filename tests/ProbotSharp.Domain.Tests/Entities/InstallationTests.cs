// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Tests.Entities;

public class InstallationTests
{
    [Fact]
    public void Create_WithValidArguments_ShouldSetProperties()
    {
        var installation = Installation.Create(InstallationId.Create(1), "octocat");

        installation.AccountLogin.Should().Be("octocat");
    }

    [Fact]
    public void UpdateAccountLogin_WithValidValue_ShouldUpdate()
    {
        var installation = Installation.Create(InstallationId.Create(1), "octocat");

        installation.UpdateAccountLogin("monalisa");

        installation.AccountLogin.Should().Be("monalisa");
    }

    [Fact]
    public void AddRepository_WhenNew_ShouldAddToCollection()
    {
        var installation = Installation.Create(InstallationId.Create(1), "octocat");

        var repo = installation.AddRepository(100, "repo", "octocat/repo");

        installation.Repositories.Should().Contain(repo);
    }

    [Fact]
    public void AddRepository_WhenDuplicate_ShouldThrow()
    {
        var installation = Installation.Create(InstallationId.Create(1), "octocat");
        installation.AddRepository(100, "repo", "octocat/repo");

        var act = () => installation.AddRepository(100, "repo", "octocat/repo");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RemoveRepository_WhenExists_ShouldRemove()
    {
        var installation = Installation.Create(InstallationId.Create(1), "octocat");
        var repo = installation.AddRepository(100, "repo", "octocat/repo");

        installation.RemoveRepository(repo.Id);

        installation.Repositories.Should().BeEmpty();
    }

    [Fact]
    public void RemoveRepository_WhenMissing_ShouldThrow()
    {
        var installation = Installation.Create(InstallationId.Create(1), "octocat");

        var act = () => installation.RemoveRepository(100);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidAccountLogin_ShouldThrow(string accountLogin)
    {
        var act = () => Installation.Create(InstallationId.Create(1), accountLogin!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*account login*");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrim()
    {
        var installation = Installation.Create(InstallationId.Create(1), "  octocat  ");

        installation.AccountLogin.Should().Be("octocat");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateAccountLogin_WithInvalidValue_ShouldThrow(string accountLogin)
    {
        var installation = Installation.Create(InstallationId.Create(1), "octocat");

        var act = () => installation.UpdateAccountLogin(accountLogin!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*account login*");
    }

    [Fact]
    public void UpdateAccountLogin_WithWhitespace_ShouldTrim()
    {
        var installation = Installation.Create(InstallationId.Create(1), "octocat");

        installation.UpdateAccountLogin("  monalisa  ");

        installation.AccountLogin.Should().Be("monalisa");
    }

    [Fact]
    public void Repositories_ShouldBeReadOnly()
    {
        var installation = Installation.Create(InstallationId.Create(1), "octocat");

        installation.Repositories.Should().BeAssignableTo<IReadOnlyCollection<Repository>>();
    }

    [Fact]
    public void Id_ShouldBeSetFromConstructor()
    {
        var installationId = InstallationId.Create(12345);

        var installation = Installation.Create(installationId, "octocat");

        installation.Id.Should().Be(installationId);
    }
}
