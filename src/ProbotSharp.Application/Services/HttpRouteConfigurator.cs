// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Abstractions;

namespace ProbotSharp.Application.Services;

/// <summary>
/// Configures HTTP routes for all loaded Probot apps.
/// This service is called after the ASP.NET Core application is built to register custom endpoints.
/// </summary>
public class HttpRouteConfigurator
{
    private readonly IReadOnlyList<IProbotApp> _apps;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HttpRouteConfigurator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRouteConfigurator"/> class.
    /// </summary>
    /// <param name="apps">The list of loaded Probot apps.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="logger">The logger for diagnostic messages.</param>
    public HttpRouteConfigurator(
        IReadOnlyList<IProbotApp> apps,
        IServiceProvider serviceProvider,
        ILogger<HttpRouteConfigurator> logger)
    {
        _apps = apps ?? throw new ArgumentNullException(nameof(apps));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Configures HTTP routes for all loaded apps.
    /// This method calls <see cref="IProbotApp.ConfigureRoutesAsync"/> for each loaded app.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder for registering routes.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endpoints"/> is null.</exception>
    /// <remarks>
    /// If any app fails to configure routes, an exception is thrown and application startup fails.
    /// This is intentional to ensure apps are properly configured before accepting requests.
    /// </remarks>
    public async Task ConfigureRoutesAsync(IEndpointRouteBuilder endpoints)
    {
        if (endpoints == null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        _logger.LogInformation("Configuring HTTP routes for {AppCount} loaded app(s)", _apps.Count);

        foreach (var app in _apps)
        {
            try
            {
                _logger.LogDebug("Configuring routes for app: {AppName} v{Version}", app.Name, app.Version);

                await app.ConfigureRoutesAsync(endpoints, _serviceProvider);

                _logger.LogInformation("Successfully configured routes for app: {AppName}", app.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring routes for app: {AppName}. Application startup will fail.", app.Name);

                // Fail fast on route configuration errors
                // This ensures apps are properly configured before accepting requests
                throw new InvalidOperationException(
                    $"Failed to configure HTTP routes for app '{app.Name}'. See inner exception for details.",
                    ex);
            }
        }

        _logger.LogInformation("Completed HTTP route configuration for all apps");
    }
}
