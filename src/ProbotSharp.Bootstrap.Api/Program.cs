using System.Text.Json;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using ProbotSharp.Adapters.Http.HealthChecks;
using ProbotSharp.Adapters.Http.Middleware;
using ProbotSharp.Adapters.Http.Webhooks;
using ProbotSharp.Adapters.Workers;
using ProbotSharp.Application.Extensions;
using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Bootstrap.Api;
using ProbotSharp.Bootstrap.Api.Middleware;
using ProbotSharp.Domain.Services;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Infrastructure.Extensions;

using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ProbotSharp")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/probotsharp-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    Log.Information("Starting ProbotSharp API application");

var builder = WebApplication.CreateBuilder(args);

// Use Serilog for ASP.NET Core logging
builder.Host.UseSerilog();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add MVC and Razor Pages support for setup wizard
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Configure webhook replay worker options
builder.Services.Configure<WebhookReplayWorkerOptions>(options =>
{
    options.MaxRetryAttempts = builder.Configuration.GetValue<int>("ProbotSharp:ReplayQueue:MaxRetryAttempts", 3);
    options.RetryBaseDelayMs = builder.Configuration.GetValue<int>("ProbotSharp:ReplayQueue:RetryBaseDelayMs", 1000);
    options.PollIntervalMs = builder.Configuration.GetValue<int>("ProbotSharp:ReplayQueue:PollIntervalMs", 1000);
});

// Register comprehensive health checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql", "ready" })
    .AddCheck<CacheHealthCheck>(
        name: "cache",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "cache", "ready" })
    .AddCheck<GitHubApiHealthCheck>(
        name: "github_api",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external", "github", "ready" });

builder.Services.AddHostedService<WebhookReplayWorker>();

var app = builder.Build();

// Add middleware in the correct order:
// 1. Correlation ID - must be first to generate IDs for logging
// 2. Response Time - early to measure full request duration
// 3. Request Logging - logs requests with correlation IDs
// 4. Global Exception Handling - catches all unhandled exceptions
// 5. Idempotency - prevents duplicate webhook processing
// 6. Setup Redirect - redirect to /setup if not configured
app.UseProbotSharpMiddleware();
app.UseIdempotency();
app.UseSetupRedirect();

app.MapGet("/", () => Results.Ok(new { application = "ProbotSharp", version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0" }));

// Health check endpoint with detailed JSON response
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds,
                exception = entry.Value.Exception?.Message,
                data = entry.Value.Data
            })
        }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(result);
    }
});

app.MapPost("/webhooks", (HttpContext context, IWebhookProcessingPort processingPort, IConfiguration configuration, WebhookSignatureValidator signatureValidator, ILogger<Program> logger)
    => WebhookEndpoint.HandleAsync(context, processingPort, configuration, signatureValidator, logger));

// Replay endpoint - allows manual replay of failed webhooks by delivery ID
app.MapPost("/webhooks/replay/{deliveryId}", async (
    string deliveryId,
    IWebhookReplayQueuePort replayQueue,
    IWebhookStoragePort storage,
    ILogger<Program> logger) =>
{
    try
    {
        // Retrieve webhook from storage
        var webhookResult = await storage.GetAsync(DeliveryId.Create(deliveryId));
        if (!webhookResult.IsSuccess || webhookResult.Value == null)
        {
            logger.LogWarning("Webhook with delivery ID {DeliveryId} not found", deliveryId);
            return Results.NotFound(new { error = "Webhook not found", deliveryId });
        }

        var webhook = webhookResult.Value;

        // Create replay command - note: signature and raw payload are not stored,
        // so replay bypasses signature validation
        var command = new ProcessWebhookCommand(
            DeliveryId: webhook.Id,
            EventName: webhook.EventName,
            Payload: webhook.Payload,
            InstallationId: webhook.InstallationId,
            Signature: WebhookSignature.Create(string.Empty), // No signature for replays
            RawPayload: string.Empty); // Raw payload not stored

        var replayCommand = new EnqueueReplayCommand(command, attempt: 0);

        // Enqueue for replay
        var enqueueResult = await replayQueue.EnqueueAsync(replayCommand);
        if (!enqueueResult.IsSuccess)
        {
            logger.LogError("Failed to enqueue replay for delivery {DeliveryId}: {Error}",
                deliveryId, enqueueResult.Error?.ToString() ?? "Unknown error");
            return Results.Problem(
                detail: enqueueResult.Error?.Message ?? "Failed to enqueue replay",
                statusCode: 500);
        }

        logger.LogInformation("Webhook replay enqueued for delivery {DeliveryId}", deliveryId);
        return Results.Accepted($"/webhooks/replay/{deliveryId}", new
        {
            message = "Webhook replay enqueued",
            deliveryId,
            eventName = webhook.EventName.Value
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error enqueuing webhook replay for delivery {DeliveryId}", deliveryId);
        return Results.Problem("Internal server error", statusCode: 500);
    }
});

// Dead-letter queue stats endpoint
app.MapGet("/webhooks/dlq/stats", async (IDeadLetterQueuePort dlq) =>
{
    var result = await dlq.GetAllAsync();
    if (!result.IsSuccess)
    {
        return Results.Problem("Failed to retrieve dead-letter queue items", statusCode: 500);
    }

    var items = result.Value ?? new List<DeadLetterItem>();
    return Results.Ok(new
    {
        totalItems = items.Count,
        items = items.Select(item => new
        {
            id = item.Id,
            deliveryId = item.Command.Command.DeliveryId.Value,
            eventName = item.Command.Command.EventName.Value,
            reason = item.Reason,
            failedAt = item.FailedAt,
            attempts = item.Command.Attempt
        })
    });
});

// Configure custom HTTP routes from loaded ProbotSharp apps
var routeConfigurator = app.Services.GetService<ProbotSharp.Application.Services.HttpRouteConfigurator>();
if (routeConfigurator != null)
{
    Log.Information("Configuring custom HTTP routes from ProbotSharp apps");
    await routeConfigurator.ConfigureRoutesAsync(app);
}

// Map MVC controllers for setup wizard
app.MapControllers();

app.Run();

    Log.Information("ProbotSharp API application stopped cleanly");
}
catch (Exception ex)
{
    Log.Fatal(ex, "ProbotSharp API application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
