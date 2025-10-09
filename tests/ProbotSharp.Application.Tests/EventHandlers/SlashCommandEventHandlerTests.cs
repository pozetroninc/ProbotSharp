// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Octokit;
using ProbotSharp.Application.EventHandlers;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Context;
using ProbotSharp.Domain.Contracts;

namespace ProbotSharp.Application.Tests.EventHandlers;

public class SlashCommandEventHandlerTests
{
    private readonly SlashCommandRouter _router;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SlashCommandEventHandler> _logger;
    private readonly SlashCommandEventHandler _handler;

    public SlashCommandEventHandlerTests()
    {
        this._router = Substitute.For<SlashCommandRouter>(Substitute.For<ILogger<SlashCommandRouter>>());
        this._serviceProvider = Substitute.For<IServiceProvider>();
        this._logger = Substitute.For<ILogger<SlashCommandEventHandler>>();
        this._handler = new SlashCommandEventHandler(this._router, this._serviceProvider, this._logger);
    }

    [Fact]
    public async Task HandleAsync_WithSlashCommand_ShouldRouteToRouter()
    {
        // Arrange
        var context = CreateContextWithCommentBody("/label bug");

        // Act
        await this._handler.HandleAsync(context);

        // Assert
        await this._router.Received(1).RouteAsync(
            Arg.Is(context),
            Arg.Is("/label bug"),
            Arg.Is(this._serviceProvider),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithMultipleSlashCommands_ShouldRouteToRouter()
    {
        // Arrange
        var commentBody = "/label bug\n/assign @user";
        var context = CreateContextWithCommentBody(commentBody);

        // Act
        await this._handler.HandleAsync(context);

        // Assert
        await this._router.Received(1).RouteAsync(
            Arg.Is(context),
            Arg.Is(commentBody),
            Arg.Is(this._serviceProvider),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithNoSlashCommands_ShouldNotRouteToRouter()
    {
        // Arrange
        var context = CreateContextWithCommentBody("This is a regular comment");

        // Act
        await this._handler.HandleAsync(context);

        // Assert
        await this._router.DidNotReceive().RouteAsync(
            Arg.Any<ProbotSharpContext>(),
            Arg.Any<string>(),
            Arg.Any<IServiceProvider>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithBotComment_ShouldNotRouteToRouter()
    {
        // Arrange
        var context = CreateContextWithCommentBody("/label bug", isBot: true);

        // Act
        await this._handler.HandleAsync(context);

        // Assert
        await this._router.DidNotReceive().RouteAsync(
            Arg.Any<ProbotSharpContext>(),
            Arg.Any<string>(),
            Arg.Any<IServiceProvider>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithEmptyCommentBody_ShouldNotRouteToRouter()
    {
        // Arrange
        var context = CreateContextWithCommentBody(string.Empty);

        // Act
        await this._handler.HandleAsync(context);

        // Assert
        await this._router.DidNotReceive().RouteAsync(
            Arg.Any<ProbotSharpContext>(),
            Arg.Any<string>(),
            Arg.Any<IServiceProvider>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithNullCommentBody_ShouldNotRouteToRouter()
    {
        // Arrange
        var payload = JObject.Parse(@"{
            ""action"": ""created"",
            ""comment"": {
                ""body"": null
            }
        }");
        var context = CreateContext(payload);

        // Act
        await this._handler.HandleAsync(context);

        // Assert
        await this._router.DidNotReceive().RouteAsync(
            Arg.Any<ProbotSharpContext>(),
            Arg.Any<string>(),
            Arg.Any<IServiceProvider>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithMissingCommentInPayload_ShouldNotThrow()
    {
        // Arrange
        var payload = JObject.Parse(@"{
            ""action"": ""created""
        }");
        var context = CreateContext(payload);

        // Act & Assert - should not throw
        await this._handler.HandleAsync(context);

        await this._router.DidNotReceive().RouteAsync(
            Arg.Any<ProbotSharpContext>(),
            Arg.Any<string>(),
            Arg.Any<IServiceProvider>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithWhitespaceOnlyCommentBody_ShouldNotRouteToRouter()
    {
        // Arrange
        var context = CreateContextWithCommentBody("   \n\t  ");

        // Act
        await this._handler.HandleAsync(context);

        // Assert
        await this._router.DidNotReceive().RouteAsync(
            Arg.Any<ProbotSharpContext>(),
            Arg.Any<string>(),
            Arg.Any<IServiceProvider>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithIssueCommentEvent_ShouldProcessCommands()
    {
        // Arrange
        var context = CreateContextWithCommentBody("/label bug", eventName: "issue_comment");

        // Act
        await this._handler.HandleAsync(context);

        // Assert
        await this._router.Received(1).RouteAsync(
            Arg.Any<ProbotSharpContext>(),
            Arg.Any<string>(),
            Arg.Any<IServiceProvider>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithPullRequestReviewCommentEvent_ShouldProcessCommands()
    {
        // Arrange
        var context = CreateContextWithCommentBody("/label bug", eventName: "pull_request_review_comment");

        // Act
        await this._handler.HandleAsync(context);

        // Assert
        await this._router.Received(1).RouteAsync(
            Arg.Any<ProbotSharpContext>(),
            Arg.Any<string>(),
            Arg.Any<IServiceProvider>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithCancellationToken_ShouldPassToRouter()
    {
        // Arrange
        var context = CreateContextWithCommentBody("/label bug");
        var cts = new CancellationTokenSource();

        // Act
        await this._handler.HandleAsync(context, cts.Token);

        // Assert
        await this._router.Received(1).RouteAsync(
            Arg.Any<ProbotSharpContext>(),
            Arg.Any<string>(),
            Arg.Any<IServiceProvider>(),
            Arg.Is(cts.Token));
    }

    // Helper methods
    private static ProbotSharpContext CreateContextWithCommentBody(
        string commentBody,
        bool isBot = false,
        string eventName = "issue_comment")
    {
        var payload = JObject.Parse($@"{{
            ""action"": ""created"",
            ""comment"": {{
                ""body"": ""{commentBody.Replace("\"", "\\\"")}""
            }},
            ""sender"": {{
                ""type"": ""{(isBot ? "Bot" : "User")}""
            }}
        }}");

        return CreateContext(payload, eventName);
    }

    private static ProbotSharpContext CreateContext(JObject payload, string eventName = "issue_comment")
    {
        var logger = Substitute.For<ILogger>();
        var gitHub = Substitute.For<IGitHubClient>();
        var graphQL = Substitute.For<IGitHubGraphQlClient>();
        var repository = new RepositoryInfo(123, "test-repo", "test-owner", "test-owner/test-repo");
        var installation = new InstallationInfo(456, "test-account");

        return new ProbotSharpContext(
            "delivery-123",
            eventName,
            "created",
            payload,
            logger,
            gitHub,
            graphQL,
            repository,
            installation);
    }
}
