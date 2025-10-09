// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Abstractions;

namespace ProbotSharp.Application.Services;

/// <summary>
/// Initializes loaded Probot apps on application startup.
/// Calls InitializeAsync on each app after the DI container is fully built.
/// </summary>
public sealed class ProbotAppInitializer : IHostedService
{
    private readonly IReadOnlyList<IProbotApp> _apps;
    private readonly EventRouter _router;
    private readonly IServiceProvider _serviceProvider;
    private readonly ProbotAppLoader _appLoader;
    private readonly ILogger<ProbotAppInitializer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProbotAppInitializer"/> class.
    /// </summary>
    /// <param name="apps">The list of loaded apps to initialize.</param>
    /// <param name="router">The event router for handler registration.</param>
    /// <param name="serviceProvider">The service provider for dependency resolution.</param>
    /// <param name="appLoader">The app loader for initialization operations.</param>
    /// <param name="logger">Logger for initialization operations.</param>
    public ProbotAppInitializer(
        IReadOnlyList<IProbotApp> apps,
        EventRouter router,
        IServiceProvider serviceProvider,
        ProbotAppLoader appLoader,
        ILogger<ProbotAppInitializer> logger)
    {
        ArgumentNullException.ThrowIfNull(apps);
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(appLoader);
        ArgumentNullException.ThrowIfNull(logger);

        _apps = apps;
        _router = router;
        _serviceProvider = serviceProvider;
        _appLoader = appLoader;
        _logger = logger;
    }

    /// <summary>
    /// Initializes all loaded apps when the host starts.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing {AppCount} Probot app(s)...", _apps.Count);

        foreach (var app in _apps)
        {
            try
            {
                await _appLoader.InitializeAppAsync(app, _router, _serviceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to initialize app {AppName}: {ErrorMessage}",
                    app.Name,
                    ex.Message);
                throw;
            }
        }

        _logger.LogInformation("Successfully initialized all Probot apps");
    }

    /// <summary>
    /// Called when the host is stopping (no cleanup needed for apps).
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Probot app initializer shutting down");
        return Task.CompletedTask;
    }
}
