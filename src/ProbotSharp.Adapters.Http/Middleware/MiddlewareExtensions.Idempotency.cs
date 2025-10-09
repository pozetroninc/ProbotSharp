// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;

namespace ProbotSharp.Adapters.Http.Middleware;

/// <summary>
/// Extension methods for registering idempotency middleware.
/// </summary>
public static partial class MiddlewareExtensions
{
    /// <summary>
    /// Adds idempotency middleware to the application pipeline.
    /// This middleware prevents duplicate webhook processing by tracking delivery IDs.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<IdempotencyMiddleware>();
    }
}
