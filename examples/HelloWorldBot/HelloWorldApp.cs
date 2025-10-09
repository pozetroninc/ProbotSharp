// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Services;

namespace HelloWorldBot;

/// <summary>
/// A simple ProbotSharp application that demonstrates basic event handling and custom HTTP routes.
/// This app greets users when they open new issues and provides a status endpoint.
/// </summary>
public class HelloWorldApp : IProbotApp
{
    /// <summary>
    /// Gets the name of the application.
    /// </summary>
    public string Name => "hello-world";

    /// <summary>
    /// Gets the version of the application.
    /// </summary>
    public string Version => "1.0.0";

    /// <summary>
    /// Configures services for the application.
    /// This is called during DI setup, before the container is built.
    /// </summary>
    public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
    {
        // Register our event handler as a scoped service
        services.AddScoped<IssueGreeter>();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Initializes the application after the DI container is built.
    /// This is where we register our event handlers with the router.
    /// </summary>
    public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
    {
        // Register the handler with the event router
        // This tells the router to call IssueGreeter when an "issues.opened" event occurs
        router.RegisterHandler("issues", "opened", typeof(IssueGreeter));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Configures custom HTTP routes for the application.
    /// This demonstrates how to add custom endpoints alongside webhook handlers.
    /// </summary>
    public Task ConfigureRoutesAsync(IEndpointRouteBuilder endpoints, IServiceProvider serviceProvider)
    {
        // Add a simple status endpoint
        endpoints.MapGet("/hello-world/status", () => Results.Ok(new
        {
            app = Name,
            version = Version,
            status = "running",
            timestamp = DateTime.UtcNow
        }))
        .WithName("HelloWorldStatus")
        .WithTags("hello-world")
        .WithDescription("Returns the status of the Hello World bot");

        return Task.CompletedTask;
    }
}
