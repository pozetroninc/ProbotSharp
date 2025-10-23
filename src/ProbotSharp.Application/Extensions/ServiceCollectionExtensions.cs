// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Abstractions.Commands;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Application.EventHandlers;
using ProbotSharp.Application.Services;

#pragma warning disable CA1848 // Performance: LoggerMessage delegates - not performance-critical for this codebase

namespace ProbotSharp.Application.Extensions;

/// <summary>
/// Extension methods for registering Probot event handlers and routing services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the EventRouter and discovers/registers all event handlers from the specified assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan for event handlers.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or assembly is null.</exception>
    public static IServiceCollection AddProbotHandlers(
        this IServiceCollection services,
        Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        return services.AddProbotHandlers([assembly]);
    }

    /// <summary>
    /// Registers the EventRouter and discovers/registers all event handlers from multiple assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for event handlers.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or assemblies is null.</exception>
    public static IServiceCollection AddProbotHandlers(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        // Discover all handlers from provided assemblies
        var handlers = EventHandlerDiscovery.DiscoverHandlers(assemblies).ToList();

        // Register each handler type as scoped (new instance per request)
        foreach (var (handlerType, _) in handlers)
        {
            services.AddScoped(handlerType);
        }

        // Register EventRouter as singleton (shared across all requests)
        services.AddSingleton<EventRouter>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<EventRouter>>();
            var router = new EventRouter(logger);

            var handlerCount = 0;

            // Register handlers with router
            foreach (var (handlerType, attributes) in handlers)
            {
                // Register handler with router for each attribute
                foreach (var attribute in attributes)
                {
                    router.RegisterHandler(attribute.EventName, attribute.Action, handlerType);
                    handlerCount++;
                }
            }

            logger.LogInformation(
                "Registered {HandlerCount} event handler(s) from {AssemblyCount} assembly(ies)",
                handlerCount,
                assemblies.Length);

            return router;
        });

        return services;
    }

    /// <summary>
    /// Registers the EventRouter without any handlers.
    /// Handlers must be registered manually using <see cref="EventRouter.RegisterHandler"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    public static IServiceCollection AddEventRouter(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<EventRouter>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<EventRouter>>();
            return new EventRouter(logger);
        });

        return services;
    }

    /// <summary>
    /// Discovers and loads Probot apps from the specified assembly path.
    /// This method performs the configuration phase (calling ConfigureAsync on each app).
    /// Apps will be initialized (InitializeAsync) after the service provider is built.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="assemblyPath">Path to the assembly containing Probot apps. If null, uses the entry assembly.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or configuration is null.</exception>
    public static async Task<IServiceCollection> AddProbotAppsAsync(
        this IServiceCollection services,
        IConfiguration configuration,
        string? assemblyPath = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Use entry assembly if no path specified
        if (string.IsNullOrWhiteSpace(assemblyPath))
        {
            assemblyPath = Assembly.GetEntryAssembly()?.Location
                ?? throw new InvalidOperationException("Could not determine entry assembly location");
        }

        // Register ProbotAppLoader
        services.AddSingleton<ProbotAppLoader>();

        // Build a temporary service provider for logging during app loading
        using var tempServiceProvider = services.BuildServiceProvider();
        var loggerFactory = tempServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<ProbotAppLoader>();

        var appLoader = new ProbotAppLoader(logger);

        // Discover apps from assembly
        var appTypes = appLoader.DiscoverApps(assemblyPath).ToList();

        if (appTypes.Count == 0)
        {
            logger.LogWarning(
                "No Probot apps found in assembly: {AssemblyPath}",
                assemblyPath);
            return services;
        }

        // Load and configure each app
        var loadedApps = new List<IProbotApp>();
        foreach (var appType in appTypes)
        {
            var app = await appLoader.LoadAppAsync(appType, tempServiceProvider).ConfigureAwait(false);
            await appLoader.ConfigureAppAsync(app, services, configuration).ConfigureAwait(false);
            loadedApps.Add(app);
        }

        // Store loaded apps for initialization phase
        services.AddSingleton<IReadOnlyList<IProbotApp>>(loadedApps);

        // Register the hosted service that will initialize apps after DI container is built
        services.AddHostedService<ProbotAppInitializer>();

        // Register HttpRouteConfigurator for configuring custom HTTP routes
        services.AddSingleton<HttpRouteConfigurator>();

        return services;
    }

    /// <summary>
    /// Registers the SlashCommandRouter and discovers/registers all slash command handlers from a single assembly.
    /// Also registers the SlashCommandEventHandler to automatically process commands from issue/PR comments.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan for slash command handlers.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or assembly is null.</exception>
    public static IServiceCollection AddSlashCommands(
        this IServiceCollection services,
        Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        return services.AddSlashCommands([assembly]);
    }

    /// <summary>
    /// Registers the SlashCommandRouter and discovers/registers all slash command handlers from multiple assemblies.
    /// Also registers the SlashCommandEventHandler to automatically process commands from issue/PR comments.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for slash command handlers.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or assemblies is null.</exception>
    public static IServiceCollection AddSlashCommands(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        // Discover all slash command handlers from provided assemblies
        var handlers = SlashCommandHandlerDiscovery.DiscoverHandlers(assemblies).ToList();

        // Register each handler type as scoped (new instance per request)
        foreach (var (handlerType, _) in handlers)
        {
            services.AddScoped(handlerType);
        }

        // Register SlashCommandRouter as singleton (shared across all requests)
        services.AddSingleton<SlashCommandRouter>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SlashCommandRouter>>();
            var router = new SlashCommandRouter(logger);

            var registrationCount = 0;

            // Register handlers with router
            foreach (var (handlerType, commandNames) in handlers)
            {
                // Register handler with router for each command name
                foreach (var commandName in commandNames)
                {
                    router.RegisterHandler(commandName, handlerType);
                    registrationCount++;
                }
            }

            logger.LogInformation(
                "Registered {HandlerCount} slash command handler(s) with {RegistrationCount} command mapping(s) from {AssemblyCount} assembly(ies)",
                handlers.Count,
                registrationCount,
                assemblies.Length);

            return router;
        });

        // Register SlashCommandEventHandler as scoped to automatically process commands from comments
        services.AddScoped<SlashCommandEventHandler>();

        return services;
    }
}

#pragma warning restore CA1848
