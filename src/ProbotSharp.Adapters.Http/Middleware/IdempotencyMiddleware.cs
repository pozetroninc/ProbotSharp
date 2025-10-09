// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Adapters.Http.Middleware;

/// <summary>
/// Middleware that prevents duplicate webhook processing using idempotency keys.
/// Uses the X-GitHub-Delivery header to track processed webhooks.
/// </summary>
public sealed class IdempotencyMiddleware
{
    private const string GitHubDeliveryHeader = "X-GitHub-Delivery";
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotencyMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public IdempotencyMiddleware(
        RequestDelegate next,
        ILogger<IdempotencyMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware to check for duplicate webhooks.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="idempotencyPort">The idempotency storage port.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, IIdempotencyPort idempotencyPort)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(idempotencyPort);

        // Only apply idempotency to webhook endpoints
        if (!IsWebhookRequest(context.Request))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Extract delivery ID from headers
        if (!context.Request.Headers.TryGetValue(GitHubDeliveryHeader, out var deliveryIdValue) ||
            string.IsNullOrWhiteSpace(deliveryIdValue))
        {
            _logger.LogWarning(
                "Webhook request missing {Header} header. Idempotency check skipped",
                GitHubDeliveryHeader);
            await _next(context).ConfigureAwait(false);
            return;
        }

        try
        {
            var deliveryId = DeliveryId.Create(deliveryIdValue.ToString());
            var idempotencyKey = IdempotencyKey.FromDeliveryId(deliveryId);

            // Try to acquire the idempotency lock
            var acquired = await idempotencyPort.TryAcquireAsync(
                idempotencyKey,
                timeToLive: TimeSpan.FromHours(24),
                cancellationToken: context.RequestAborted).ConfigureAwait(false);

            if (!acquired)
            {
                // Duplicate webhook delivery
                _logger.LogWarning(
                    "Duplicate webhook delivery detected: {DeliveryId}. Request rejected",
                    deliveryId.Value);

                context.Response.StatusCode = StatusCodes.Status202Accepted;
                await context.Response.WriteAsync(
                    $"Webhook delivery {deliveryId.Value} has already been processed",
                    context.RequestAborted).ConfigureAwait(false);
                return;
            }

            // Process the webhook
            _logger.LogDebug(
                "Idempotency lock acquired for delivery: {DeliveryId}",
                deliveryId.Value);

            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error during idempotency check for delivery: {DeliveryId}",
                deliveryIdValue);

            // Fail open: allow the request to proceed if idempotency check fails
            await _next(context).ConfigureAwait(false);
        }
    }

    private static bool IsWebhookRequest(HttpRequest request)
    {
        // Check if the request path is a webhook endpoint
        return request.Path.StartsWithSegments("/api/webhooks", StringComparison.OrdinalIgnoreCase)
            || request.Path.StartsWithSegments("/webhooks", StringComparison.OrdinalIgnoreCase);
    }
}
