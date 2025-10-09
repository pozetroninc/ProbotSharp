// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Extensions;
using ProbotSharp.Application.Services;

namespace ExtensionsBot;

/// <summary>
/// Example ProbotSharp app demonstrating all three built-in extensions:
/// - Slash Commands: Interactive commands from comments
/// - Metadata Storage: Persistent key-value data
/// - Comment Attachments: Rich structured content
/// </summary>
public class ExtensionsApp : IProbotApp
{
    /// <summary>
    /// Gets the display name for this app.
    /// </summary>
    public string Name => "Extensions Demo Bot";

    /// <summary>
    /// Gets the version of this app.
    /// </summary>
    public string Version => "1.0.0";

    /// <summary>
    /// Configures services and registers all extension handlers.
    /// </summary>
    public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
    {
        // Register slash command handlers from this assembly
        // This auto-discovers all classes decorated with [SlashCommandHandler]
        services.AddSlashCommands(typeof(ExtensionsApp).Assembly);

        // MetadataService and CommentAttachmentService are automatically registered
        // by the framework, so no additional registration is needed

        return Task.CompletedTask;
    }

    /// <summary>
    /// Initializes the app after the service provider is built.
    /// </summary>
    /// <param name="router">The event router for registering handlers.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
    {
        // Slash command handlers are automatically discovered and registered via AddSlashCommands
        // Event handlers using metadata and attachments are registered via EventHandler attribute
        return Task.CompletedTask;
    }
}
