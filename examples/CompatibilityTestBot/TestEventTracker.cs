using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;

namespace CompatibilityTestBot;

/// <summary>
/// Represents a tracked webhook event for testing purposes.
/// </summary>
public record TrackedEvent
{
    public required string EventName { get; init; }
    public string? Action { get; init; }
    public required string DeliveryId { get; init; }
    public required JObject Payload { get; init; }
    public required DateTime ReceivedAt { get; init; }
}

/// <summary>
/// Thread-safe in-memory event tracking service for compatibility testing.
/// Stores all received webhook events and provides query/filter capabilities.
/// </summary>
public class TestEventTracker
{
    private readonly ConcurrentBag<TrackedEvent> _events = new();
    private readonly ILogger<TestEventTracker> _logger;

    public TestEventTracker(ILogger<TestEventTracker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Add a new event to the tracker.
    /// </summary>
    public void AddEvent(TrackedEvent trackedEvent)
    {
        ArgumentNullException.ThrowIfNull(trackedEvent);

        _events.Add(trackedEvent);

        _logger.LogInformation(
            "Tracked event: {EventName} (action: {Action}, delivery: {DeliveryId})",
            trackedEvent.EventName,
            trackedEvent.Action ?? "null",
            trackedEvent.DeliveryId);
    }

    /// <summary>
    /// Get all tracked events.
    /// </summary>
    public IReadOnlyList<TrackedEvent> GetAllEvents()
    {
        return _events.ToList();
    }

    /// <summary>
    /// Get events filtered by event name.
    /// </summary>
    public IReadOnlyList<TrackedEvent> GetEventsByName(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            throw new ArgumentException("Event name cannot be null or whitespace.", nameof(eventName));
        }

        return _events
            .Where(e => e.EventName.Equals(eventName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Clear all tracked events.
    /// </summary>
    public void ClearEvents()
    {
        _events.Clear();
        _logger.LogInformation("All tracked events cleared");
    }

    /// <summary>
    /// Get the count of tracked events.
    /// </summary>
    public int Count => _events.Count;
}
