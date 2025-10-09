// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Diagnostics;

using ProbotSharp.Application.Ports.Outbound;

namespace ProbotSharp.Infrastructure.Adapters.Observability;

/// <summary>
/// Distributed tracing adapter using System.Diagnostics.ActivitySource.
/// Provides OpenTelemetry-compatible tracing without requiring the OpenTelemetry SDK.
/// Activities created by this adapter can be exported by OpenTelemetry exporters when configured.
/// </summary>
public sealed class ActivitySourceTracingAdapter : ITracingPort, IDisposable
{
    private readonly ActivitySource _activitySource;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivitySourceTracingAdapter"/> class.
    /// </summary>
    /// <param name="sourceName">The name of the activity source (typically the service name).</param>
    /// <param name="version">Optional version string for the activity source.</param>
    public ActivitySourceTracingAdapter(string sourceName = "ProbotSharp", string? version = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        this._activitySource = new ActivitySource(sourceName, version);
    }

    /// <inheritdoc />
    public Activity? StartActivity(
        string name,
        ActivityKind kind = ActivityKind.Internal,
        ActivityContext parentContext = default,
        params KeyValuePair<string, object?>[] tags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(tags);
        ObjectDisposedException.ThrowIf(this._disposed, this);

        var activity = parentContext == default
            ? this._activitySource.StartActivity(name, kind)
            : this._activitySource.StartActivity(name, kind, parentContext);

        if (activity != null && tags.Length > 0)
        {
            foreach (var tag in tags)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }

        return activity;
    }

    /// <inheritdoc />
    public void AddEvent(string name, params KeyValuePair<string, object?>[] attributes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(attributes);
        ObjectDisposedException.ThrowIf(this._disposed, this);

        var currentActivity = Activity.Current;
        if (currentActivity == null)
        {
            return;
        }

        var activityEvent = attributes.Length > 0
            ? new ActivityEvent(name, tags: new ActivityTagsCollection(attributes))
            : new ActivityEvent(name);

        currentActivity.AddEvent(activityEvent);
    }

    /// <inheritdoc />
    public void AddTags(params KeyValuePair<string, object?>[] tags)
    {
        ArgumentNullException.ThrowIfNull(tags);
        ObjectDisposedException.ThrowIf(this._disposed, this);

        var currentActivity = Activity.Current;
        if (currentActivity == null)
        {
            return;
        }

        foreach (var tag in tags)
        {
            currentActivity.SetTag(tag.Key, tag.Value);
        }
    }

    /// <inheritdoc />
    public void RecordException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ObjectDisposedException.ThrowIf(this._disposed, this);

        var currentActivity = Activity.Current;
        if (currentActivity == null)
        {
            return;
        }

        // Record exception as an event with standard OpenTelemetry semantic conventions
        var tags = new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.StackTrace },
        };

        currentActivity.AddEvent(new ActivityEvent("exception", tags: tags));
        currentActivity.SetStatus(ActivityStatusCode.Error, exception.Message);
    }

    /// <inheritdoc />
    public ActivityContext GetCurrentContext()
    {
        ObjectDisposedException.ThrowIf(this._disposed, this);

        var currentActivity = Activity.Current;
        return currentActivity?.Context ?? default;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._activitySource.Dispose();
        this._disposed = true;
    }
}
