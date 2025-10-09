// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Globalization;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ProbotSharp.Adapters.Http.Middleware;

/// <summary>
/// Middleware that measures and logs response times for HTTP requests.
/// Also adds the elapsed time to the response headers.
/// </summary>
public class ResponseTimeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseTimeMiddleware> _logger;
    private const string ResponseTimeHeaderName = "X-Response-Time-Ms";

    public ResponseTimeMiddleware(RequestDelegate next, ILogger<ResponseTimeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var stopwatch = Stopwatch.StartNew();
        var elapsedMilliseconds = 0L;

        context.Response.OnStarting(() =>
        {
            if (stopwatch.IsRunning)
            {
                stopwatch.Stop();
                elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }

            context.Response.Headers[ResponseTimeHeaderName] = elapsedMilliseconds.ToString(CultureInfo.InvariantCulture);

            return Task.CompletedTask;
        });

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            if (stopwatch.IsRunning)
            {
                stopwatch.Stop();
                elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                if (!context.Response.HasStarted)
                {
                    context.Response.Headers[ResponseTimeHeaderName] = elapsedMilliseconds.ToString(CultureInfo.InvariantCulture);
                }
            }
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;

            // Add response time to response headers
            _logger.LogInformation(
                "Request completed in {ElapsedMs}ms - {Method} {Path} - Status: {StatusCode}, CorrelationId: {CorrelationId}",
                elapsedMilliseconds,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                correlationId);
        }
    }
}
