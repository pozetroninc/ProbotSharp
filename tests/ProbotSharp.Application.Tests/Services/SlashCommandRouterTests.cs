// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Octokit;
using ProbotSharp.Application.Abstractions.Commands;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Commands;
using ProbotSharp.Domain.Context;
using ProbotSharp.Domain.Contracts;

namespace ProbotSharp.Application.Tests.Services;

public class SlashCommandRouterTests
{
    private readonly ILogger<SlashCommandRouter> _logger;
    private readonly SlashCommandRouter _router;

    public SlashCommandRouterTests()
    {
        this._logger = Substitute.For<ILogger<SlashCommandRouter>>();
        this._router = new SlashCommandRouter(this._logger);
    }

    [Fact]
    public void RegisterHandler_WithValidHandler_ShouldRegister()
    {
        // Arrange
        var commandName = "test";
        var handlerType = typeof(TestCommandHandler);

        // Act
        this._router.RegisterHandler(commandName, handlerType);

        // Assert - should not throw
    }

    [Fact]
    public void RegisterHandler_WithNullCommandName_ShouldThrowArgumentException()
    {
        // Arrange
        string commandName = null!;
        var handlerType = typeof(TestCommandHandler);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => this._router.RegisterHandler(commandName, handlerType));
    }

    [Fact]
    public void RegisterHandler_WithEmptyCommandName_ShouldThrowArgumentException()
    {
        // Arrange
        var commandName = string.Empty;
        var handlerType = typeof(TestCommandHandler);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => this._router.RegisterHandler(commandName, handlerType));
    }

    [Fact]
    public void RegisterHandler_WithNullHandlerType_ShouldThrowArgumentNullException()
    {
        // Arrange
        var commandName = "test";
        Type handlerType = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => this._router.RegisterHandler(commandName, handlerType));
    }

    [Fact]
    public void RegisterHandler_WithNonHandlerType_ShouldThrowArgumentException()
    {
        // Arrange
        var commandName = "test";
        var handlerType = typeof(string); // Not an ISlashCommandHandler

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => this._router.RegisterHandler(commandName, handlerType));
        exception.Message.Should().Contain("must implement ISlashCommandHandler");
    }

    [Fact]
    public async Task RouteAsync_WithNoCommands_ShouldNotInvokeHandlers()
    {
        // Arrange
        var context = CreateTestContext();
        var commentBody = "This is a regular comment without commands";
        var serviceProvider = CreateServiceProvider();

        // Act
        await this._router.RouteAsync(context, commentBody, serviceProvider);

        // Assert - no exceptions thrown
    }

    [Fact]
    public async Task RouteAsync_WithValidCommand_ShouldInvokeHandler()
    {
        // Arrange
        var context = CreateTestContext();
        var commentBody = "/test argument";
        var handler = Substitute.For<ISlashCommandHandler>();
        var serviceProvider = CreateServiceProviderWithHandler<TestCommandHandler>(handler);

        this._router.RegisterHandler("test", typeof(TestCommandHandler));

        // Act
        await this._router.RouteAsync(context, commentBody, serviceProvider);

        // Assert
        await handler.Received(1).HandleAsync(
            Arg.Is(context),
            Arg.Is<SlashCommand>(cmd => cmd.Name == "test" && cmd.Arguments == "argument"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RouteAsync_WithMultipleCommands_ShouldInvokeAllHandlers()
    {
        // Arrange
        var context = CreateTestContext();
        var commentBody = "/test1 arg1\n/test2 arg2";
        var handler1 = Substitute.For<ISlashCommandHandler>();
        var handler2 = Substitute.For<ISlashCommandHandler>();

        var services = new ServiceCollection();
        services.AddTransient<TestCommandHandler>(_ => new TestCommandHandler(handler1));
        services.AddTransient<AnotherTestCommandHandler>(_ => new AnotherTestCommandHandler(handler2));
        var serviceProvider = services.BuildServiceProvider();

        this._router.RegisterHandler("test1", typeof(TestCommandHandler));
        this._router.RegisterHandler("test2", typeof(AnotherTestCommandHandler));

        // Act
        await this._router.RouteAsync(context, commentBody, serviceProvider);

        // Assert
        await handler1.Received(1).HandleAsync(
            Arg.Any<ProbotSharpContext>(),
            Arg.Is<SlashCommand>(cmd => cmd.Name == "test1"),
            Arg.Any<CancellationToken>());

        await handler2.Received(1).HandleAsync(
            Arg.Any<ProbotSharpContext>(),
            Arg.Is<SlashCommand>(cmd => cmd.Name == "test2"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RouteAsync_WithMultipleHandlersForSameCommand_ShouldInvokeBoth()
    {
        // Arrange
        var context = CreateTestContext();
        var commentBody = "/test argument";
        var handler1 = Substitute.For<ISlashCommandHandler>();
        var handler2 = Substitute.For<ISlashCommandHandler>();

        var services = new ServiceCollection();
        services.AddTransient<TestCommandHandler>(_ => new TestCommandHandler(handler1));
        services.AddTransient<AnotherTestCommandHandler>(_ => new AnotherTestCommandHandler(handler2));
        var serviceProvider = services.BuildServiceProvider();

        this._router.RegisterHandler("test", typeof(TestCommandHandler));
        this._router.RegisterHandler("test", typeof(AnotherTestCommandHandler));

        // Act
        await this._router.RouteAsync(context, commentBody, serviceProvider);

        // Assert
        await handler1.Received(1).HandleAsync(
            Arg.Any<ProbotSharpContext>(),
            Arg.Is<SlashCommand>(cmd => cmd.Name == "test"),
            Arg.Any<CancellationToken>());

        await handler2.Received(1).HandleAsync(
            Arg.Any<ProbotSharpContext>(),
            Arg.Is<SlashCommand>(cmd => cmd.Name == "test"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RouteAsync_WithNoMatchingHandler_ShouldNotThrow()
    {
        // Arrange
        var context = CreateTestContext();
        var commentBody = "/unknown command";
        var serviceProvider = CreateServiceProvider();

        // Act & Assert - should not throw
        await this._router.RouteAsync(context, commentBody, serviceProvider);
    }

    [Fact]
    public async Task RouteAsync_WithHandlerThrowing_ShouldContinueProcessing()
    {
        // Arrange
        var context = CreateTestContext();
        var commentBody = "/test1 arg1\n/test2 arg2";

        var handler1 = Substitute.For<ISlashCommandHandler>();
        handler1.HandleAsync(Arg.Any<ProbotSharpContext>(), Arg.Any<SlashCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Handler failed")));

        var handler2 = Substitute.For<ISlashCommandHandler>();

        var services = new ServiceCollection();
        services.AddTransient<TestCommandHandler>(_ => new TestCommandHandler(handler1));
        services.AddTransient<AnotherTestCommandHandler>(_ => new AnotherTestCommandHandler(handler2));
        var serviceProvider = services.BuildServiceProvider();

        this._router.RegisterHandler("test1", typeof(TestCommandHandler));
        this._router.RegisterHandler("test2", typeof(AnotherTestCommandHandler));

        // Act
        await this._router.RouteAsync(context, commentBody, serviceProvider);

        // Assert - second handler should still be called
        await handler2.Received(1).HandleAsync(
            Arg.Any<ProbotSharpContext>(),
            Arg.Is<SlashCommand>(cmd => cmd.Name == "test2"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RouteAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var context = CreateTestContext();
        var commentBody = "/test argument";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = Substitute.For<ISlashCommandHandler>();
        handler.HandleAsync(Arg.Any<ProbotSharpContext>(), Arg.Any<SlashCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromCanceled(cts.Token));

        var serviceProvider = CreateServiceProviderWithHandler<TestCommandHandler>(handler);
        this._router.RegisterHandler("test", typeof(TestCommandHandler));

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            this._router.RouteAsync(context, commentBody, serviceProvider, cts.Token));
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task RouteAsync_WithCaseInsensitiveCommandName_ShouldMatchHandler()
    {
        // Arrange
        var context = CreateTestContext();
        var commentBody = "/TEST argument"; // Uppercase in comment
        var handler = Substitute.For<ISlashCommandHandler>();
        var serviceProvider = CreateServiceProviderWithHandler<TestCommandHandler>(handler);

        this._router.RegisterHandler("test", typeof(TestCommandHandler)); // Lowercase registration

        // Act
        await this._router.RouteAsync(context, commentBody, serviceProvider);

        // Assert
        await handler.Received(1).HandleAsync(
            Arg.Any<ProbotSharpContext>(),
            Arg.Is<SlashCommand>(cmd => cmd.Name == "TEST"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RouteAsync_WithEmptyCommentBody_ShouldNotThrow()
    {
        // Arrange
        var context = CreateTestContext();
        var commentBody = string.Empty;
        var serviceProvider = CreateServiceProvider();

        // Act & Assert - should not throw
        await this._router.RouteAsync(context, commentBody, serviceProvider);
    }

    [Fact]
    public async Task RouteAsync_WithNullCommentBody_ShouldNotThrow()
    {
        // Arrange
        var context = CreateTestContext();
        string? commentBody = null;
        var serviceProvider = CreateServiceProvider();

        // Act & Assert - should not throw
        await this._router.RouteAsync(context, commentBody!, serviceProvider);
    }

    [Fact]
    public void RegisterHandler_WithSameHandlerMultipleTimes_ShouldOnlyRegisterOnce()
    {
        // Arrange
        var commandName = "test";
        var handlerType = typeof(TestCommandHandler);

        // Act
        this._router.RegisterHandler(commandName, handlerType);
        this._router.RegisterHandler(commandName, handlerType); // Register again

        // Assert - should not throw and should only register once (check via routing)
        var context = CreateTestContext();
        var commentBody = "/test arg";
        var handler = Substitute.For<ISlashCommandHandler>();
        var serviceProvider = CreateServiceProviderWithHandler<TestCommandHandler>(handler);

        // When routing, the handler should only be invoked once
        this._router.RouteAsync(context, commentBody, serviceProvider).Wait();
        handler.Received(1).HandleAsync(Arg.Any<ProbotSharpContext>(), Arg.Any<SlashCommand>(), Arg.Any<CancellationToken>());
    }

    // Helper methods
    private static ProbotSharpContext CreateTestContext()
    {
        var payload = JObject.Parse("{\"action\":\"created\",\"comment\":{\"body\":\"test\"}}");
        var logger = Substitute.For<ILogger>();
        var gitHub = Substitute.For<IGitHubClient>();
        var graphQL = Substitute.For<IGitHubGraphQlClient>();
        var repository = new RepositoryInfo(123, "test-repo", "test-owner", "test-owner/test-repo");
        var installation = new InstallationInfo(456, "test-account");

        return new ProbotSharpContext(
            "delivery-123",
            "issue_comment",
            "created",
            payload,
            logger,
            gitHub,
            graphQL,
            repository,
            installation);
    }

    private static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        return services.BuildServiceProvider();
    }

    private static IServiceProvider CreateServiceProviderWithHandler<THandler>(ISlashCommandHandler handlerInstance)
        where THandler : class, ISlashCommandHandler
    {
        var services = new ServiceCollection();
        // Create an instance of THandler that delegates to the substitute
        // This works because TestCommandHandler accepts an ISlashCommandHandler in its constructor
        services.AddSingleton<THandler>(_ => (THandler)Activator.CreateInstance(typeof(THandler), handlerInstance)!);
        return services.BuildServiceProvider();
    }

    // Test handler implementations
    private class TestCommandHandler : ISlashCommandHandler
    {
        private readonly ISlashCommandHandler? _inner;

        public TestCommandHandler(ISlashCommandHandler? inner = null)
        {
            this._inner = inner;
        }

        public Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken cancellationToken = default)
        {
            return this._inner?.HandleAsync(context, command, cancellationToken) ?? Task.CompletedTask;
        }
    }

    private class AnotherTestCommandHandler : ISlashCommandHandler
    {
        private readonly ISlashCommandHandler? _inner;

        public AnotherTestCommandHandler(ISlashCommandHandler? inner = null)
        {
            this._inner = inner;
        }

        public Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken cancellationToken = default)
        {
            return this._inner?.HandleAsync(context, command, cancellationToken) ?? Task.CompletedTask;
        }
    }
}
