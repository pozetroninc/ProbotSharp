using ProbotSharp.Application.Extensions;
using ProbotSharp.Infrastructure.Extensions;
using ProbotSharp.Adapters.Http.Middleware;
using ProbotSharp.Adapters.Http.Webhooks;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Domain.Services;
using Serilog;
using CompatibilityTestBot;
using Microsoft.AspNetCore.Mvc;

// Configure Serilog for console logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting CompatibilityTestBot - Probot Sharp compatibility test application");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog for ASP.NET Core logging
    builder.Host.UseSerilog();

    // Register TestEventTracker as singleton (shared across all requests)
    builder.Services.AddSingleton<TestEventTracker>();

    // Add ProbotSharp core services
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Register CompatibilityTestApp and discover event handlers
    await builder.Services.AddProbotAppsAsync(builder.Configuration);

    var app = builder.Build();

    // Add ProbotSharp middleware
    app.UseProbotSharpMiddleware();
    app.UseIdempotency();

    // Root endpoint - Application metadata
    app.MapGet("/", () => Results.Ok(new
    {
        application = "CompatibilityTestBot",
        description = "Probot Sharp compatibility test application for integration testing",
        version = "1.0.0",
        mode = "test",
        purpose = "Tracks webhook events for compatibility verification with Probot (Node.js)",
        endpoints = new
        {
            root = "GET /",
            health = "GET /health",
            ping = "GET /ping",
            webhooks = "POST /webhooks",
            webhooksAlt = "POST /api/github/webhooks",
            testEvents = "GET /test/events",
            testEventsByName = "GET /test/events/{eventName}",
            clearEvents = "DELETE /test/events"
        }
    }));

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow,
        mode = "test",
        dependencies = new
        {
            database = "In-memory (testing mode)",
            cache = "In-memory (MemoryCache)",
            queue = "In-memory (ConcurrentQueue)"
        }
    }));

    // Ping endpoint (Probot compatible) - returns plain text "PONG"
    app.MapGet("/ping", () => Results.Text("PONG"));

    // Primary webhook endpoint (ProbotSharp convention)
    app.MapPost("/webhooks", (
        HttpContext context,
        IWebhookProcessingPort processingPort,
        IConfiguration configuration,
        WebhookSignatureValidator signatureValidator,
        ILogger<Program> logger) =>
        WebhookEndpoint.HandleAsync(context, processingPort, configuration, signatureValidator, logger));

    // Alternate webhook endpoint (Probot Node.js compatibility)
    app.MapPost("/api/github/webhooks", (
        HttpContext context,
        IWebhookProcessingPort processingPort,
        IConfiguration configuration,
        WebhookSignatureValidator signatureValidator,
        ILogger<Program> logger) =>
        WebhookEndpoint.HandleAsync(context, processingPort, configuration, signatureValidator, logger));

    // Test API: Get all tracked events
    app.MapGet("/test/events", (TestEventTracker eventTracker) =>
    {
        var events = eventTracker.GetAllEvents();
        return Results.Ok(new
        {
            count = events.Count,
            events = events.Select(e => new
            {
                eventName = e.EventName,
                action = e.Action,
                deliveryId = e.DeliveryId,
                payload = e.Payload,
                receivedAt = e.ReceivedAt.ToString("o")
            })
        });
    });

    // Test API: Get events filtered by name
    app.MapGet("/test/events/{eventName}", (string eventName, TestEventTracker eventTracker) =>
    {
        try
        {
            var events = eventTracker.GetEventsByName(eventName);
            return Results.Ok(new
            {
                events = events.Select(e => new
                {
                    eventName = e.EventName,
                    action = e.Action,
                    deliveryId = e.DeliveryId,
                    payload = e.Payload,
                    receivedAt = e.ReceivedAt.ToString("o")
                })
            });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    });

    // Test API: Get bot status
    app.MapGet("/test/status", (TestEventTracker eventTracker) =>
    {
        return Results.Ok(new
        {
            app = "CompatibilityTestBot",
            version = "1.0.0",
            status = "running",
            eventsTracked = eventTracker.Count,
            uptime = DateTime.UtcNow.ToString("o"),
            mode = "test"
        });
    });

    // Test API: Clear all tracked events
    app.MapDelete("/test/events", (TestEventTracker eventTracker) =>
    {
        eventTracker.ClearEvents();
        return Results.Ok(new
        {
            message = "All tracked events cleared",
            timestamp = DateTime.UtcNow
        });
    });

    Log.Information("CompatibilityTestBot started successfully");
    Log.Information("Webhook endpoints:");
    Log.Information("  - POST /webhooks (ProbotSharp convention)");
    Log.Information("  - POST /api/github/webhooks (Probot Node.js compatibility)");
    Log.Information("Test API endpoints:");
    Log.Information("  - GET /test/events (get all tracked events)");
    Log.Information("  - GET /test/events/{{eventName}} (filter by event name)");
    Log.Information("  - DELETE /test/events (clear tracked events)");
    Log.Information("Health endpoints:");
    Log.Information("  - GET /health");
    Log.Information("  - GET /ping (Probot compatible)");
    Log.Information("Running in TEST mode - events are tracked for integration testing");

    app.Run();

    Log.Information("CompatibilityTestBot stopped cleanly");
}
catch (Exception ex)
{
    Log.Fatal(ex, "CompatibilityTestBot terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
