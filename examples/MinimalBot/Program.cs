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
    Log.Information("Starting MinimalBot - A minimal ProbotSharp example");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog for ASP.NET Core logging
    builder.Host.UseSerilog();

    // Add ProbotSharp core services (no infrastructure dependencies required)
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    // Add minimal middleware
    app.UseProbotSharpMiddleware();
    app.UseIdempotency();

    // Root endpoint
    app.MapGet("/", () => Results.Ok(new
    {
        application = "MinimalBot",
        description = "A minimal ProbotSharp bot with zero infrastructure dependencies",
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

    Log.Information("MinimalBot started successfully");
    Log.Information("Webhooks endpoint: http://localhost:5000/webhooks");
    Log.Information("Health check: http://localhost:5000/health");
    Log.Information("Running in IN-MEMORY mode - no persistence, no external dependencies");

    app.Run();

    Log.Information("MinimalBot stopped cleanly");
}
catch (Exception ex)
{
    Log.Fatal(ex, "MinimalBot terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
