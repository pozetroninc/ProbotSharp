// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Globalization;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1848 // Performance: LoggerMessage delegates - not performance-critical for this codebase

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

    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseTimeMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger for recording response time metrics.</param>
    public ResponseTimeMiddleware(RequestDelegate next, ILogger<ResponseTimeMiddleware> logger)
    {
        this._next = next;
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to handle the HTTP request.
    /// Measures elapsed time and adds it to response headers and logs.
    /// </summary>
    /// <param name="context">The HTTP context for the request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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
            await this._next(context).ConfigureAwait(false);
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
            this._logger.LogInformation(
                "Request completed in {ElapsedMs}ms - {Method} {Path} - Status: {StatusCode}, CorrelationId: {CorrelationId}",
                elapsedMilliseconds,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                correlationId);
        }
    }
}

#pragma warning restore CA1848
