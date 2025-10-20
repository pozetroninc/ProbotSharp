// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Linq;

using NSubstitute;

using Octokit;

using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Attachments;
using ProbotSharp.Domain.Context;
using ProbotSharp.Domain.Contracts;

namespace ProbotSharp.Application.Tests.Services;

public class CommentAttachmentServiceTests
{
    private readonly IGitHubClient _gitHubClient = Substitute.For<IGitHubClient>();
    private readonly IGitHubGraphQlClient _graphQLClient = Substitute.For<IGitHubGraphQlClient>();
    private readonly IIssuesClient _issuesClient = Substitute.For<IIssuesClient>();
    private readonly IIssueCommentsClient _commentsClient = Substitute.For<IIssueCommentsClient>();
    private readonly ILogger _logger = Substitute.For<ILogger>();

    public CommentAttachmentServiceTests()
    {
        _gitHubClient.Issue.Returns(_issuesClient);
        _issuesClient.Comment.Returns(_commentsClient);
    }

    private ProbotSharpContext CreateContext(JObject payload)
    {
        return new ProbotSharpContext(
            id: "test-delivery-id",
            eventName: "issue_comment",
            eventAction: "created",
            payload: payload,
            logger: _logger,
            gitHub: _gitHubClient,
            graphQL: _graphQLClient,
            repository: new RepositoryInfo(123456, "testrepo", "testowner", "testowner/testrepo"),
            installation: null);
    }

    [Fact]
    public async Task AddAsync_WithValidComment_ShouldAppendAttachment()
    {
        // Arrange
        var payload = new JObject
        {
            ["comment"] = new JObject
            {
                ["id"] = 12345,
                ["body"] = "Original comment text",
            },
        };

        var context = CreateContext(payload);
        var existingComment = new IssueComment(
            id: 12345,
            nodeId: "node123",
            url: "https://api.github.com/repos/testowner/testrepo/issues/comments/12345",
            htmlUrl: "https://github.com/testowner/testrepo/issues/1#issuecomment-12345",
            body: "Original comment text",
            createdAt: DateTimeOffset.UtcNow,
            updatedAt: null,
            user: Substitute.For<User>(),
            reactions: null,
            authorAssociation: AuthorAssociation.None);

        _commentsClient.Get("testowner", "testrepo", 12345)
            .Returns(Task.FromResult(existingComment));

        var service = new CommentAttachmentService(context);
        var attachment = new CommentAttachment
        {
            Title = "Test Attachment",
            Text = "Test text",
        };

        // Act
        await service.AddAsync(attachment);

        // Assert
        await _commentsClient.Received(1).Update(
            "testowner",
            "testrepo",
            12345,
            Arg.Is<string>(body =>
                body.Contains("Original comment text") &&
                body.Contains("<!-- probot-sharp-attachments -->") &&
                body.Contains("### Test Attachment")));
    }

    [Fact]
    public async Task AddAsync_WithExistingAttachments_ShouldReplaceAttachments()
    {
        // Arrange
        var payload = new JObject
        {
            ["comment"] = new JObject
            {
                ["id"] = 12345,
                ["body"] = "Original comment text",
            },
        };

        var context = CreateContext(payload);
        var existingComment = new IssueComment(
            id: 12345,
            nodeId: "node123",
            url: "https://api.github.com/repos/testowner/testrepo/issues/comments/12345",
            htmlUrl: "https://github.com/testowner/testrepo/issues/1#issuecomment-12345",
            body: "Original comment text\n\n<!-- probot-sharp-attachments -->\n\n---\n\n### Old Attachment\n\n---",
            createdAt: DateTimeOffset.UtcNow,
            updatedAt: null,
            user: Substitute.For<User>(),
            reactions: null,
            authorAssociation: AuthorAssociation.None);

        _commentsClient.Get("testowner", "testrepo", 12345)
            .Returns(Task.FromResult(existingComment));

        var service = new CommentAttachmentService(context);
        var attachment = new CommentAttachment
        {
            Title = "New Attachment",
        };

        // Act
        await service.AddAsync(attachment);

        // Assert
        await _commentsClient.Received(1).Update(
            "testowner",
            "testrepo",
            12345,
            Arg.Is<string>(body =>
                body.Contains("Original comment text") &&
                body.Contains("### New Attachment") &&
                !body.Contains("### Old Attachment")));
    }

