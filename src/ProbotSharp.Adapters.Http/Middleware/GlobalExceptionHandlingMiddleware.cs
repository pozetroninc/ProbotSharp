// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1848 // Performance: LoggerMessage delegates - not performance-critical for this codebase

namespace ProbotSharp.Adapters.Http.Middleware;

/// <summary>
/// Middleware that catches unhandled exceptions and returns proper error responses
/// following RFC 7807 Problem Details specification.
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger for recording unhandled exceptions.</param>
    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        this._next = next;
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to handle the HTTP request.
    /// Catches any unhandled exceptions and returns RFC 7807 Problem Details responses.
    /// </summary>
    /// <param name="context">The HTTP context for the request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await this._next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // CA1031: Catching general exception is intentional here for global exception handling
            await this.HandleExceptionAsync(context, ex).ConfigureAwait(false);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? traceId;

        this._logger.LogError(exception,
            "Unhandled exception occurred. TraceId: {TraceId}, CorrelationId: {CorrelationId}, Path: {Path}",
            traceId, correlationId, context.Request.Path);

        var statusCode = exception switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            InvalidOperationException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetailsResponse
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = GetTitle(statusCode),
            Status = statusCode,
            Detail = exception.Message,
            Instance = context.Request.Path,
            TraceId = traceId,
            CorrelationId = correlationId
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var json = JsonSerializer.Serialize(problemDetails, s_jsonOptions);
        await context.Response.WriteAsync(json, context.RequestAborted).ConfigureAwait(false);
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status500InternalServerError => "Internal Server Error",
        _ => "An error occurred"
    };

    private class ProblemDetailsResponse
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Status { get; set; }
        public string Detail { get; set; } = string.Empty;
        public string Instance { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
    }
}

#pragma warning restore CA1848
