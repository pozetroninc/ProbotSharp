// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Application.Services;

namespace PaginationBot;

/// <summary>
/// A ProbotSharp application that demonstrates pagination patterns with the GitHub API.
/// This app shows how to efficiently fetch and process large datasets from GitHub.
/// </summary>
public class PaginationApp : IProbotApp
{
    /// <summary>
    /// Gets the name of the application.
    /// </summary>
    public string Name => "pagination-bot";

    /// <summary>
    /// Gets the version of the application.
    /// </summary>
    public string Version => "1.0.0";

    /// <summary>
    /// Configures services for the application.
    /// </summary>
    public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
    {
        // Register the pagination examples handler
        services.AddScoped<PaginationExamples>();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Initializes the application by registering event handlers.
    /// </summary>
    public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
    {
        // Register handler for when a repository is added to the installation
        // This will demonstrate various pagination patterns
        router.RegisterHandler("installation_repositories", "added", typeof(PaginationExamples));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Configures custom HTTP routes for the application.
    /// </summary>
    public Task ConfigureRoutesAsync(IEndpointRouteBuilder endpoints, IServiceProvider serviceProvider)
    {
        // No custom routes needed for this example
        return Task.CompletedTask;
    }
}
