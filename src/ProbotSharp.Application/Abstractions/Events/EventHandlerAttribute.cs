// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Abstractions.Events;

/// <summary>
/// Attribute to decorate event handler classes with event name and action filters.
/// Multiple attributes can be applied to a single handler class to respond to multiple events.
/// Supports wildcard patterns: "*" matches all events, "issues.*" matches all issue actions.
/// </summary>
/// <example>
/// <code>
/// [EventHandler("issues", "opened")]
/// [EventHandler("issues", "reopened")]
/// public class IssueOpenedHandler : IEventHandler
/// {
///     public Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
///     {
///         // Handle issue opened/reopened events
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class EventHandlerAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventHandlerAttribute"/> class.
    /// </summary>
    /// <param name="eventName">The name of the webhook event to handle (e.g., "issues", "pull_request", "*").</param>
    /// <param name="action">The action to filter on (e.g., "opened", "closed"), or null to match all actions.</param>
    public EventHandlerAttribute(string eventName, string? action = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        this.EventName = eventName;
        this.Action = action;
    }

    /// <summary>
    /// Gets the name of the webhook event to handle.
    /// Can be a specific event name like "issues" or a wildcard like "*".
    /// </summary>
    public string EventName { get; }

    /// <summary>
    /// Gets the action to filter on, or null to match all actions.
    /// Can be a specific action like "opened" or a wildcard like "*".
    /// </summary>
    public string? Action { get; }
}
