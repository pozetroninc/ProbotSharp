// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProbotSharp.Application.Services;

namespace ProbotSharp.Application.Abstractions;

/// <summary>
/// Defines a Probot application that can register event handlers, configure services, and add custom HTTP routes.
/// Implementations of this interface are discovered and loaded by the <see cref="ProbotAppLoader"/>.
/// </summary>
public interface IProbotApp
{
    /// <summary>
    /// Gets the name of the application.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of the application.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Configures services for the application.
    /// This method is called during service collection configuration, before the DI container is built.
    /// Use this to register event handlers, custom services, and other dependencies.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ConfigureAsync(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// Initializes the application after the DI container is built.
    /// This method is called after services are configured and can be used for startup tasks
    /// such as registering event handlers with the EventRouter.
    /// </summary>
    /// <param name="router">The event router for registering handlers.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider);

    /// <summary>
    /// Configures HTTP routes for the application.
    /// This method is called after the ASP.NET Core middleware pipeline is configured.
    /// Use this to register custom HTTP endpoints accessible alongside webhook handlers.
    /// The default implementation does nothing, so apps without custom routes don't need to override.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder for registering routes.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <example>
    /// <code>
    /// public Task ConfigureRoutesAsync(IEndpointRouteBuilder endpoints, IServiceProvider serviceProvider)
    /// {
    ///     var apiGroup = endpoints.MapGroup("/my-app/api");
    ///
    ///     apiGroup.MapGet("/status", async (IMyService svc) =>
    ///         Results.Ok(await svc.GetStatusAsync()));
    ///
    ///     apiGroup.MapPost("/trigger", async ([FromBody] Request req, IMyService svc) =>
    ///         Results.Accepted(await svc.TriggerAsync(req)));
    ///
    ///     return Task.CompletedTask;
    /// }
    /// </code>
    /// </example>
    Task ConfigureRoutesAsync(IEndpointRouteBuilder endpoints, IServiceProvider serviceProvider)
    {
        // Default implementation: no custom routes
        return Task.CompletedTask;
    }
}
