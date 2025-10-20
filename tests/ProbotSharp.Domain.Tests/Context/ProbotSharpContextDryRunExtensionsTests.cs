// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Linq;

using NSubstitute;

using Octokit;

using ProbotSharp.Domain.Context;
using ProbotSharp.Domain.Contracts;

namespace ProbotSharp.Domain.Tests.Context;

public class ProbotSharpContextDryRunExtensionsTests
{
    private readonly ILogger _logger;
    private readonly IGitHubClient _gitHub;
    private readonly IGitHubGraphQlClient _graphQL;

    public ProbotSharpContextDryRunExtensionsTests()
    {
        _logger = Substitute.For<ILogger>();
        _gitHub = Substitute.For<IGitHubClient>();
        _graphQL = Substitute.For<IGitHubGraphQlClient>();
    }

    private ProbotSharpContext CreateContext(bool isDryRun)
    {
        var payload = JObject.Parse("{}");
        return new ProbotSharpContext(
            "delivery-123",
            "issues",
            "opened",
            payload,
            _logger,
            _gitHub,
            _graphQL,
            null,
            null,
            isDryRun);
    }

    [Fact]
    public void LogDryRun_WhenInDryRunMode_ShouldLogInformation()
    {
        // Arrange
        var context = CreateContext(isDryRun: true);
        var action = "Create issue";
        var parameters = new { title = "Test Issue", body = "Test body" };

        // Act
        context.LogDryRun(action, parameters);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("[DRY-RUN]") && o.ToString()!.Contains(action)),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogDryRun_WhenNotInDryRunMode_ShouldNotLog()
    {
        // Arrange
        var context = CreateContext(isDryRun: false);
        var action = "Create issue";
        var parameters = new { title = "Test Issue" };

        // Act
        context.LogDryRun(action, parameters);

        // Assert
        _logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogDryRun_WithNullParameters_ShouldLogWithoutParameters()
    {
        // Arrange
        var context = CreateContext(isDryRun: true);
        var action = "Create issue";

        // Act
        context.LogDryRun(action, null);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("[DRY-RUN]") && o.ToString()!.Contains(action)),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogDryRun_WithNullContext_ShouldThrow()
    {
        // Arrange
        ProbotSharpContext? context = null;

        // Act
        var act = () => context!.LogDryRun("action");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LogDryRun_WithEmptyAction_ShouldThrow()
    {
        // Arrange
        var context = CreateContext(isDryRun: true);

        // Act
        var act = () => context.LogDryRun(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ThrowIfNotDryRun_WhenInDryRunMode_ShouldNotThrow()
    {
        // Arrange
        var context = CreateContext(isDryRun: true);

        // Act
        var act = () => context.ThrowIfNotDryRun("This is dangerous");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ThrowIfNotDryRun_WhenNotInDryRunMode_ShouldThrow()
    {
        // Arrange
        var context = CreateContext(isDryRun: false);
        var message = "This operation is too dangerous";

        // Act
        var act = () => context.ThrowIfNotDryRun(message);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage(message);
    }

    [Fact]
    public void ThrowIfNotDryRun_WithNullContext_ShouldThrow()
    {
        // Arrange
        ProbotSharpContext? context = null;

        // Act
        var act = () => context!.ThrowIfNotDryRun("message");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteOrLogAsync_WhenInDryRunMode_ShouldLogAndNotExecute()
    {
        // Arrange
        var context = CreateContext(isDryRun: true);
        var actionExecuted = false;
        var actionDescription = "Create issue comment";
        var parameters = new { owner = "test", repo = "repo", number = 1 };

        // Act
        await context.ExecuteOrLogAsync(
            actionDescription,
            async () =>
            {
                actionExecuted = true;
                await Task.CompletedTask;
            },
            parameters);

        // Assert
        actionExecuted.Should().BeFalse();
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("[DRY-RUN]") && o.ToString()!.Contains(actionDescription)),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExecuteOrLogAsync_WhenNotInDryRunMode_ShouldExecuteAndNotLog()
    {
        // Arrange
        var context = CreateContext(isDryRun: false);
        var actionExecuted = false;
        var actionDescription = "Create issue comment";

        // Act
        await context.ExecuteOrLogAsync(
            actionDescription,
            async () =>
            {
                actionExecuted = true;
                await Task.CompletedTask;
            });

        // Assert
        actionExecuted.Should().BeTrue();
        _logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExecuteOrLogAsync_WithResult_WhenInDryRunMode_ShouldReturnDryRunResult()
    {
        // Arrange
        var context = CreateContext(isDryRun: true);
        var actionDescription = "Get issue";
        var dryRunResult = 42;

        // Act
        var result = await context.ExecuteOrLogAsync(
            actionDescription,
            async () =>
            {
                await Task.CompletedTask;
                return 999; // This should not be returned
            },
            dryRunResult: dryRunResult);

        // Assert
        result.Should().Be(dryRunResult);
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("[DRY-RUN]") && o.ToString()!.Contains(actionDescription)),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExecuteOrLogAsync_WithResult_WhenNotInDryRunMode_ShouldReturnActualResult()
    {
        // Arrange
        var context = CreateContext(isDryRun: false);
        var actionDescription = "Get issue";
        var actualResult = 999;
        var dryRunResult = 42;

        // Act
        var result = await context.ExecuteOrLogAsync(
            actionDescription,
            async () =>
            {
                await Task.CompletedTask;
                return actualResult;
            },
            dryRunResult: dryRunResult);

        // Assert
        result.Should().Be(actualResult);
        _logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExecuteOrLogAsync_WithNullAction_ShouldThrow()
    {
        // Arrange
        var context = CreateContext(isDryRun: false);

        // Act
        var act = async () => await context.ExecuteOrLogAsync("action", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteOrLogAsync_WithEmptyDescription_ShouldThrow()
    {
        // Arrange
        var context = CreateContext(isDryRun: false);

        // Act
        var act = async () => await context.ExecuteOrLogAsync(
            string.Empty,
            async () => await Task.CompletedTask);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
