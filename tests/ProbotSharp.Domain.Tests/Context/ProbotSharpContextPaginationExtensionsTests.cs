// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Octokit;
using ProbotSharp.Domain.Context;
using ProbotSharp.Domain.Contracts;

namespace ProbotSharp.Domain.Tests.Context;

public class ProbotSharpContextPaginationExtensionsTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();
    private readonly IGitHubClient _gitHub = Substitute.For<IGitHubClient>();
    private readonly IGitHubGraphQlClient _graphQL = Substitute.For<IGitHubGraphQlClient>();
    private readonly IIssuesClient _issuesClient = Substitute.For<IIssuesClient>();
    private readonly IPullRequestsClient _pullRequestsClient = Substitute.For<IPullRequestsClient>();
    private readonly IIssueCommentsClient _issueCommentsClient = Substitute.For<IIssueCommentsClient>();

    public ProbotSharpContextPaginationExtensionsTests()
    {
        _gitHub.Issue.Returns(_issuesClient);
        _gitHub.PullRequest.Returns(_pullRequestsClient);
        _issuesClient.Comment.Returns(_issueCommentsClient);
    }

    [Fact]
    public async Task GetAllIssuesAsync_WithValidContext_ShouldCallCorrectAPI()
    {
        // Arrange
        var payload = JObject.Parse(@"{}");
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

        var expectedIssues = new List<Issue>
        {
            CreateMockIssue(1),
            CreateMockIssue(2)
        };

        _issuesClient
            .GetAllForRepository("test-owner", "test-repo", ApiOptions.None)
            .Returns(expectedIssues.AsReadOnly());

        // Act
        var result = await context.GetAllIssuesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedIssues);
        await _issuesClient.Received(1).GetAllForRepository("test-owner", "test-repo", ApiOptions.None);
    }

    [Fact]
    public async Task GetAllIssuesAsync_WithApiOptions_ShouldPassOptions()
    {
        // Arrange
        var payload = JObject.Parse(@"{}");
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

        var options = new ApiOptions
        {
            PageSize = 50,
            PageCount = 2
        };

        var expectedIssues = new List<Issue> { CreateMockIssue(1) };
        _issuesClient
            .GetAllForRepository("test-owner", "test-repo", options)
            .Returns(expectedIssues.AsReadOnly());

        // Act
        var result = await context.GetAllIssuesAsync(options);

        // Assert
        result.Should().HaveCount(1);
        await _issuesClient.Received(1).GetAllForRepository("test-owner", "test-repo", options);
    }

    [Fact]
    public async Task GetAllIssuesAsync_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        ProbotSharpContext? context = null;

        // Act
        var act = async () => await context!.GetAllIssuesAsync();

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetAllIssuesAsync_WithNullRepository_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = JObject.Parse(@"{}");
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
        var act = async () => await context.GetAllIssuesAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Repository owner not found*");
    }

    [Fact]
    public async Task GetAllPullRequestsAsync_WithValidContext_ShouldCallCorrectAPI()
    {
        // Arrange
        var payload = JObject.Parse(@"{}");
        var repository = new RepositoryInfo(123, "my-repo", "my-owner", "my-owner/my-repo");
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

        var expectedPRs = new List<PullRequest>
        {
            CreateMockPullRequest(1),
            CreateMockPullRequest(2),
            CreateMockPullRequest(3)
        };

        _pullRequestsClient
            .GetAllForRepository("my-owner", "my-repo", ApiOptions.None)
            .Returns(expectedPRs.AsReadOnly());

        // Act
        var result = await context.GetAllPullRequestsAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(expectedPRs);
        await _pullRequestsClient.Received(1).GetAllForRepository("my-owner", "my-repo", ApiOptions.None);
    }

    [Fact]
    public async Task GetAllPullRequestsAsync_WithApiOptions_ShouldPassOptions()
    {
        // Arrange
        var payload = JObject.Parse(@"{}");
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

        var options = new ApiOptions
        {
            PageSize = 100,
            PageCount = 1
        };

        var expectedPRs = new List<PullRequest> { CreateMockPullRequest(42) };
        _pullRequestsClient
            .GetAllForRepository("test-owner", "test-repo", options)
            .Returns(expectedPRs.AsReadOnly());

        // Act
        var result = await context.GetAllPullRequestsAsync(options);

        // Assert
        result.Should().HaveCount(1);
        await _pullRequestsClient.Received(1).GetAllForRepository("test-owner", "test-repo", options);
    }

    [Fact]
    public async Task GetAllPullRequestsAsync_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        ProbotSharpContext? context = null;

        // Act
        var act = async () => await context!.GetAllPullRequestsAsync();

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetAllCommentsAsync_WithValidContext_ShouldCallCorrectAPI()
    {
        // Arrange
        var payload = JObject.Parse(@"{}");
        var repository = new RepositoryInfo(123, "test-repo", "test-owner", "test-owner/test-repo");
        var context = new ProbotSharpContext(
            "delivery-1",
            "issue_comment",
            "created",
            payload,
            _logger,
            _gitHub,
            _graphQL,
            repository,
            null);

        var expectedComments = new List<IssueComment>
        {
            CreateMockIssueComment(1),
            CreateMockIssueComment(2)
        };

        _issueCommentsClient
            .GetAllForIssue("test-owner", "test-repo", 42, ApiOptions.None)
            .Returns(expectedComments.AsReadOnly());

        // Act
        var result = await context.GetAllCommentsAsync(42);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedComments);
        await _issueCommentsClient.Received(1).GetAllForIssue("test-owner", "test-repo", 42, ApiOptions.None);
    }

    [Fact]
    public async Task GetAllCommentsAsync_WithApiOptions_ShouldPassOptions()
    {
        // Arrange
        var payload = JObject.Parse(@"{}");
        var repository = new RepositoryInfo(123, "test-repo", "test-owner", "test-owner/test-repo");
        var context = new ProbotSharpContext(
            "delivery-1",
            "issue_comment",
            "created",
            payload,
            _logger,
            _gitHub,
            _graphQL,
            repository,
            null);

        var options = new ApiOptions
        {
            PageSize = 10,
            PageCount = 1
        };

        var expectedComments = new List<IssueComment> { CreateMockIssueComment(1) };
        _issueCommentsClient
            .GetAllForIssue("test-owner", "test-repo", 99, options)
            .Returns(expectedComments.AsReadOnly());

        // Act
        var result = await context.GetAllCommentsAsync(99, options);

        // Assert
        result.Should().HaveCount(1);
        await _issueCommentsClient.Received(1).GetAllForIssue("test-owner", "test-repo", 99, options);
    }

    [Fact]
    public async Task GetAllCommentsAsync_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        ProbotSharpContext? context = null;

        // Act
        var act = async () => await context!.GetAllCommentsAsync(42);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    // Helper methods to create mock objects
    private static Issue CreateMockIssue(int number)
    {
        return new Issue(
            url: $"https://api.github.com/repos/test/test/issues/{number}",
            htmlUrl: $"https://github.com/test/test/issues/{number}",
            commentsUrl: $"https://api.github.com/repos/test/test/issues/{number}/comments",
            eventsUrl: $"https://api.github.com/repos/test/test/issues/{number}/events",
            number: number,
            state: ItemState.Open,
            title: $"Issue {number}",
            body: $"Body of issue {number}",
            closedBy: null,
            user: CreateMockUser(),
            labels: new List<Label>().AsReadOnly(),
            assignee: null,
            assignees: new List<User>().AsReadOnly(),
            milestone: null,
            comments: 0,
            pullRequest: null,
            closedAt: null,
            createdAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow,
            id: number,
            nodeId: $"node_{number}",
            locked: false,
            repository: null,
            reactions: null,
            activeLockReason: null,
            stateReason: null);
    }

    private static PullRequest CreateMockPullRequest(int number)
    {
        return new PullRequest(number);
    }

    private static IssueComment CreateMockIssueComment(int id)
    {
        return new IssueComment(
            id: id,
            nodeId: $"node_{id}",
            url: $"https://api.github.com/repos/test/test/issues/comments/{id}",
            htmlUrl: $"https://github.com/test/test/issues/42#issuecomment-{id}",
            body: $"Comment body {id}",
            createdAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow,
            user: CreateMockUser(),
            reactions: null,
            authorAssociation: AuthorAssociation.None);
    }

    private static User CreateMockUser()
    {
        return new User(
            avatarUrl: "https://avatars.githubusercontent.com/u/1?v=4",
            bio: null,
            blog: null,
            collaborators: 0,
            company: null,
            createdAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow,
            diskUsage: 0,
            email: null,
            followers: 0,
            following: 0,
            hireable: false,
            htmlUrl: "https://github.com/testuser",
            totalPrivateRepos: 0,
            id: 1,
            nodeId: "node_1",
            location: null,
            login: "testuser",
            name: "Test User",
            ownedPrivateRepos: 0,
            plan: null,
            privateGists: 0,
            publicGists: 0,
            publicRepos: 0,
            url: "https://api.github.com/users/testuser",
            permissions: null,
            siteAdmin: false,
            ldapDistinguishedName: null,
            suspendedAt: null);
    }
}
