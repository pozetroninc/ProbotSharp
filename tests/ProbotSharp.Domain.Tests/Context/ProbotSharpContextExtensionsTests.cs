// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Octokit;
using ProbotSharp.Domain.Context;
using ProbotSharp.Domain.Contracts;

namespace ProbotSharp.Domain.Tests.Context;

public class ProbotSharpContextExtensionsTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();
    private readonly IGitHubClient _gitHub = Substitute.For<IGitHubClient>();
    private readonly IGitHubGraphQlClient _graphQL = Substitute.For<IGitHubGraphQlClient>();

    [Fact]
    public void Issue_WithValidPayload_ShouldReturnOwnerRepoNumber()
    {
        // Arrange
        var payload = JObject.Parse(@"{
            ""issue"": {
                ""number"": 42
            }
        }");

        var repository = new RepositoryInfo(123, "test-repo", "test-owner", "test-owner/test-repo");
        var context = new ProbotSharpContext(
            "delivery-1",
            "issues",
            "opened",
            payload,
            _logger,
            _gitHub,
            _graphQL,
            repository,
            null);

        // Act
        var (owner, repo, number) = context.Issue();

        // Assert
        owner.Should().Be("test-owner");
        repo.Should().Be("test-repo");
        number.Should().Be(42);
    }

    [Fact]
    public void Issue_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        ProbotSharpContext? context = null;

        // Act
        var act = () => context!.Issue();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Issue_WithNullRepository_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = JObject.Parse(@"{""issue"":{""number"":42}}");
        var context = new ProbotSharpContext(
            "delivery-1",
            "issues",
            "opened",
            payload,
            _logger,
            _gitHub,
            _graphQL,
            null, // No repository
            null);

        // Act
        var act = () => context.Issue();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Repository owner not found*");
    }

    [Fact]
    public void Issue_WithMissingIssueNumber_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = JObject.Parse(@"{}"); // No issue object
        var repository = new RepositoryInfo(123, "test-repo", "test-owner", "test-owner/test-repo");
        var context = new ProbotSharpContext(
            "delivery-1",
            "issues",
            "opened",
            payload,
            _logger,
            _gitHub,
            _graphQL,
            repository,
            null);

        // Act
        var act = () => context.Issue();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Issue number not found*");
    }

    [Fact]
    public void Repo_WithValidPayload_ShouldReturnOwnerRepo()
    {
        // Arrange
        var payload = JObject.Parse(@"{}");
        var repository = new RepositoryInfo(123, "my-repo", "my-owner", "my-owner/my-repo");
        var context = new ProbotSharpContext(
            "delivery-1",
            "push",
            null,
            payload,
            _logger,
            _gitHub,
            _graphQL,
            repository,
            null);

        // Act
        var (owner, repo) = context.Repo();

        // Assert
        owner.Should().Be("my-owner");
        repo.Should().Be("my-repo");
    }

    [Fact]
    public void Repo_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        ProbotSharpContext? context = null;

        // Act
        var act = () => context!.Repo();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Repo_WithNullRepository_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = JObject.Parse(@"{}");
        var context = new ProbotSharpContext(
            "delivery-1",
            "push",
            null,
            payload,
            _logger,
            _gitHub,
            _graphQL,
            null, // No repository
            null);

        // Act
        var act = () => context.Repo();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Repository owner not found*");
    }

    [Fact]
    public void PullRequest_WithValidPayload_ShouldReturnOwnerRepoNumber()
    {
        // Arrange
        var payload = JObject.Parse(@"{
            ""pull_request"": {
                ""number"": 99
            }
        }");

        var repository = new RepositoryInfo(123, "test-repo", "test-owner", "test-owner/test-repo");
        var context = new ProbotSharpContext(
            "delivery-1",
            "pull_request",
            "opened",
            payload,
            _logger,
            _gitHub,
            _graphQL,
            repository,
            null);

        // Act
        var (owner, repo, number) = context.PullRequest();

        // Assert
        owner.Should().Be("test-owner");
        repo.Should().Be("test-repo");
        number.Should().Be(99);
    }

    [Fact]
    public void PullRequest_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        ProbotSharpContext? context = null;

        // Act
        var act = () => context!.PullRequest();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void PullRequest_WithNullRepository_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = JObject.Parse(@"{""pull_request"":{""number"":99}}");
        var context = new ProbotSharpContext(
            "delivery-1",
            "pull_request",
            "opened",
            payload,
            _logger,
            _gitHub,
            _graphQL,
            null, // No repository
            null);

        // Act
        var act = () => context.PullRequest();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Repository owner not found*");
    }

    [Fact]
    public void PullRequest_WithMissingPullRequestNumber_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = JObject.Parse(@"{}"); // No pull_request object
        var repository = new RepositoryInfo(123, "test-repo", "test-owner", "test-owner/test-repo");
        var context = new ProbotSharpContext(
            "delivery-1",
            "pull_request",
            "opened",
            payload,
            _logger,
            _gitHub,
            _graphQL,
            repository,
            null);

        // Act
        var act = () => context.PullRequest();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Pull request number not found*");
    }

    [Fact]
    public void Issue_WithDifferentIssueNumbers_ShouldReturnCorrectValue()
    {
        // Arrange
        var repository = new RepositoryInfo(123, "test-repo", "test-owner", "test-owner/test-repo");

        var payload1 = JObject.Parse(@"{""issue"":{""number"":1}}");
        var context1 = new ProbotSharpContext("d-1", "issues", "opened", payload1, _logger, _gitHub, _graphQL, repository, null);

        var payload2 = JObject.Parse(@"{""issue"":{""number"":9999}}");
        var context2 = new ProbotSharpContext("d-2", "issues", "opened", payload2, _logger, _gitHub, _graphQL, repository, null);

        // Act
        var (_, _, number1) = context1.Issue();
        var (_, _, number2) = context2.Issue();

        // Assert
        number1.Should().Be(1);
        number2.Should().Be(9999);
    }

    [Fact]
    public void Repo_WithDifferentOwners_ShouldReturnCorrectValue()
    {
        // Arrange
        var payload = JObject.Parse(@"{}");

        var repo1 = new RepositoryInfo(1, "repo1", "owner1", "owner1/repo1");
        var context1 = new ProbotSharpContext("d-1", "push", null, payload, _logger, _gitHub, _graphQL, repo1, null);

        var repo2 = new RepositoryInfo(2, "repo2", "owner2", "owner2/repo2");
        var context2 = new ProbotSharpContext("d-2", "push", null, payload, _logger, _gitHub, _graphQL, repo2, null);

        // Act
        var (owner1, repoName1) = context1.Repo();
        var (owner2, repoName2) = context2.Repo();

        // Assert
        owner1.Should().Be("owner1");
        repoName1.Should().Be("repo1");
        owner2.Should().Be("owner2");
        repoName2.Should().Be("repo2");
    }
}
