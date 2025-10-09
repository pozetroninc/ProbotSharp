// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Services;

namespace HttpExtensibilityBot;

/// <summary>
/// Demonstrates HTTP extensibility in ProbotSharp.
/// This app shows how to add custom HTTP endpoints alongside webhook handlers.
/// </summary>
public class HttpExtensibilityApp : IProbotApp
{
    public string Name => "http-extensibility-demo";
    public string Version => "1.0.0";

    public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
    {
        // Register custom service
        services.AddScoped<IReportingService, ReportingService>();
        return Task.CompletedTask;
    }

    public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
    {
        // Could register event handlers here
        return Task.CompletedTask;
    }

    public Task ConfigureRoutesAsync(IEndpointRouteBuilder endpoints, IServiceProvider serviceProvider)
    {
        // Group all custom routes under /api/demo
        var apiGroup = endpoints.MapGroup("/api/demo");

        // GET endpoint with no dependencies
        apiGroup.MapGet("/ping", () => Results.Ok(new
        {
            message = "pong",
            timestamp = DateTime.UtcNow
        }))
        .WithName("DemoPing")
        .WithTags("demo")
        .WithDescription("Simple ping endpoint for testing");

        // GET endpoint with DI
        apiGroup.MapGet("/report", async (IReportingService reporting) =>
        {
            var report = await reporting.GenerateReportAsync();
            return Results.Ok(report);
        })
        .WithName("DemoReport")
        .WithTags("demo")
        .WithDescription("Generate a status report");

        // POST endpoint with request body and DI
        apiGroup.MapPost("/trigger", async (
            [FromBody] TriggerRequest request,
            IReportingService reporting,
            ILogger<HttpExtensibilityApp> logger) =>
        {
            if (string.IsNullOrWhiteSpace(request.Action))
            {
                return Results.BadRequest(new { error = "Action is required" });
            }

            logger.LogInformation("Trigger received: {Action}", request.Action);
            await reporting.TriggerAsync(request.Action);

            return Results.Accepted($"/api/demo/report", new
            {
                message = "Trigger accepted",
                action = request.Action
            });
        })
        .WithName("DemoTrigger")
        .WithTags("demo")
        .WithDescription("Trigger a custom action");

        // GET endpoint with route parameter
        apiGroup.MapGet("/status/{id}", (string id, IReportingService reporting) =>
        {
            var status = reporting.GetStatus(id);
            return status != null
                ? Results.Ok(status)
                : Results.NotFound(new { error = "Status not found", id });
        })
        .WithName("DemoStatus")
        .WithTags("demo")
        .WithDescription("Get status by ID");

        return Task.CompletedTask;
    }
}

public record TriggerRequest(string Action);

public interface IReportingService
{
    Task<object> GenerateReportAsync();
    Task TriggerAsync(string action);
    object? GetStatus(string id);
}

public class ReportingService : IReportingService
{
    private readonly Dictionary<string, object> _statuses = new();

    public Task<object> GenerateReportAsync()
    {
        return Task.FromResult<object>(new
        {
            totalEvents = 42,
            lastProcessed = DateTime.UtcNow.AddMinutes(-5),
            queueDepth = 3,
            generatedAt = DateTime.UtcNow
        });
    }

    public Task TriggerAsync(string action)
    {
        var id = Guid.NewGuid().ToString();
        _statuses[id] = new
        {
            action,
            triggeredAt = DateTime.UtcNow,
            status = "pending"
        };
        return Task.CompletedTask;
    }

    public object? GetStatus(string id)
    {
        return _statuses.GetValueOrDefault(id);
    }
}
