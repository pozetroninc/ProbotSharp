// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NSubstitute;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Context;
using ProbotSharp.Domain.Contracts;

namespace ProbotSharp.Application.Tests.Services;

public class EventRouterTests
{
    private readonly ILogger<EventRouter> _logger = Substitute.For<ILogger<EventRouter>>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IServiceScope _scope = Substitute.For<IServiceScope>();
    private readonly IServiceProvider _scopedServiceProvider = Substitute.For<IServiceProvider>();

    public EventRouterTests()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.Returns(_scopedServiceProvider);
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
    }

    private EventRouter CreateSut() => new(_logger);

    private static ProbotSharpContext CreateContext(string eventName, string? action = null)
    {
        var payload = new JObject { ["test"] = "data" };
        var logger = Substitute.For<ILogger>();
        var gitHub = Substitute.For<Octokit.IGitHubClient>();
        var graphQL = Substitute.For<IGitHubGraphQlClient>();

        return new ProbotSharpContext(
            id: "test-delivery-id",
            eventName: eventName,
            eventAction: action,
            payload: payload,
            logger: logger,
            gitHub: gitHub,
            graphQL: graphQL,
            repository: null,
            installation: null);
    }

    [Fact]
    public void RegisterHandler_WithValidParameters_ShouldSucceed()
    {
        var sut = CreateSut();

        var act = () => sut.RegisterHandler("issues", "opened", typeof(TestHandler));

        act.Should().NotThrow();
        sut.HandlerCount.Should().Be(1);
    }

    [Fact]
    public void RegisterHandler_WithNullEventName_ShouldThrow()
    {
        var sut = CreateSut();

        var act = () => sut.RegisterHandler(null!, "opened", typeof(TestHandler));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RegisterHandler_WithNullHandlerType_ShouldThrow()
    {
        var sut = CreateSut();

        var act = () => sut.RegisterHandler("issues", "opened", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisterHandler_WithNonHandlerType_ShouldThrow()
    {
        var sut = CreateSut();

        var act = () => sut.RegisterHandler("issues", "opened", typeof(string));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*must implement IEventHandler*");
    }

    [Fact]
    public async Task RouteAsync_WithMatchingExactHandler_ShouldExecuteHandler()
    {
        var sut = CreateSut();
        var handler = Substitute.For<IEventHandler>();
        sut.RegisterHandler("issues", "opened", typeof(TestHandler));

        _scopedServiceProvider.GetService(typeof(TestHandler)).Returns(handler);

        var context = CreateContext("issues", "opened");
        await sut.RouteAsync(context, _serviceProvider, CancellationToken.None);

        await handler.Received(1).HandleAsync(
            Arg.Is<ProbotSharpContext>(c => c.EventName == "issues" && c.EventAction == "opened"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RouteAsync_WithWildcardEventHandler_ShouldExecuteForAllEvents()
    {
        var sut = CreateSut();
        var handler = Substitute.For<IEventHandler>();
        sut.RegisterHandler("*", null, typeof(TestHandler));

        _scopedServiceProvider.GetService(typeof(TestHandler)).Returns(handler);

        var context = CreateContext("issues", "opened");
        await sut.RouteAsync(context, _serviceProvider, CancellationToken.None);

        await handler.Received(1).HandleAsync(Arg.Any<ProbotSharpContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RouteAsync_WithEventWildcardActionHandler_ShouldExecuteForAllActions()
    {
        var sut = CreateSut();
        var handler = Substitute.For<IEventHandler>();
        sut.RegisterHandler("issues.*", null, typeof(TestHandler));

        _scopedServiceProvider.GetService(typeof(TestHandler)).Returns(handler);

        var context = CreateContext("issues", "opened");
        await sut.RouteAsync(context, _serviceProvider, CancellationToken.None);

        await handler.Received(1).HandleAsync(
            Arg.Is<ProbotSharpContext>(c => c.EventName == "issues"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RouteAsync_WithActionWildcardHandler_ShouldExecuteForAllActions()
    {
        var sut = CreateSut();
        var handler = Substitute.For<IEventHandler>();
        sut.RegisterHandler("issues", "*", typeof(TestHandler));

        _scopedServiceProvider.GetService(typeof(TestHandler)).Returns(handler);

        var context = CreateContext("issues", "opened");
        await sut.RouteAsync(context, _serviceProvider, CancellationToken.None);

        await handler.Received(1).HandleAsync(Arg.Any<ProbotSharpContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RouteAsync_WithNullActionHandler_ShouldMatchAnyAction()
    {
        var sut = CreateSut();
        var handler = Substitute.For<IEventHandler>();
        sut.RegisterHandler("issues", null, typeof(TestHandler));

        _scopedServiceProvider.GetService(typeof(TestHandler)).Returns(handler);

        var context = CreateContext("issues", "opened");
        await sut.RouteAsync(context, _serviceProvider, CancellationToken.None);

        await handler.Received(1).HandleAsync(Arg.Any<ProbotSharpContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RouteAsync_WithNoMatchingHandlers_ShouldNotExecuteAnything()
    {
        var sut = CreateSut();
        var handler = Substitute.For<IEventHandler>();
        sut.RegisterHandler("pull_request", "opened", typeof(TestHandler));

        _scopedServiceProvider.GetService(typeof(TestHandler)).Returns(handler);

        var context = CreateContext("issues", "opened");
        await sut.RouteAsync(context, _serviceProvider, CancellationToken.None);

        await handler.DidNotReceive().HandleAsync(Arg.Any<ProbotSharpContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RouteAsync_WithMultipleMatchingHandlers_ShouldExecuteAll()
    {
        var sut = CreateSut();
        var handler1 = Substitute.For<IEventHandler>();
        var handler2 = Substitute.For<IEventHandler>();

        sut.RegisterHandler("issues", "opened", typeof(TestHandler));
        sut.RegisterHandler("issues", "*", typeof(AnotherTestHandler));

        _scopedServiceProvider.GetService(typeof(TestHandler)).Returns(handler1);
        _scopedServiceProvider.GetService(typeof(AnotherTestHandler)).Returns(handler2);

        var context = CreateContext("issues", "opened");
        await sut.RouteAsync(context, _serviceProvider, CancellationToken.None);

        await handler1.Received(1).HandleAsync(Arg.Any<ProbotSharpContext>(), Arg.Any<CancellationToken>());
        await handler2.Received(1).HandleAsync(Arg.Any<ProbotSharpContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RouteAsync_WhenHandlerThrows_ShouldContinueWithOtherHandlers()
    {
        var sut = CreateSut();
        var handler1 = Substitute.For<IEventHandler>();
        var handler2 = Substitute.For<IEventHandler>();

        handler1.HandleAsync(Arg.Any<ProbotSharpContext>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("Test exception"));

        sut.RegisterHandler("issues", "opened", typeof(TestHandler));
        sut.RegisterHandler("issues", "opened", typeof(AnotherTestHandler));

        _scopedServiceProvider.GetService(typeof(TestHandler)).Returns(handler1);
        _scopedServiceProvider.GetService(typeof(AnotherTestHandler)).Returns(handler2);

        var context = CreateContext("issues", "opened");
        var act = async () => await sut.RouteAsync(context, _serviceProvider, CancellationToken.None);

        await act.Should().NotThrowAsync();
        await handler2.Received(1).HandleAsync(Arg.Any<ProbotSharpContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RouteAsync_WhenCancelled_ShouldThrowOperationCancelledException()
    {
        var sut = CreateSut();
        var handler = Substitute.For<IEventHandler>();
        var cts = new CancellationTokenSource();

        handler.HandleAsync(Arg.Any<ProbotSharpContext>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new OperationCanceledException());

        sut.RegisterHandler("issues", "opened", typeof(TestHandler));
        _scopedServiceProvider.GetService(typeof(TestHandler)).Returns(handler);

        var context = CreateContext("issues", "opened");
        cts.Cancel();

        var act = async () => await sut.RouteAsync(context, _serviceProvider, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RouteAsync_WithCaseInsensitiveEventName_ShouldMatch()
    {
        var sut = CreateSut();
        var handler = Substitute.For<IEventHandler>();
        sut.RegisterHandler("ISSUES", "opened", typeof(TestHandler));

        _scopedServiceProvider.GetService(typeof(TestHandler)).Returns(handler);

        var context = CreateContext("issues", "opened");
        await sut.RouteAsync(context, _serviceProvider, CancellationToken.None);

        await handler.Received(1).HandleAsync(Arg.Any<ProbotSharpContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RouteAsync_WithCaseInsensitiveAction_ShouldMatch()
    {
        var sut = CreateSut();
        var handler = Substitute.For<IEventHandler>();
        sut.RegisterHandler("issues", "OPENED", typeof(TestHandler));

        _scopedServiceProvider.GetService(typeof(TestHandler)).Returns(handler);

        var context = CreateContext("issues", "opened");
        await sut.RouteAsync(context, _serviceProvider, CancellationToken.None);

        await handler.Received(1).HandleAsync(Arg.Any<ProbotSharpContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RouteAsync_WithEventWithoutAction_ShouldMatchNullActionHandler()
    {
        var sut = CreateSut();
        var handler = Substitute.For<IEventHandler>();
        sut.RegisterHandler("push", null, typeof(TestHandler));

        _scopedServiceProvider.GetService(typeof(TestHandler)).Returns(handler);

        var context = CreateContext("push", null);
        await sut.RouteAsync(context, _serviceProvider, CancellationToken.None);

        await handler.Received(1).HandleAsync(
            Arg.Is<ProbotSharpContext>(c => c.EventName == "push" && c.EventAction == null),
            Arg.Any<CancellationToken>());
    }

    // Test handler classes
    private class TestHandler : IEventHandler
    {
        public Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private class AnotherTestHandler : IEventHandler
    {
        public Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
