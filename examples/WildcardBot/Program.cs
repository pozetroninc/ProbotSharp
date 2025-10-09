using ProbotSharp.Application.Extensions;
using ProbotSharp.Infrastructure.Extensions;
using ProbotSharp.Adapters.Http.Middleware;
using ProbotSharp.Adapters.Http.Webhooks;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Domain.Services;
using WildcardBot;
using Serilog;

// Configure Serilog for console logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting WildcardBot - Demonstrating wildcard event handlers");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog for ASP.NET Core logging
    builder.Host.UseSerilog();

    // Add ProbotSharp core services
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Load ProbotSharp apps from this assembly (will auto-discover WildcardApp)
    await builder.Services.AddProbotAppsAsync(builder.Configuration);

    var app = builder.Build();

    // Add middleware
    app.UseProbotSharpMiddleware();
    app.UseIdempotency();

    // Root endpoint
    app.MapGet("/", () => Results.Ok(new
    {
        application = "WildcardBot",
        description = "Demonstrates wildcard event handlers (*, event.*, specific events)",
        version = "1.0.0",
        handlers = new[]
        {
            "AllEventsLogger - Logs all webhook events [EventHandler(\"*\", null)]",
            "AllIssueEventsHandler - Handles all issue events [EventHandler(\"issues\", \"*\")]",
            "SpecificEventHandler - Handles specific events [EventHandler(\"issues\", \"opened\")]",
            "MetricsCollector - Collects metrics from all events [EventHandler(\"*\", null)]"
        }
    }));

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow
    }));

    // Webhook endpoint
    app.MapPost("/webhooks", (
        HttpContext context,
        IWebhookProcessingPort processingPort,
        IConfiguration configuration,
        WebhookSignatureValidator signatureValidator,
        ILogger<Program> logger) =>
        WebhookEndpoint.HandleAsync(context, processingPort, configuration, signatureValidator, logger));

    Log.Information("WildcardBot started successfully");
    Log.Information("Webhooks endpoint: http://localhost:5000/webhooks");
    Log.Information("Health check: http://localhost:5000/health");
    Log.Information("Wildcard handlers registered:");
    Log.Information("  - AllEventsLogger: Logs all events (*)");
    Log.Information("  - AllIssueEventsHandler: Handles all issue events (issues.*)");
    Log.Information("  - SpecificEventHandler: Handles issues.opened specifically");
    Log.Information("  - MetricsCollector: Collects metrics from all events (*)");

    app.Run();

    Log.Information("WildcardBot stopped cleanly");
}
catch (Exception ex)
{
    Log.Fatal(ex, "WildcardBot terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
