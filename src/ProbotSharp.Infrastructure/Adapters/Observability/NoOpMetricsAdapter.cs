// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Ports.Outbound;

namespace ProbotSharp.Infrastructure.Adapters.Observability;

/// <summary>
/// No-op metrics adapter used until a concrete observability stack is integrated.
/// </summary>
public sealed class NoOpMetricsAdapter : IMetricsPort
{
    private static readonly IDisposable EmptyScope = new EmptyDisposable();

    /// <inheritdoc />
    public IDisposable MeasureDuration(string name, params KeyValuePair<string, object?>[] tags)
        => EmptyScope;

    /// <inheritdoc />
    public void IncrementCounter(string name, long value = 1, params KeyValuePair<string, object?>[] tags)
    {
    }

    /// <inheritdoc />
    public void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
    }

    /// <inheritdoc />
    public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
    }

    /// <summary>
    /// Empty disposable used for no-op scopes.
    /// </summary>
    private sealed class EmptyDisposable : IDisposable
    {
        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}

