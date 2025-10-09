using ProbotSharp.Application.Extensions;
using ProbotSharp.Infrastructure.Extensions;
using ProbotSharp.Adapters.Http.Middleware;
using ProbotSharp.Adapters.Http.Webhooks;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Domain.Services;
using Serilog;

// Configure Serilog for console logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting ExtensionsBot");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog for ASP.NET Core logging
    builder.Host.UseSerilog();

    // Add ProbotSharp core services
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Load ProbotSharp apps from this assembly
    await builder.Services.AddProbotAppsAsync(builder.Configuration);

    var app = builder.Build();

    // Add middleware
    app.UseProbotSharpMiddleware();
    app.UseIdempotency();

    // Root endpoint
    app.MapGet("/", () => Results.Ok(new
    {
        application = "ExtensionsBot",
        description = "ProbotSharp example application",
        version = "1.0.0",
        mode = "in-memory"
    }));

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow,
        dependencies = new
        {
            database = "Not configured (in-memory mode)",
            cache = "In-memory (MemoryCache)",
            queue = "In-memory (ConcurrentQueue)"
        }
    }));

    // Webhook endpoint
    app.MapPost("/webhooks", (
        HttpContext context,
        IWebhookProcessingPort processingPort,
        IConfiguration configuration,
        WebhookSignatureValidator signatureValidator,
        ILogger<Program> logger) =>
        WebhookEndpoint.HandleAsync(context, processingPort, configuration, signatureValidator, logger));

    Log.Information("ExtensionsBot started successfully");
    Log.Information("Webhooks endpoint: http://localhost:5000/webhooks");
    Log.Information("Health check: http://localhost:5000/health");
    Log.Information("Running in IN-MEMORY mode");

    app.Run();

    Log.Information("ExtensionsBot stopped cleanly");
}
catch (Exception ex)
{
    Log.Fatal(ex, "ExtensionsBot terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
