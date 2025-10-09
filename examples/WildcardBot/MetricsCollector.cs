// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Context;

namespace WildcardBot;

/// <summary>
/// Collects metrics from all webhook events using wildcard pattern.
/// Demonstrates how wildcard handlers can be used for cross-cutting concerns
/// like monitoring, analytics, and observability.
/// </summary>
[EventHandler("*", null)]
public class MetricsCollector : IEventHandler
{
    // In-memory event counter (in production, use proper metrics service)
    private static readonly ConcurrentDictionary<string, long> EventCounts = new();

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        // Construct event key (e.g., "issues.opened", "pull_request.closed")
        var eventKey = context.EventAction != null
            ? $"{context.EventName}.{context.EventAction}"
            : context.EventName;

        // Increment counter
        var count = EventCounts.AddOrUpdate(eventKey, 1, (_, current) => current + 1);

        context.Logger.LogDebug(
            "[MetricsCollector] Event: {EventKey} | Total count: {Count}",
            eventKey,
            count);

        // Example: Log top 5 most common events periodically
        if (count % 10 == 0)
        {
            var topEvents = EventCounts
                .OrderByDescending(kvp => kvp.Value)
                .Take(5)
                .ToList();

            context.Logger.LogInformation(
                "[MetricsCollector] Top 5 events: {TopEvents}",
                string.Join(", ", topEvents.Select(kvp => $"{kvp.Key}={kvp.Value}")));
        }

        // In production, you might:
        // - Send metrics to Application Insights, Prometheus, or DataDog
        // - Record event timing/latency
        // - Track error rates per event type
        // - Monitor payload sizes
        // - Alert on unusual patterns

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets current event counts (for testing/debugging).
    /// </summary>
    public static IReadOnlyDictionary<string, long> GetEventCounts()
    {
        return EventCounts.ToArray().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Resets event counts (for testing).
    /// </summary>
    public static void ResetCounts()
    {
        EventCounts.Clear();
    }
}
