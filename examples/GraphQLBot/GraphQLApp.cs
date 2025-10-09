// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Services;

namespace GraphQLBot;

/// <summary>
/// A ProbotSharp application that demonstrates using the GraphQL API helper on ProbotSharpContext.
/// This example shows how to execute GraphQL queries and mutations against GitHub's GraphQL API.
/// </summary>
public class GraphQLApp : IProbotApp
{
    /// <summary>
    /// Gets the name of the application.
    /// </summary>
    public string Name => "graphql-bot";

    /// <summary>
    /// Gets the version of the application.
    /// </summary>
    public string Version => "1.0.0";

    /// <summary>
    /// Configures services for the application.
    /// </summary>
    public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
    {
        // Register our event handlers as scoped services
        services.AddScoped<IssueGraphQLHandler>();
        services.AddScoped<PullRequestGraphQLHandler>();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Initializes the application by registering event handlers with the router.
    /// </summary>
    public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
    {
        // Register handlers for issue events
        router.RegisterHandler("issues", "opened", typeof(IssueGraphQLHandler));

        // Register handlers for pull request events
        router.RegisterHandler("pull_request", "opened", typeof(PullRequestGraphQLHandler));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Configures custom HTTP routes for the application.
    /// </summary>
    public Task ConfigureRoutesAsync(IEndpointRouteBuilder endpoints, IServiceProvider serviceProvider)
    {
        // Add a status endpoint
        endpoints.MapGet("/graphql-bot/status", () => Results.Ok(new
        {
            app = Name,
            version = Version,
            status = "running",
            description = "Demonstrates GraphQL API usage with ProbotSharpContext.GraphQL",
            timestamp = DateTime.UtcNow
        }))
        .WithName("GraphQLBotStatus")
        .WithTags("graphql-bot")
        .WithDescription("Returns the status of the GraphQL bot");

        return Task.CompletedTask;
    }
}
