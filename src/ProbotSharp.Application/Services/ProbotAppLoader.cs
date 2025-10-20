// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Abstractions;

#pragma warning disable CA1848 // Performance: LoggerMessage delegates - not performance-critical for this codebase

namespace ProbotSharp.Application.Services;

/// <summary>
/// Discovers and loads Probot applications from assemblies.
/// Responsible for finding implementations of <see cref="IProbotApp"/>,
/// instantiating them, and orchestrating their lifecycle.
/// </summary>
public sealed class ProbotAppLoader
{
    private readonly ILogger<ProbotAppLoader> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProbotAppLoader"/> class.
    /// </summary>
    /// <param name="logger">Logger for app loading operations.</param>
    public ProbotAppLoader(ILogger<ProbotAppLoader> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        this._logger = logger;
    }

    /// <summary>
    /// Discovers all implementations of <see cref="IProbotApp"/> in the specified assembly.
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly to scan for apps.</param>
    /// <returns>Enumerable of app types found in the assembly.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the assembly file is not found.</exception>
    /// <exception cref="BadImageFormatException">Thrown when the assembly file is invalid.</exception>
    public IEnumerable<Type> DiscoverApps(string assemblyPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);

        this._logger.LogDebug("Discovering Probot apps in assembly: {AssemblyPath}", assemblyPath);

        Assembly assembly;
        try
        {
            // Load the assembly
            assembly = Assembly.LoadFrom(assemblyPath);
        }
        catch (FileNotFoundException ex)
        {
            this._logger.LogError(ex, "Assembly not found: {AssemblyPath}", assemblyPath);
            throw;
        }
        catch (BadImageFormatException ex)
        {
            this._logger.LogError(ex, "Invalid assembly format: {AssemblyPath}", assemblyPath);
            throw;
        }

        // Find all types that implement IProbotApp
        var appTypes = assembly.GetTypes()
            .Where(t => typeof(IProbotApp).IsAssignableFrom(t) &&
                       t is { IsClass: true, IsAbstract: false })
            .ToList();

        this._logger.LogInformation(
            "Discovered {AppCount} Probot app(s) in assembly {AssemblyName}",
            appTypes.Count,
            assembly.GetName().Name);

        foreach (var appType in appTypes)
        {
            this._logger.LogDebug("Found app type: {AppType}", appType.FullName);
        }

        return appTypes;
    }

    /// <summary>
    /// Loads and instantiates a Probot app from its type.
    /// </summary>
    /// <param name="appType">The type of the app to load.</param>
    /// <param name="serviceProvider">Service provider for resolving dependencies.</param>
    /// <returns>The instantiated app instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the app cannot be instantiated.</exception>
    public Task<IProbotApp> LoadAppAsync(Type appType, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(appType);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        this._logger.LogDebug("Loading app type: {AppType}", appType.FullName);

        try
        {
            // Try to create instance using DI first (in case the app has dependencies)
            IProbotApp? app;
            try
            {
                app = ActivatorUtilities.CreateInstance(serviceProvider, appType) as IProbotApp;
            }
            catch
            {
                // Fall back to parameterless constructor
                app = Activator.CreateInstance(appType) as IProbotApp;
            }

            if (app == null)
            {
                throw new InvalidOperationException(
                    $"Failed to create instance of app type {appType.FullName}");
            }

            this._logger.LogInformation(
                "Successfully loaded app: {AppName} v{Version}",
                app.Name,
                app.Version);

            return Task.FromResult(app);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to instantiate app type {AppType}: {ErrorMessage}",
                appType.FullName,
                ex.Message);
            throw new InvalidOperationException(
                $"Failed to instantiate app type {appType.FullName}",
                ex);
        }
    }

    /// <summary>
    /// Configures services for a Probot app.
    /// </summary>
    /// <param name="app">The app to configure.</param>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ConfigureAppAsync(
        IProbotApp app,
        IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        this._logger.LogDebug(
            "Configuring services for app: {AppName} v{Version}",
            app.Name,
            app.Version);

        try
        {
            await app.ConfigureAsync(services, configuration).ConfigureAwait(false);

            this._logger.LogInformation(
                "Successfully configured services for app: {AppName}",
                app.Name);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to configure services for app {AppName}: {ErrorMessage}",
                app.Name,
                ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Initializes a Probot app after the DI container is built.
    /// </summary>
    /// <param name="app">The app to initialize.</param>
    /// <param name="router">The event router for registering handlers.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAppAsync(
        IProbotApp app,
        EventRouter router,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        this._logger.LogDebug(
            "Initializing app: {AppName} v{Version}",
            app.Name,
            app.Version);

        try
        {
            await app.InitializeAsync(router, serviceProvider).ConfigureAwait(false);

            this._logger.LogInformation(
                "Successfully initialized app: {AppName}",
                app.Name);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to initialize app {AppName}: {ErrorMessage}",
                app.Name,
                ex.Message);
            throw;
        }
    }
}

#pragma warning restore CA1848
