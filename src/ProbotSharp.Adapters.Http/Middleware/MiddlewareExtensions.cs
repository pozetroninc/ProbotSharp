// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;

namespace ProbotSharp.Adapters.Http.Middleware;

/// <summary>
/// Extension methods for registering ProbotSharp middleware components.
/// </summary>
public static partial class MiddlewareExtensions
{
    /// <summary>
    /// Adds all ProbotSharp middleware to the application pipeline in the correct order:
    /// 1. Correlation ID (must be first to generate IDs for logging)
    /// 2. Response Time tracking
    /// 3. Request Logging
    /// 4. Global Exception Handling (must be early to catch all exceptions)
    /// </summary>
    public static IApplicationBuilder UseProbotSharpMiddleware(this IApplicationBuilder app)
    {
        app.UseCorrelationId();
        app.UseResponseTime();
        app.UseRequestLogging();
        app.UseGlobalExceptionHandling();

        return app;
    }

    /// <summary>
    /// Adds the Correlation ID middleware to the application pipeline.
    /// This should be one of the first middleware in the pipeline.
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }

    /// <summary>
    /// Adds the Request Logging middleware to the application pipeline.
    /// This should come after the Correlation ID middleware.
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }

    /// <summary>
    /// Adds the Response Time tracking middleware to the application pipeline.
    /// This should be early in the pipeline to measure the full request duration.
    /// </summary>
    public static IApplicationBuilder UseResponseTime(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ResponseTimeMiddleware>();
    }

    /// <summary>
    /// Adds the Global Exception Handling middleware to the application pipeline.
    /// This should be early in the pipeline to catch all unhandled exceptions.
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}
