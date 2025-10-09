// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;

using ProbotSharp.Application.Ports.Outbound;

namespace ProbotSharp.Infrastructure.Adapters.Observability;

/// <summary>
/// Metrics adapter using System.Diagnostics.Metrics for OpenTelemetry integration.
/// Provides instrumentation for counters, gauges, histograms, and duration measurements.
/// </summary>
public sealed class OpenTelemetryMetricsAdapter : IMetricsPort, IDisposable
{
    private readonly Meter _meter;
    private readonly Dictionary<string, Counter<long>> _counters = new();
    private readonly Dictionary<string, Histogram<double>> _histograms = new();
    private readonly Dictionary<string, ObservableGauge<double>> _gauges = new();
    private readonly Dictionary<string, double> _gaugeValues = new();
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenTelemetryMetricsAdapter"/> class with a meter for the specified service name.
    /// </summary>
    /// <param name="meterName">The name of the meter (typically the service name).</param>
    /// <param name="version">Optional version string for the meter.</param>
    public OpenTelemetryMetricsAdapter(string meterName = "ProbotSharp", string? version = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(meterName);
        this._meter = new Meter(meterName, version);
    }

    /// <inheritdoc />
    public void IncrementCounter(string name, long value = 1, params KeyValuePair<string, object?>[] tags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(tags);
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Counter value must be non-negative");
        }

        ObjectDisposedException.ThrowIf(this._disposed, this);

        var counter = this.GetOrCreateCounter(name);
        counter.Add(value, tags);
    }

    /// <inheritdoc />
    public void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(tags);
        ObjectDisposedException.ThrowIf(this._disposed, this);

        lock (this._lock)
        {
            var key = CreateGaugeKey(name, tags);
            this._gaugeValues[key] = value;

            if (!this._gauges.ContainsKey(name))
            {
                var gauge = this._meter.CreateObservableGauge(name, () =>
                {
                    lock (this._lock)
                    {
                        var measurements = new List<Measurement<double>>();
                        foreach (var kvp in this._gaugeValues.Where(kv => kv.Key.StartsWith(name + "|", StringComparison.Ordinal)))
                        {
                            var storedTags = ParseGaugeKey(kvp.Key);
                            measurements.Add(new Measurement<double>(kvp.Value, storedTags));
                        }

                        return measurements;
                    }
                });
                this._gauges[name] = gauge;
            }
        }
    }

    /// <inheritdoc />
    public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(tags);
        ObjectDisposedException.ThrowIf(this._disposed, this);

        var histogram = this.GetOrCreateHistogram(name);
        histogram.Record(value, tags);
    }

    /// <inheritdoc />
    public IDisposable MeasureDuration(string name, params KeyValuePair<string, object?>[] tags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(tags);
        ObjectDisposedException.ThrowIf(this._disposed, this);

        return new DurationMeasurement(this, name, tags);
    }

    private Counter<long> GetOrCreateCounter(string name)
    {
        lock (this._lock)
        {
            if (!this._counters.TryGetValue(name, out var counter))
            {
                counter = this._meter.CreateCounter<long>(name);
                this._counters[name] = counter;
            }

            return counter;
        }
    }

    private Histogram<double> GetOrCreateHistogram(string name)
    {
        lock (this._lock)
        {
            if (!this._histograms.TryGetValue(name, out var histogram))
            {
                histogram = this._meter.CreateHistogram<double>(name);
                this._histograms[name] = histogram;
            }

            return histogram;
        }
    }

    private static string CreateGaugeKey(string name, KeyValuePair<string, object?>[] tags)
    {
        if (tags.Length == 0)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{name}|");
        }

        var tagString = string.Join(',', tags.Select(t => string.Create(CultureInfo.InvariantCulture, $"{t.Key}={t.Value}")));
        return string.Create(CultureInfo.InvariantCulture, $"{name}|{tagString}");
    }

    private static KeyValuePair<string, object?>[] ParseGaugeKey(string key)
    {
        var parts = key.Split('|');
        if (parts.Length < 2 || string.IsNullOrEmpty(parts[1]))
        {
            return Array.Empty<KeyValuePair<string, object?>>();
        }

        var tagPairs = parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries);
        var tags = new List<KeyValuePair<string, object?>>();

        foreach (var pair in tagPairs)
        {
            var kvp = pair.Split('=');
            if (kvp.Length == 2)
            {
                tags.Add(new KeyValuePair<string, object?>(kvp[0], kvp[1]));
            }
        }

        return tags.ToArray();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._meter.Dispose();
        this._disposed = true;
    }

    /// <summary>
    /// Measures the duration of an operation and records it as a histogram when disposed.
    /// </summary>
    private sealed class DurationMeasurement : IDisposable
    {
        private readonly OpenTelemetryMetricsAdapter _adapter;
        private readonly string _name;
        private readonly KeyValuePair<string, object?>[] _tags;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DurationMeasurement"/> class and starts timing.
        /// </summary>
        /// <param name="adapter">The parent metrics adapter.</param>
        /// <param name="name">The name of the measurement.</param>
        /// <param name="tags">The tags to associate with the measurement.</param>
        public DurationMeasurement(
            OpenTelemetryMetricsAdapter adapter,
            string name,
            KeyValuePair<string, object?>[] tags)
        {
            this._adapter = adapter;
            this._name = name;
            this._tags = tags;
            this._stopwatch = Stopwatch.StartNew();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this._disposed)
            {
                return;
            }

            this._stopwatch.Stop();
            var durationMs = this._stopwatch.Elapsed.TotalMilliseconds;
            this._adapter.RecordHistogram(this._name, durationMs, this._tags);
            this._disposed = true;
        }
    }
}
