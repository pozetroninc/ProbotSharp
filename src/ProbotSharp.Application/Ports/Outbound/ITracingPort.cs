// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Diagnostics;

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Port for distributed tracing and activity tracking.
/// Abstracts the underlying tracing implementation (e.g., OpenTelemetry, Application Insights).
/// </summary>
public interface ITracingPort
{
    /// <summary>
    /// Starts a new activity (span) for tracking an operation.
    /// </summary>
    /// <param name="name">The name of the activity/operation.</param>
    /// <param name="kind">The kind of activity (e.g., Server, Client, Internal).</param>
    /// <param name="parentContext">Optional parent activity context for distributed tracing.</param>
    /// <param name="tags">Optional key-value pairs to attach as tags to the activity.</param>
    /// <returns>The started activity, or null if tracing is disabled.</returns>
    Activity? StartActivity(
        string name,
        ActivityKind kind = ActivityKind.Internal,
        ActivityContext parentContext = default,
        params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Adds an event to the current activity with optional attributes.
    /// </summary>
    /// <param name="name">The name of the event.</param>
    /// <param name="attributes">Optional key-value pairs for event attributes.</param>
    void AddEvent(string name, params KeyValuePair<string, object?>[] attributes);

    /// <summary>
    /// Adds tags to the current activity.
    /// </summary>
    /// <param name="tags">Key-value pairs to add as tags.</param>
    void AddTags(params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Records an exception in the current activity.
    /// </summary>
    /// <param name="exception">The exception to record.</param>
    void RecordException(Exception exception);

    /// <summary>
    /// Gets the current activity context for propagation across service boundaries.
    /// </summary>
    /// <returns>The current activity context, or default if no activity is active.</returns>
    ActivityContext GetCurrentContext();
}
