// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Application.Services;

namespace DryRunBot;

/// <summary>
/// Demonstrates the dry-run mode feature for safely testing bulk operations.
/// This app shows multiple patterns for implementing dry-run logic to prevent
/// accidental execution of large-scale changes.
///
/// To enable dry-run mode, set the environment variable: PROBOT_DRY_RUN=true
/// </summary>
public class DryRunApp : IProbotApp
{
    /// <summary>
    /// Gets the name of the application.
    /// </summary>
    public string Name => "dry-run-bot";

    /// <summary>
    /// Gets the version of the application.
    /// </summary>
    public string Version => "1.0.0";

    /// <summary>
    /// Configures services for the application.
    /// </summary>
    public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
    {
        // Register event handlers
        services.AddScoped<BulkIssueCreator>();
        services.AddScoped<BulkCommentProcessor>();
        services.AddScoped<LabelManager>();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Initializes the application and registers event handlers.
    /// </summary>
    public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
    {
        // Example 1: Bulk issue creation (using dry-run checks)
        router.RegisterHandler("repository", "created", typeof(BulkIssueCreator));

        // Example 2: Bulk comment processing (using ExecuteOrLog helper)
        router.RegisterHandler("issues", "labeled", typeof(BulkCommentProcessor));

        // Example 3: Label management (using ThrowIfNotDryRun)
        router.RegisterHandler("push", "null", typeof(LabelManager));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Configures custom HTTP routes.
    /// </summary>
    public Task ConfigureRoutesAsync(IEndpointRouteBuilder endpoints, IServiceProvider serviceProvider)
    {
        endpoints.MapGet("/dry-run/status", () => Results.Ok(new
        {
            app = Name,
            version = Version,
            dryRunMode = Environment.GetEnvironmentVariable("PROBOT_DRY_RUN")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false,
            timestamp = DateTime.UtcNow,
        }))
        .WithName("DryRunBotStatus")
        .WithTags("dry-run")
        .WithDescription("Returns the status and current dry-run mode setting");

        return Task.CompletedTask;
    }
}
