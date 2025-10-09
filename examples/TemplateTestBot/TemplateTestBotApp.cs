// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TemplateTestBot.Handlers;
using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Services;

namespace TemplateTestBot;

/// <summary>
/// Main ProbotSharp application for TemplateTestBot.
/// Bot created from template for testing
/// </summary>
public class TemplateTestBotApp : IProbotApp
{
    /// <summary>
    /// Gets the name of the application.
    /// </summary>
    public string Name => "TemplateTestBot";

    /// <summary>
    /// Gets the version of the application.
    /// </summary>
    public string Version => "1.0.0";

    /// <summary>
    /// Configures services for the application.
    /// This is called during DI setup, before the container is built.
    /// Register your event handlers and any custom services here.
    /// </summary>
    public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
    {
        // Register event handlers as scoped services
        services.AddScoped<ExampleHandler>();

        // Add your custom services here
        // Example: services.AddScoped<IMyService, MyService>();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Initializes the application after the DI container is built.
    /// Register your event handlers with the router here.
    /// </summary>
    public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
    {
        // Register event handlers with the event router
        // The router will call these handlers when matching webhook events are received

        router.RegisterHandler("issues", "opened", typeof(ExampleHandler));

        // Add more handler registrations here
        // Examples:
        // router.RegisterHandler("pull_request", "opened", typeof(PullRequestHandler));
        // router.RegisterHandler("push", null, typeof(PushHandler));
        // router.RegisterHandler("*", null, typeof(AllEventsHandler)); // Wildcard - all events

        return Task.CompletedTask;
    }
}
