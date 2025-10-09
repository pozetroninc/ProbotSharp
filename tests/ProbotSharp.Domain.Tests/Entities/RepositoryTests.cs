// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Entities;

namespace ProbotSharp.Domain.Tests.Entities;

public class RepositoryTests
{
    [Fact]
    public void Create_WithValidArguments_ShouldInitializeProperties()
    {
        var repo = Repository.Create(1, "repo", "octocat/repo");

        repo.Name.Should().Be("repo");
        repo.FullName.Should().Be("octocat/repo");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidId_ShouldThrow(long id)
    {
        var act = () => Repository.Create(id, "repo", "octocat/repo");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Rename_WithValidArguments_ShouldUpdateProperties()
    {
        var repo = Repository.Create(1, "repo", "octocat/repo");

        repo.Rename("new-repo", "octocat/new-repo");

        repo.Name.Should().Be("new-repo");
        repo.FullName.Should().Be("octocat/new-repo");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrow(string name)
    {
        var act = () => Repository.Create(1, name!, "octocat/repo");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*name*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidFullName_ShouldThrow(string fullName)
    {
        var act = () => Repository.Create(1, "repo", fullName!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*full name*");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrim()
    {
        var repo = Repository.Create(1, "  repo  ", "  octocat/repo  ");

        repo.Name.Should().Be("repo");
        repo.FullName.Should().Be("octocat/repo");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithInvalidName_ShouldThrow(string name)
    {
        var repo = Repository.Create(1, "repo", "octocat/repo");

        var act = () => repo.Rename(name!, "octocat/repo");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*name*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithInvalidFullName_ShouldThrow(string fullName)
    {
        var repo = Repository.Create(1, "repo", "octocat/repo");

        var act = () => repo.Rename("repo", fullName!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*full name*");
    }

    [Fact]
    public void Rename_WithWhitespace_ShouldTrim()
    {
        var repo = Repository.Create(1, "repo", "octocat/repo");

        repo.Rename("  new-repo  ", "  octocat/new-repo  ");

        repo.Name.Should().Be("new-repo");
        repo.FullName.Should().Be("octocat/new-repo");
    }

    [Fact]
    public void Id_ShouldBeSetFromConstructor()
    {
        var repo = Repository.Create(12345, "repo", "octocat/repo");

        repo.Id.Should().Be(12345);
    }
}
