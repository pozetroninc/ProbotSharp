// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1848 // Performance: LoggerMessage delegates - not performance-critical for this codebase

namespace ProbotSharp.Adapters.Http.Middleware;

/// <summary>
/// Middleware that logs incoming HTTP requests with correlation IDs.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestLoggingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger for recording HTTP request information.</param>
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        this._next = next;
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to handle the HTTP request.
    /// Logs incoming request details and completion status with correlation ID.
    /// </summary>
    /// <param name="context">The HTTP context for the request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;
        var request = context.Request;

        this._logger.LogInformation(
            "Incoming request: {Method} {Path} {QueryString} - CorrelationId: {CorrelationId}, RemoteIP: {RemoteIP}, UserAgent: {UserAgent}",
            request.Method,
            request.Path,
            request.QueryString,
            correlationId,
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            request.Headers["User-Agent"].ToString() ?? "unknown");

        await this._next(context).ConfigureAwait(false);

        this._logger.LogInformation(
            "Request completed: {Method} {Path} - Status: {StatusCode}, CorrelationId: {CorrelationId}",
            request.Method,
            request.Path,
            context.Response.StatusCode,
            correlationId);
    }
}

#pragma warning restore CA1848
