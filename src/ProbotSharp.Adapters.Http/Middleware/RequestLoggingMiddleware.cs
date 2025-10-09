// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ProbotSharp.Adapters.Http.Middleware;

/// <summary>
/// Middleware that logs incoming HTTP requests with correlation IDs.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;
        var request = context.Request;

        _logger.LogInformation(
            "Incoming request: {Method} {Path} {QueryString} - CorrelationId: {CorrelationId}, RemoteIP: {RemoteIP}, UserAgent: {UserAgent}",
            request.Method,
            request.Path,
            request.QueryString,
            correlationId,
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            request.Headers["User-Agent"].ToString() ?? "unknown");

        await _next(context).ConfigureAwait(false);

        _logger.LogInformation(
            "Request completed: {Method} {Path} - Status: {StatusCode}, CorrelationId: {CorrelationId}",
            request.Method,
            request.Path,
            context.Response.StatusCode,
            correlationId);
    }
}
