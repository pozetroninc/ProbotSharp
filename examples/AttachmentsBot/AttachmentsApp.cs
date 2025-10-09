// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Services;

namespace AttachmentsBot;

/// <summary>
/// A ProbotSharp application that demonstrates adding structured attachments to comments.
/// This app adds build status cards when users comment with /build-status.
/// </summary>
public class AttachmentsApp : IProbotApp
{
    /// <summary>
    /// Gets the name of the application.
    /// </summary>
    public string Name => "attachments-bot";

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
        services.AddScoped<BuildStatusAttachment>();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Initializes the application after the DI container is built.
    /// This is where we register our event handlers with the router.
    /// </summary>
    public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
    {
        // Register the handler with the event router
        // This tells the router to call BuildStatusAttachment when an "issue_comment.created" event occurs
        router.RegisterHandler("issue_comment", "created", typeof(BuildStatusAttachment));

        return Task.CompletedTask;
    }
}
