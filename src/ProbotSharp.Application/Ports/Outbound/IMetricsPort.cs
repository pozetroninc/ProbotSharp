// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Port for recording application metrics and telemetry.
/// Abstracts the underlying metrics/observability implementation (e.g., OpenTelemetry, Prometheus).
/// </summary>
public interface IMetricsPort
{
    /// <summary>
    /// Increments a counter metric by the specified value.
    /// Counters are used to track cumulative values that only increase (e.g., total requests, errors).
    /// </summary>
    /// <param name="name">The name of the counter metric.</param>
    /// <param name="value">The value to increment by (default: 1).</param>
    /// <param name="tags">Optional key-value pairs for metric dimensions/labels.</param>
    void IncrementCounter(string name, long value = 1, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Records a gauge metric with the specified value.
    /// Gauges represent point-in-time measurements that can increase or decrease (e.g., active connections, queue depth).
    /// </summary>
    /// <param name="name">The name of the gauge metric.</param>
    /// <param name="value">The current value to record.</param>
    /// <param name="tags">Optional key-value pairs for metric dimensions/labels.</param>
    void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Records a histogram/distribution metric with the specified value.
    /// Histograms track the distribution of values (e.g., request duration, payload size).
    /// </summary>
    /// <param name="name">The name of the histogram metric.</param>
    /// <param name="value">The value to record in the distribution.</param>
    /// <param name="tags">Optional key-value pairs for metric dimensions/labels.</param>
    void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Measures the duration of an operation and records it as a histogram metric when disposed.
    /// Useful for tracking execution time of methods or operations.
    /// </summary>
    /// <param name="name">The name of the duration metric.</param>
    /// <param name="tags">Optional key-value pairs for metric dimensions/labels.</param>
    /// <returns>A disposable object that records the duration when disposed.</returns>
    /// <example>
    /// <code>
    /// using (metrics.MeasureDuration("webhook.processing.duration", new KeyValuePair&lt;string, object?&gt;("event", "push")))
    /// {
    ///     // Operation to measure
    /// }
    /// </code>
    /// </example>
    IDisposable MeasureDuration(string name, params KeyValuePair<string, object?>[] tags);
}
