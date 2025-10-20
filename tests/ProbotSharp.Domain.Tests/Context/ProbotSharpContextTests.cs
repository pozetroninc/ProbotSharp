// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Linq;

using NSubstitute;

using Octokit;

using ProbotSharp.Domain.Context;
using ProbotSharp.Domain.Contracts;

namespace ProbotSharp.Domain.Tests.Context;

public class ProbotSharpContextTests
{
    [Fact]
    public void Constructor_WithValidArguments_ShouldSetProperties()
    {
        // Arrange
        var id = "delivery-123";
        var eventName = "issues";
        var eventAction = "opened";
        var payload = JObject.Parse("{\"action\":\"opened\",\"issue\":{\"number\":1}}");
        var logger = Substitute.For<ILogger>();
        var gitHub = Substitute.For<IGitHubClient>();
        var graphQL = Substitute.For<IGitHubGraphQlClient>();
        var repository = new RepositoryInfo(123, "test-repo", "test-owner", "test-owner/test-repo");
        var installation = new InstallationInfo(456, "test-account");

        // Act
        var context = new ProbotSharpContext(
            id,
            eventName,
            eventAction,
            payload,
            logger,
            gitHub,
            graphQL,
            repository,
            installation);

        // Assert
        context.Id.Should().Be(id);
        context.EventName.Should().Be(eventName);
        context.EventAction.Should().Be(eventAction);
        context.Payload.Should().NotBeNull();
        context.Payload["action"]!.Value<string>().Should().Be("opened");
        context.Logger.Should().Be(logger);
        context.GitHub.Should().Be(gitHub);
        context.Repository.Should().Be(repository);
        context.Installation.Should().Be(installation);
    }

