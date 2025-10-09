// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProbotSharp.Application.Extensions;
using ProbotSharp.Infrastructure.Extensions;
using ProbotSharp.Adapters.Http.Middleware;
using ProbotSharp.Adapters.Http.Webhooks;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Domain.Services;

// Load environment variables from .env file if it exists
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(
    Enum.Parse<LogLevel>(
        builder.Configuration["LOG_LEVEL_DEFAULT"] ?? "Information",
        ignoreCase: true));

// Add ProbotSharp infrastructure and application services
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Load ProbotSharp apps from the current assembly
await builder.Services.AddProbotAppsAsync(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseRouting();

// Map webhook endpoint
app.MapPost("/api/github/webhooks", async (
    HttpContext context,
    IServiceProvider serviceProvider) =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Read the request body
        using var reader = new StreamReader(context.Request.Body);
        var payload = await reader.ReadToEndAsync();

        // TODO: Process webhook through ProcessWebhookUseCase
        // This is a simplified implementation - you'll need to:
        // 1. Validate webhook signature
        // 2. Parse payload
        // 3. Call ProcessWebhookUseCase

        logger.LogInformation("Received webhook from GitHub");

        return Results.Ok(new { message = "Webhook received" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing webhook");
        return Results.StatusCode(500);
    }
});

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", bot = "TemplateTestBot" }));

// Root endpoint
app.MapGet("/", () => Results.Ok(new
{
    name = "TemplateTestBot",
    description = "Bot created from template for testing",
    version = "1.0.0",
    author = "ProbotSharp Test"
}));

app.Logger.LogInformation("Starting TemplateTestBot...");
app.Logger.LogInformation("Bot created from template for testing");
app.Logger.LogInformation("Webhook endpoint: POST /api/github/webhooks");
app.Logger.LogInformation("Health check: GET /health");

app.Run();
