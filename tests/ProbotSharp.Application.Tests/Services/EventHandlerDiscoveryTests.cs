// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Reflection;

using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Context;

namespace ProbotSharp.Application.Tests.Services;

public class EventHandlerDiscoveryTests
{
    [Fact]
    public void DiscoverHandlers_WithSingleAttribute_ShouldReturnHandler()
    {
        var assembly = typeof(EventHandlerDiscoveryTests).Assembly;

        var handlers = EventHandlerDiscovery.DiscoverHandlers(assembly).ToList();

        handlers.Should().ContainSingle(h =>
            h.HandlerType == typeof(SingleAttributeHandler) &&
            h.Attributes.Length == 1 &&
            h.Attributes[0].EventName == "issues" &&
            h.Attributes[0].Action == "opened");
    }

    [Fact]
    public void DiscoverHandlers_WithMultipleAttributes_ShouldReturnAllAttributes()
    {
        var assembly = typeof(EventHandlerDiscoveryTests).Assembly;

        var handlers = EventHandlerDiscovery.DiscoverHandlers(assembly).ToList();

        var multiAttributeHandler = handlers.FirstOrDefault(h => h.HandlerType == typeof(MultiAttributeHandler));
        multiAttributeHandler.Should().NotBe(default);
        multiAttributeHandler.Attributes.Should().HaveCount(2);
        multiAttributeHandler.Attributes.Should().Contain(a => a.EventName == "issues" && a.Action == "opened");
        multiAttributeHandler.Attributes.Should().Contain(a => a.EventName == "issues" && a.Action == "closed");
    }

    [Fact]
    public void DiscoverHandlers_WithNoAttributes_ShouldNotReturnHandler()
    {
        var assembly = typeof(EventHandlerDiscoveryTests).Assembly;

        var handlers = EventHandlerDiscovery.DiscoverHandlers(assembly).ToList();

        handlers.Should().NotContain(h => h.HandlerType == typeof(NoAttributeHandler));
    }

    [Fact]
    public void DiscoverHandlers_WithAbstractClass_ShouldNotReturnHandler()
    {
        var assembly = typeof(EventHandlerDiscoveryTests).Assembly;

        var handlers = EventHandlerDiscovery.DiscoverHandlers(assembly).ToList();

        handlers.Should().NotContain(h => h.HandlerType == typeof(AbstractHandler));
    }

    [Fact]
    public void DiscoverHandlers_WithNonPublicClass_ShouldNotReturnHandler()
    {
        var assembly = typeof(EventHandlerDiscoveryTests).Assembly;

        var handlers = EventHandlerDiscovery.DiscoverHandlers(assembly).ToList();

        handlers.Should().NotContain(h => h.HandlerType == typeof(InternalHandler));
    }

    [Fact]
    public void DiscoverHandlers_WithWildcardAttribute_ShouldReturnHandler()
    {
        var assembly = typeof(EventHandlerDiscoveryTests).Assembly;

        var handlers = EventHandlerDiscovery.DiscoverHandlers(assembly).ToList();

        handlers.Should().Contain(h =>
            h.HandlerType == typeof(WildcardHandler) &&
            h.Attributes[0].EventName == "*");
    }

    [Fact]
    public void DiscoverHandlers_WithNullAssembly_ShouldThrow()
    {
        var act = () => EventHandlerDiscovery.DiscoverHandlers((Assembly)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DiscoverHandlers_WithMultipleAssemblies_ShouldReturnHandlersFromAll()
    {
        var assembly1 = typeof(EventHandlerDiscoveryTests).Assembly;
        var assembly2 = typeof(IEventHandler).Assembly;

        var handlers = EventHandlerDiscovery.DiscoverHandlers(assembly1, assembly2).ToList();

        // Should find handlers from both assemblies (at least from test assembly)
        handlers.Should().Contain(h => h.HandlerType == typeof(SingleAttributeHandler));
    }

    // Test handler classes
    [EventHandler("issues", "opened")]
    public class SingleAttributeHandler : IEventHandler
    {
        public Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    [EventHandler("issues", "opened")]
    [EventHandler("issues", "closed")]
    public class MultiAttributeHandler : IEventHandler
    {
        public Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public class NoAttributeHandler : IEventHandler
    {
        public Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    [EventHandler("issues", "opened")]
    public abstract class AbstractHandler : IEventHandler
    {
        public abstract Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default);
    }

    [EventHandler("issues", "opened")]
    internal class InternalHandler : IEventHandler
    {
        public Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    [EventHandler("*")]
    public class WildcardHandler : IEventHandler
    {
        public Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
