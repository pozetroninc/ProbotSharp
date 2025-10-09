// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Services;

namespace WildcardBot;

/// <summary>
/// Demonstrates wildcard event handler patterns in ProbotSharp.
/// Shows how to handle all events (*), all actions for an event (event.*), and specific events.
/// </summary>
public class WildcardApp : IProbotApp
{
    public string Name => "wildcard-bot";
    public string Version => "1.0.0";

    public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
    {
        // Register all handlers
        services.AddScoped<AllEventsLogger>();
        services.AddScoped<AllIssueEventsHandler>();
        services.AddScoped<SpecificEventHandler>();
        services.AddScoped<MetricsCollector>();

        return Task.CompletedTask;
    }

    public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
    {
        // Register wildcard handlers
        // * (all events)
        router.RegisterHandler("*", null, typeof(AllEventsLogger));
        router.RegisterHandler("*", null, typeof(MetricsCollector));

        // event.* (all actions for specific event)
        router.RegisterHandler("issues", "*", typeof(AllIssueEventsHandler));

        // Specific event + action
        router.RegisterHandler("issues", "opened", typeof(SpecificEventHandler));

        return Task.CompletedTask;
    }

    public Task ConfigureRoutesAsync(IEndpointRouteBuilder endpoints, IServiceProvider serviceProvider)
    {
        // No custom routes needed for this example
        return Task.CompletedTask;
    }
}
