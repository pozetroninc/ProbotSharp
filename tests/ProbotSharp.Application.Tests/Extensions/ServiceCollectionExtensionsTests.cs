// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Abstractions.Commands;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Application.EventHandlers;
using ProbotSharp.Application.Extensions;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Commands;
using ProbotSharp.Domain.Context;

namespace ProbotSharp.Application.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddProbotHandlers_WithNullServices_ShouldThrow()
    {
        // Arrange
        IServiceCollection? services = null;
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var act = () => services!.AddProbotHandlers(assembly);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddProbotHandlers_WithNullAssembly_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        Assembly? assembly = null;

        // Act
        var act = () => services.AddProbotHandlers(assembly!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddProbotHandlers_WithValidAssembly_ShouldRegisterEventRouter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var assembly = typeof(TestEventHandler).Assembly;

        // Act
        services.AddProbotHandlers(assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var router = provider.GetService<EventRouter>();
        router.Should().NotBeNull();
    }

    [Fact]
    public void AddProbotHandlers_WithMultipleAssemblies_ShouldRegisterHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var assembly1 = typeof(TestEventHandler).Assembly;
        var assembly2 = Assembly.GetExecutingAssembly();

        // Act
        services.AddProbotHandlers(assembly1, assembly2);
        var provider = services.BuildServiceProvider();

        // Assert
        var router = provider.GetService<EventRouter>();
        router.Should().NotBeNull();

        // Verify handler was registered
        var handler = provider.GetService<TestEventHandler>();
        handler.Should().NotBeNull();
    }

    [Fact]
    public void AddProbotHandlers_WithNullServicesArray_ShouldThrow()
    {
        // Arrange
        IServiceCollection? services = null;
        var assemblies = new[] { Assembly.GetExecutingAssembly() };

        // Act
        var act = () => services!.AddProbotHandlers(assemblies);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddProbotHandlers_WithNullAssembliesArray_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        Assembly[]? assemblies = null;

        // Act
        var act = () => services.AddProbotHandlers(assemblies!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddEventRouter_WithNullServices_ShouldThrow()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddEventRouter();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddEventRouter_WithValidServices_ShouldRegisterRouter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEventRouter();
        var provider = services.BuildServiceProvider();

        // Assert
        var router = provider.GetService<EventRouter>();
        router.Should().NotBeNull();
    }

    [Fact]
    public async Task AddProbotAppsAsync_WithNullServices_ShouldThrow()
    {
        // Arrange
        IServiceCollection? services = null;
        var configuration = new ConfigurationBuilder().Build();

        // Act
        Func<Task> act = async () => await services!.AddProbotAppsAsync(configuration);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AddProbotAppsAsync_WithNullConfiguration_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration? configuration = null;

        // Act
        Func<Task> act = async () => await services.AddProbotAppsAsync(configuration!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AddProbotAppsAsync_WithNoAppsFound_ShouldReturnServicesWithoutError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();
        var assemblyPath = Assembly.GetExecutingAssembly().Location;

        // Act
        var result = await services.AddProbotAppsAsync(configuration, assemblyPath);

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddSlashCommands_WithNullServices_ShouldThrow()
    {
        // Arrange
        IServiceCollection? services = null;
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var act = () => services!.AddSlashCommands(assembly);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddSlashCommands_WithNullAssembly_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        Assembly? assembly = null;

        // Act
        var act = () => services.AddSlashCommands(assembly!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddSlashCommands_WithValidAssembly_ShouldRegisterRouter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var assembly = typeof(TestSlashCommandHandler).Assembly;

        // Act
        services.AddSlashCommands(assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var router = provider.GetService<SlashCommandRouter>();
        router.Should().NotBeNull();

        var handler = provider.GetService<SlashCommandEventHandler>();
        handler.Should().NotBeNull();
    }

    [Fact]
    public void AddSlashCommands_WithMultipleAssemblies_ShouldRegisterHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var assembly1 = typeof(TestSlashCommandHandler).Assembly;
        var assembly2 = Assembly.GetExecutingAssembly();

        // Act
        services.AddSlashCommands(assembly1, assembly2);
        var provider = services.BuildServiceProvider();

        // Assert
        var router = provider.GetService<SlashCommandRouter>();
        router.Should().NotBeNull();

        // Verify handler was registered
        var handler = provider.GetService<TestSlashCommandHandler>();
        handler.Should().NotBeNull();
    }

    [Fact]
    public void AddSlashCommands_WithNullServicesArray_ShouldThrow()
    {
        // Arrange
        IServiceCollection? services = null;
        var assemblies = new[] { Assembly.GetExecutingAssembly() };

        // Act
        var act = () => services!.AddSlashCommands(assemblies);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddSlashCommands_WithNullAssembliesArray_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        Assembly[]? assemblies = null;

        // Act
        var act = () => services.AddSlashCommands(assemblies!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}

[EventHandler("test", "action")]
public class TestEventHandler : IEventHandler
{
    public Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

[SlashCommandHandler("test")]
public class TestSlashCommandHandler : ISlashCommandHandler
{
    public Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
