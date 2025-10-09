using ProbotSharp.Adapters.Http.Middleware;
using ProbotSharp.Adapters.Http.Webhooks;
using ProbotSharp.Application.Extensions;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Services;
using ProbotSharp.Infrastructure.Extensions;
using ConfigBot;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting ConfigBot - Repository Configuration Example");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Add ProbotSharp services
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Register ConfigBot handler manually (using attribute-based registration)
    builder.Services.AddScoped<ConfigBotHandler>();
    builder.Services.AddSingleton<EventRouter>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<EventRouter>>();
        var router = new EventRouter(logger);
        router.RegisterHandler("issues", "opened", typeof(ConfigBotHandler));
        logger.LogInformation("Registered ConfigBotHandler for issues.opened event");
        return router;
    });

    var app = builder.Build();

    app.UseProbotSharpMiddleware();
    app.UseIdempotency();

    app.MapGet("/", () => Results.Ok(new
    {
        application = "ConfigBot",
        description = "Demonstrates repository-backed configuration with context.config()",
        version = "1.0.0"
    }));

    app.MapGet("/health", () => Results.Ok(new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow
    }));

    app.MapPost("/webhooks", (
        HttpContext context,
        IWebhookProcessingPort processingPort,
        IConfiguration configuration,
        WebhookSignatureValidator signatureValidator,
        ILogger<Program> logger) =>
        WebhookEndpoint.HandleAsync(context, processingPort, configuration, signatureValidator, logger));

    Log.Information("ConfigBot started successfully");
    Log.Information("Webhooks endpoint: http://localhost:5000/webhooks");
    Log.Information("Health check: http://localhost:5000/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ConfigBot terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