    [Fact]
    public async Task AddAsync_WithMultipleAttachments_ShouldRenderAll()
    {
        // Arrange
        var payload = new JObject
        {
            ["comment"] = new JObject
            {
                ["id"] = 12345,
                ["body"] = "Original comment text",
            },
        };

        var context = CreateContext(payload);
        var existingComment = new IssueComment(
            id: 12345,
            nodeId: "node123",
            url: "https://api.github.com/repos/testowner/testrepo/issues/comments/12345",
            htmlUrl: "https://github.com/testowner/testrepo/issues/1#issuecomment-12345",
            body: "Original comment text",
            createdAt: DateTimeOffset.UtcNow,
            updatedAt: null,
            user: Substitute.For<User>(),
            reactions: null,
            authorAssociation: AuthorAssociation.None);

        _commentsClient.Get("testowner", "testrepo", 12345)
            .Returns(Task.FromResult(existingComment));

        var service = new CommentAttachmentService(context);
        var attachments = new[]
        {
            new CommentAttachment { Title = "First" },
            new CommentAttachment { Title = "Second" },
        };

        // Act
        await service.AddAsync(attachments);

        // Assert
        await _commentsClient.Received(1).Update(
            "testowner",
            "testrepo",
            12345,
            Arg.Is<string>(body =>
                body.Contains("### First") &&
                body.Contains("### Second")));
    }

    [Fact]
    public async Task AddAsync_WithNoCommentInPayload_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = new JObject
        {
            ["issue"] = new JObject { ["number"] = 1 },
        };

        var context = CreateContext(payload);
        var service = new CommentAttachmentService(context);
        var attachment = new CommentAttachment { Title = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AddAsync(attachment));
    }

    [Fact]
    public async Task AddAsync_WithReviewComment_ShouldExtractCommentId()
    {
        // Arrange
        var payload = new JObject
        {
            ["review_comment"] = new JObject
            {
                ["id"] = 67890,
                ["body"] = "Review comment text",
            },
        };

        var context = CreateContext(payload);
        var existingComment = new IssueComment(
            id: 67890,
            nodeId: "node456",
            url: "https://api.github.com/repos/testowner/testrepo/issues/comments/67890",
            htmlUrl: "https://github.com/testowner/testrepo/issues/1#issuecomment-67890",
            body: "Review comment text",
            createdAt: DateTimeOffset.UtcNow,
            updatedAt: null,
            user: Substitute.For<User>(),
            reactions: null,
            authorAssociation: AuthorAssociation.None);

        _commentsClient.Get("testowner", "testrepo", 67890)
            .Returns(Task.FromResult(existingComment));

        var service = new CommentAttachmentService(context);
        var attachment = new CommentAttachment { Title = "Test" };

        // Act
        await service.AddAsync(attachment);

        // Assert
        await _commentsClient.Received(1).Get("testowner", "testrepo", 67890);
        await _commentsClient.Received(1).Update(
            "testowner",
            "testrepo",
            67890,
            Arg.Any<string>());
    }

    [Fact]
    public async Task AddAsync_WithNoRepository_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = new JObject
        {
            ["comment"] = new JObject
            {
                ["id"] = 12345,
                ["body"] = "Original comment text",
            },
        };

        var context = new ProbotSharpContext(
            id: "test-delivery-id",
            eventName: "issue_comment",
            eventAction: "created",
            payload: payload,
            logger: _logger,
            gitHub: _gitHubClient,
            graphQL: _graphQLClient,
            repository: null, // No repository
            installation: null);

        var service = new CommentAttachmentService(context);
        var attachment = new CommentAttachment { Title = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AddAsync(attachment));
    }

    [Fact]
    public async Task AddAsync_WithFieldsInAttachment_ShouldRenderFields()
    {
        // Arrange
        var payload = new JObject
        {
            ["comment"] = new JObject
            {
                ["id"] = 12345,
                ["body"] = "Original comment text",
            },
        };

        var context = CreateContext(payload);
        var existingComment = new IssueComment(
            id: 12345,
            nodeId: "node123",
            url: "https://api.github.com/repos/testowner/testrepo/issues/comments/12345",
            htmlUrl: "https://github.com/testowner/testrepo/issues/1#issuecomment-12345",
            body: "Original comment text",
            createdAt: DateTimeOffset.UtcNow,
            updatedAt: null,
            user: Substitute.For<User>(),
            reactions: null,
            authorAssociation: AuthorAssociation.None);

        _commentsClient.Get("testowner", "testrepo", 12345)
            .Returns(Task.FromResult(existingComment));

        var service = new CommentAttachmentService(context);
        var attachment = new CommentAttachment
        {
            Title = "Build Status",
            Fields = new List<AttachmentField>
            {
                new() { Title = "Duration", Value = "2m 34s" },
                new() { Title = "Tests", Value = "142 passed" },
            },
        };

        // Act
        await service.AddAsync(attachment);

        // Assert
        await _commentsClient.Received(1).Update(
            "testowner",
            "testrepo",
            12345,
            Arg.Is<string>(body =>
                body.Contains("**Duration**: 2m 34s") &&
                body.Contains("**Tests**: 142 passed")));
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CommentAttachmentService(null!));
    }
}
