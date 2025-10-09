// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Services;

namespace MetadataBot;

/// <summary>
/// A ProbotSharp application demonstrating metadata storage capabilities.
/// This app tracks edit counts on issues and reports them when the issue is closed.
/// </summary>
public class MetadataApp : IProbotApp
{
    /// <summary>
    /// Gets the name of the application.
    /// </summary>
    public string Name => "metadata-bot";

    /// <summary>
    /// Gets the version of the application.
    /// </summary>
    public string Version => "1.0.0";

    /// <summary>
    /// Configures services for the application.
    /// </summary>
    public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
    {
        // Register our event handlers
        services.AddScoped<EditCountTracker>();
        services.AddScoped<EditCountReporter>();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Initializes the application by registering event handlers.
    /// </summary>
    public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
    {
        // Track edits on issues and comments
        router.RegisterHandler("issues", "edited", typeof(EditCountTracker));
        router.RegisterHandler("issue_comment", "edited", typeof(EditCountTracker));

        // Report edit count when issue is closed
        router.RegisterHandler("issues", "closed", typeof(EditCountReporter));

        return Task.CompletedTask;
    }
}
