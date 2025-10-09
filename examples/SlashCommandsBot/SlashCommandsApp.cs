// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Extensions;
using ProbotSharp.Application.Services;

namespace SlashCommandsBot;

/// <summary>
/// Example ProbotSharp app demonstrating slash command functionality.
/// This app responds to slash commands in issue and PR comments.
/// </summary>
public class SlashCommandsApp : IProbotApp
{
    /// <summary>
    /// Gets the display name for this app.
    /// </summary>
    public string Name => "Slash Commands Bot";

    /// <summary>
    /// Gets the version of this app.
    /// </summary>
    public string Version => "1.0.0";

    /// <summary>
    /// Configures services and registers slash command handlers.
    /// </summary>
    public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
    {
        // Register slash command handlers from this assembly
        services.AddSlashCommands(typeof(SlashCommandsApp).Assembly);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Initializes the app after the service provider is built.
    /// </summary>
    /// <param name="router">The event router for registering handlers.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
    {
        // No initialization needed for this example
        // Slash command handlers are automatically discovered and registered via AddSlashCommands
        return Task.CompletedTask;
    }
}
