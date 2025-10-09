// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Diagnostics;

using ProbotSharp.Application.Ports.Outbound;

namespace ProbotSharp.Infrastructure.Adapters.Observability;

/// <summary>
/// No-op implementation of ITracingPort that performs no operations.
/// Used when distributed tracing is disabled.
/// </summary>
public sealed class NoOpTracingAdapter : ITracingPort
{
    /// <inheritdoc />
    public Activity? StartActivity(
        string name,
        ActivityKind kind = ActivityKind.Internal,
        ActivityContext parentContext = default,
        params KeyValuePair<string, object?>[] tags)
    {
        return null;
    }

    /// <inheritdoc />
    public void AddEvent(string name, params KeyValuePair<string, object?>[] attributes)
    {
        // No-op
    }

    /// <inheritdoc />
    public void AddTags(params KeyValuePair<string, object?>[] tags)
    {
        // No-op
    }

    /// <inheritdoc />
    public void RecordException(Exception exception)
    {
        // No-op
    }

    /// <inheritdoc />
    public ActivityContext GetCurrentContext()
    {
        return default;
    }
}
