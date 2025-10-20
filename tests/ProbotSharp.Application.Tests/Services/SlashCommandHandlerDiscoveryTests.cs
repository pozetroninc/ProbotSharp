// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Reflection;

using ProbotSharp.Application.Abstractions.Commands;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Commands;
using ProbotSharp.Domain.Context;

namespace ProbotSharp.Application.Tests.Services;

public class SlashCommandHandlerDiscoveryTests
{
    [Fact]
    public void DiscoverHandlers_WithSingleAssembly_ShouldDiscoverAttributedHandlers()
    {
        // Arrange
        var assembly = typeof(SlashCommandHandlerDiscoveryTests).Assembly;

        // Act
        var handlers = SlashCommandHandlerDiscovery.DiscoverHandlers(assembly).ToList();

        // Assert
        handlers.Should().NotBeEmpty();
        handlers.Should().Contain(h => h.HandlerType == typeof(TestDiscoveryHandler));
    }

    [Fact]
    public void DiscoverHandlers_WithMultipleAssemblies_ShouldDiscoverFromAll()
    {
        // Arrange
        var assemblies = new[]
        {
            typeof(SlashCommandHandlerDiscoveryTests).Assembly,
            typeof(SlashCommandRouter).Assembly // Application assembly
        };

        // Act
        var handlers = SlashCommandHandlerDiscovery.DiscoverHandlers(assemblies).ToList();

        // Assert
        handlers.Should().NotBeEmpty();
    }

    [Fact]
    public void DiscoverHandlers_ShouldReturnCorrectCommandNames()
    {
        // Arrange
        var assembly = typeof(SlashCommandHandlerDiscoveryTests).Assembly;

        // Act
        var handlers = SlashCommandHandlerDiscovery.DiscoverHandlers(assembly).ToList();

        // Assert
        var testHandler = handlers.FirstOrDefault(h => h.HandlerType == typeof(TestDiscoveryHandler));
        testHandler.Should().NotBeNull();
        testHandler.Commands.Should().Contain("test");
    }

    [Fact]
    public void DiscoverHandlers_WithMultipleCommandsPerHandler_ShouldReturnAllCommands()
    {
        // Arrange
        var assembly = typeof(SlashCommandHandlerDiscoveryTests).Assembly;

        // Act
        var handlers = SlashCommandHandlerDiscovery.DiscoverHandlers(assembly).ToList();

        // Assert
        var multiHandler = handlers.FirstOrDefault(h => h.HandlerType == typeof(MultiCommandHandler));
        multiHandler.Should().NotBeNull();
        multiHandler.Commands.Should().HaveCount(2);
        multiHandler.Commands.Should().Contain("cmd1");
        multiHandler.Commands.Should().Contain("cmd2");
    }

    [Fact]
    public void DiscoverHandlers_ShouldIgnoreNonAttributedClasses()
    {
        // Arrange
        var assembly = typeof(SlashCommandHandlerDiscoveryTests).Assembly;

        // Act
        var handlers = SlashCommandHandlerDiscovery.DiscoverHandlers(assembly).ToList();

        // Assert
        handlers.Should().NotContain(h => h.HandlerType == typeof(NonAttributedHandler));
    }

    [Fact]
    public void DiscoverHandlers_ShouldIgnoreAbstractClasses()
    {
        // Arrange
        var assembly = typeof(SlashCommandHandlerDiscoveryTests).Assembly;

        // Act
        var handlers = SlashCommandHandlerDiscovery.DiscoverHandlers(assembly).ToList();

        // Assert
        handlers.Should().NotContain(h => h.HandlerType == typeof(AbstractTestHandler));
    }

    [Fact]
    public void DiscoverHandlers_ShouldIgnoreInterfaces()
    {
        // Arrange
        var assembly = typeof(SlashCommandHandlerDiscoveryTests).Assembly;

        // Act
        var handlers = SlashCommandHandlerDiscovery.DiscoverHandlers(assembly).ToList();

        // Assert
        handlers.Should().NotContain(h => h.HandlerType == typeof(ISlashCommandHandler));
    }

    [Fact]
    public void DiscoverHandlers_ShouldIgnoreNonHandlerClasses()
    {
        // Arrange
        var assembly = typeof(SlashCommandHandlerDiscoveryTests).Assembly;

        // Act
        var handlers = SlashCommandHandlerDiscovery.DiscoverHandlers(assembly).ToList();

        // Assert
        handlers.Should().NotContain(h => h.HandlerType == typeof(NotAHandler));
    }

    [Fact]
    public void DiscoverHandlers_WithNullAssembly_ShouldThrowArgumentNullException()
    {
        // Arrange
        Assembly assembly = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SlashCommandHandlerDiscovery.DiscoverHandlers(assembly));
    }

    [Fact]
    public void DiscoverHandlers_WithNullAssemblyArray_ShouldThrowArgumentNullException()
    {
        // Arrange
        Assembly[] assemblies = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SlashCommandHandlerDiscovery.DiscoverHandlers(assemblies));
    }

    [Fact]
    public void DiscoverHandlers_WithEmptyAssemblyArray_ShouldReturnEmpty()
    {
        // Arrange
        var assemblies = Array.Empty<Assembly>();

        // Act
        var handlers = SlashCommandHandlerDiscovery.DiscoverHandlers(assemblies).ToList();

        // Assert
        handlers.Should().BeEmpty();
    }

    // Test handler classes for discovery
    [SlashCommandHandler("test")]
    private class TestDiscoveryHandler : ISlashCommandHandler
    {
        public Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    [SlashCommandHandler("cmd1")]
    [SlashCommandHandler("cmd2")]
    private class MultiCommandHandler : ISlashCommandHandler
    {
        public Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    // Should be ignored - no attribute
    private class NonAttributedHandler : ISlashCommandHandler
    {
        public Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    // Should be ignored - abstract
    [SlashCommandHandler("abstract")]
    private abstract class AbstractTestHandler : ISlashCommandHandler
    {
        public abstract Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken cancellationToken = default);
    }

    // Should be ignored - not a handler
    [SlashCommandHandler("nothandler")]
    private class NotAHandler
    {
        public Task DoSomething()
        {
            return Task.CompletedTask;
        }
    }
}
