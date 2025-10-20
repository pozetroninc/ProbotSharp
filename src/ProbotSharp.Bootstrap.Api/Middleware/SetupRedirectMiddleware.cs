// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Ports.Outbound;

#pragma warning disable CA1848 // Performance: LoggerMessage delegates - not performance-critical for this codebase

namespace ProbotSharp.Bootstrap.Api.Middleware;

/// <summary>
/// Middleware that redirects to /setup if the app is not configured.
/// Can be disabled by setting PROBOTSHARP_SKIP_SETUP=true environment variable.
/// </summary>
public class SetupRedirectMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SetupRedirectMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetupRedirectMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger for recording setup redirect operations.</param>
    public SetupRedirectMiddleware(RequestDelegate next, ILogger<SetupRedirectMiddleware> logger)
    {
        this._next = next;
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to handle the HTTP request.
    /// Redirects to /setup if the application is not configured.
    /// </summary>
    /// <param name="context">The HTTP context for the request.</param>
    /// <param name="manifestPort">The manifest persistence port for checking configuration status.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, IManifestPersistencePort manifestPort)
    {
        // Skip if already on setup route
        if (context.Request.Path.StartsWithSegments("/setup"))
        {
            await this._next(context).ConfigureAwait(false);
            return;
        }

        // Skip if PROBOTSHARP_SKIP_SETUP=true
        var skipSetup = Environment.GetEnvironmentVariable("PROBOTSHARP_SKIP_SETUP");
        if (skipSetup?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
        {
            await this._next(context).ConfigureAwait(false);
            return;
        }

        // Skip for health checks, webhooks, and root API endpoint
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/webhooks") ||
            context.Request.Path.Equals("/", StringComparison.Ordinal))
        {
            await this._next(context).ConfigureAwait(false);
            return;
        }

        try
        {
            // Check if configured
            var manifestResult = await manifestPort.GetAsync().ConfigureAwait(false);
            var isConfigured = manifestResult.IsSuccess && !string.IsNullOrWhiteSpace(manifestResult.Value);

            if (!isConfigured)
            {
                this._logger.LogInformation("App not configured, redirecting to /setup");
                context.Response.Redirect("/setup");
                return;
            }
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Error checking configuration status, allowing request to proceed");
            // If we can't check, allow the request to proceed
        }

        await this._next(context).ConfigureAwait(false);
    }
}

/// <summary>
/// Extension methods for SetupRedirectMiddleware.
/// </summary>
public static class SetupRedirectMiddlewareExtensions
{
    /// <summary>
    /// Adds the SetupRedirectMiddleware to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseSetupRedirect(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SetupRedirectMiddleware>();
    }
}

#pragma warning restore CA1848
