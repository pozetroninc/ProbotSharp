// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Ports.Outbound;

namespace ProbotSharp.Bootstrap.Api.Middleware;

/// <summary>
/// Middleware that redirects to /setup if the app is not configured.
/// Can be disabled by setting PROBOTSHARP_SKIP_SETUP=true environment variable.
/// </summary>
public class SetupRedirectMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SetupRedirectMiddleware> _logger;

    public SetupRedirectMiddleware(RequestDelegate next, ILogger<SetupRedirectMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IManifestPersistencePort manifestPort)
    {
        // Skip if already on setup route
        if (context.Request.Path.StartsWithSegments("/setup"))
        {
            await _next(context);
            return;
        }

        // Skip if PROBOTSHARP_SKIP_SETUP=true
        var skipSetup = Environment.GetEnvironmentVariable("PROBOTSHARP_SKIP_SETUP");
        if (skipSetup?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
        {
            await _next(context);
            return;
        }

        // Skip for health checks, webhooks, and root API endpoint
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/webhooks") ||
            context.Request.Path.Equals("/", StringComparison.Ordinal))
        {
            await _next(context);
            return;
        }

        try
        {
            // Check if configured
            var manifestResult = await manifestPort.GetAsync();
            var isConfigured = manifestResult.IsSuccess && !string.IsNullOrWhiteSpace(manifestResult.Value);

            if (!isConfigured)
            {
                _logger.LogInformation("App not configured, redirecting to /setup");
                context.Response.Redirect("/setup");
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking configuration status, allowing request to proceed");
            // If we can't check, allow the request to proceed
        }

        await _next(context);
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