    [Fact]
    public void Constructor_WithNullId_ShouldThrow()
    {
        // Arrange
        var payload = JObject.Parse("{}");
        var logger = Substitute.For<ILogger>();
        var gitHub = Substitute.For<IGitHubClient>();
        var graphQL = Substitute.For<IGitHubGraphQlClient>();

        // Act
        var act = () => new ProbotSharpContext(
            null!,
            "issues",
            "opened",
            payload,
            logger,
            gitHub,
            graphQL,
            null,
            null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyEventName_ShouldThrow()
    {
        // Arrange
        var payload = JObject.Parse("{}");
        var logger = Substitute.For<ILogger>();
        var gitHub = Substitute.For<IGitHubClient>();
        var graphQL = Substitute.For<IGitHubGraphQlClient>();

        // Act
        var act = () => new ProbotSharpContext(
            "delivery-123",
            string.Empty,
            "opened",
            payload,
            logger,
            gitHub,
            graphQL,
            null,
            null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNullPayload_ShouldThrow()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var gitHub = Substitute.For<IGitHubClient>();
        var graphQL = Substitute.For<IGitHubGraphQlClient>();

        // Act
        var act = () => new ProbotSharpContext(
            "delivery-123",
            "issues",
            "opened",
            null!,
            logger,
            gitHub,
            graphQL,
            null,
            null);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsBot_WhenSenderIsBot_ShouldReturnTrue()
    {
        // Arrange
        var payload = JObject.Parse("{\"sender\":{\"type\":\"Bot\"}}");
        var logger = Substitute.For<ILogger>();
        var gitHub = Substitute.For<IGitHubClient>();
        var graphQL = Substitute.For<IGitHubGraphQlClient>();
        var context = new ProbotSharpContext(
            "delivery-123",
            "issues",
            "opened",
            payload,
            logger,
            gitHub,
            graphQL,
            null,
            null);

        // Act
        var result = context.IsBot();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsBot_WhenSenderIsUser_ShouldReturnFalse()
    {
        // Arrange
        var payload = JObject.Parse("{\"sender\":{\"type\":\"User\"}}");
        var logger = Substitute.For<ILogger>();
        var gitHub = Substitute.For<IGitHubClient>();
        var graphQL = Substitute.For<IGitHubGraphQlClient>();
        var context = new ProbotSharpContext(
            "delivery-123",
            "issues",
            "opened",
            payload,
            logger,
            gitHub,
            graphQL,
            null,
            null);

        // Act
        var result = context.IsBot();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsBot_WhenNoSender_ShouldReturnFalse()
    {
        // Arrange
        var payload = JObject.Parse("{}");
        var logger = Substitute.For<ILogger>();
        var gitHub = Substitute.For<IGitHubClient>();
        var graphQL = Substitute.For<IGitHubGraphQlClient>();
        var context = new ProbotSharpContext(
            "delivery-123",
            "issues",
            "opened",
            payload,
            logger,
            gitHub,
            graphQL,
            null,
            null);

        // Act
        var result = context.IsBot();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetPayload_WithValidType_ShouldDeserialize()
    {
        // Arrange
        var payload = JObject.Parse("{\"issue\":{\"number\":42,\"title\":\"Test Issue\"}}");
        var logger = Substitute.For<ILogger>();
        var gitHub = Substitute.For<IGitHubClient>();
        var graphQL = Substitute.For<IGitHubGraphQlClient>();
        var context = new ProbotSharpContext(
            "delivery-123",
            "issues",
            "opened",
            payload,
            logger,
            gitHub,
            graphQL,
            null,
            null);

        // Act
        var result = context.GetPayload<TestPayload>();

        // Assert
        result.Should().NotBeNull();
        result.Issue.Should().NotBeNull();
        result.Issue.Number.Should().Be(42);
        result.Issue.Title.Should().Be("Test Issue");
    }

    [Fact]
    public void GetRepositoryFullName_WithRepository_ShouldReturnFullName()
    {
        // Arrange
        var payload = JObject.Parse("{}");
        var logger = Substitute.For<ILogger>();
        var gitHub = Substitute.For<IGitHubClient>();
        var graphQL = Substitute.For<IGitHubGraphQlClient>();
        var repository = new RepositoryInfo(123, "test-repo", "test-owner", "test-owner/test-repo");
        var context = new ProbotSharpContext(
            "delivery-123",
            "issues",
            "opened",
            payload,
            logger,
            gitHub,
            graphQL,
            repository,
            null);

        // Act
        var result = context.GetRepositoryFullName();

        // Assert
        result.Should().Be("test-owner/test-repo");
    }

    [Fact]
    public void GetRepositoryFullName_WithoutRepository_ShouldThrow()
    {
        // Arrange
        var payload = JObject.Parse("{}");
        var logger = Substitute.For<ILogger>();
        var gitHub = Substitute.For<IGitHubClient>();
        var graphQL = Substitute.For<IGitHubGraphQlClient>();
        var context = new ProbotSharpContext(
            "delivery-123",
            "issues",
            "opened",
            payload,
            logger,
            gitHub,
            graphQL,
            null,
            null);

        // Act
        var act = () => context.GetRepositoryFullName();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IsDryRun_WhenNotSpecified_ShouldDefaultToFalse()
    {
        // Arrange
        var payload = JObject.Parse("{}");
        var logger = Substitute.For<ILogger>();
        var gitHub = Substitute.For<IGitHubClient>();
        var graphQL = Substitute.For<IGitHubGraphQlClient>();

        // Act - using default parameter value
        var context = new ProbotSharpContext(
            "delivery-123",
            "issues",
            "opened",
            payload,
            logger,
            gitHub,
            graphQL,
            null,
            null);

        // Assert
        context.IsDryRun.Should().BeFalse();
    }

    [Fact]
    public void IsDryRun_WhenSetToTrue_ShouldReturnTrue()
    {
        // Arrange
        var payload = JObject.Parse("{}");
        var logger = Substitute.For<ILogger>();
        var gitHub = Substitute.For<IGitHubClient>();
        var graphQL = Substitute.For<IGitHubGraphQlClient>();

        // Act
        var context = new ProbotSharpContext(
            "delivery-123",
            "issues",
            "opened",
            payload,
            logger,
            gitHub,
            graphQL,
            null,
            null,
            isDryRun: true);

        // Assert
        context.IsDryRun.Should().BeTrue();
    }

    [Fact]
    public void IsDryRun_WhenSetToFalse_ShouldReturnFalse()
    {
        // Arrange
        var payload = JObject.Parse("{}");
        var logger = Substitute.For<ILogger>();
        var gitHub = Substitute.For<IGitHubClient>();
        var graphQL = Substitute.For<IGitHubGraphQlClient>();

        // Act
        var context = new ProbotSharpContext(
            "delivery-123",
            "issues",
            "opened",
            payload,
            logger,
            gitHub,
            graphQL,
            null,
            null,
            isDryRun: false);

        // Assert
        context.IsDryRun.Should().BeFalse();
    }

    private sealed class TestPayload
    {
        public TestIssue Issue { get; set; } = null!;
    }

    private sealed class TestIssue
    {
        public int Number { get; set; }

        public string Title { get; set; } = null!;
    }
}
