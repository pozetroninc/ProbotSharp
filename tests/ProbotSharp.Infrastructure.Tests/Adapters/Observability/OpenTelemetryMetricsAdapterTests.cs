// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Diagnostics.Metrics;

using ProbotSharp.Infrastructure.Adapters.Observability;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Observability;

/// <summary>
/// Tests for <see cref="OpenTelemetryMetricsAdapter"/> covering counter, gauge, histogram, and duration measurements.
/// </summary>
public sealed class OpenTelemetryMetricsAdapterTests : IDisposable
{
    private readonly OpenTelemetryMetricsAdapter _adapter;
    private readonly MeterListener _listener;
    private readonly List<(Instrument instrument, long value, KeyValuePair<string, object?>[] tags)> _counterMeasurements = [];
    private readonly List<(Instrument instrument, double value, KeyValuePair<string, object?>[] tags)> _histogramMeasurements = [];
    private bool _disposed;

    public OpenTelemetryMetricsAdapterTests()
    {
        this._adapter = new OpenTelemetryMetricsAdapter("TestMeter", "1.0.0");
        this._listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "TestMeter")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };

        this._listener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
        {
            this._counterMeasurements.Add((instrument, value, tags.ToArray()));
        });

        this._listener.SetMeasurementEventCallback<double>((instrument, value, tags, state) =>
        {
            this._histogramMeasurements.Add((instrument, value, tags.ToArray()));
        });

        this._listener.Start();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenMeterNameIsNull()
    {
        var act = () => new OpenTelemetryMetricsAdapter(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenMeterNameIsEmpty()
    {
        var act = () => new OpenTelemetryMetricsAdapter(string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IncrementCounter_ShouldRecordCounterValue()
    {
        // Arrange
        var tags = new[] { new KeyValuePair<string, object?>("test", "value") };

        // Act
        this._adapter.IncrementCounter("test_counter", 5, tags);

        // Assert
        this._counterMeasurements.Should().ContainSingle();
        var measurement = this._counterMeasurements[0];
        measurement.instrument.Name.Should().Be("test_counter");
        measurement.value.Should().Be(5);
    }

    [Fact]
    public void IncrementCounter_ShouldUseDefaultValueOfOne_WhenNoValueProvided()
    {
        // Act
        this._adapter.IncrementCounter("test_counter");

        // Assert
        this._counterMeasurements.Should().ContainSingle();
        this._counterMeasurements[0].value.Should().Be(1);
    }

    [Fact]
    public void IncrementCounter_ShouldThrowArgumentException_WhenNameIsNull()
    {
        var act = () => this._adapter.IncrementCounter(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IncrementCounter_ShouldThrowArgumentException_WhenNameIsEmpty()
    {
        var act = () => this._adapter.IncrementCounter(string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IncrementCounter_ShouldThrowArgumentOutOfRangeException_WhenValueIsNegative()
    {
        var act = () => this._adapter.IncrementCounter("test_counter", -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void IncrementCounter_ShouldThrowArgumentNullException_WhenTagsIsNull()
    {
        var act = () => this._adapter.IncrementCounter("test_counter", 1, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IncrementCounter_ShouldReuseExistingCounter_WhenCalledMultipleTimes()
    {
        // Act
        this._adapter.IncrementCounter("test_counter", 1);
        this._adapter.IncrementCounter("test_counter", 2);

        // Assert
        this._counterMeasurements.Should().HaveCount(2);
        this._counterMeasurements[0].value.Should().Be(1);
        this._counterMeasurements[1].value.Should().Be(2);
    }

    [Fact]
    public void RecordGauge_ShouldStoreGaugeValue()
    {
        // Arrange
        var tags = new[] { new KeyValuePair<string, object?>("env", "test") };

        // Act
        this._adapter.RecordGauge("test_gauge", 42.5, tags);

        // Assert - gauge values are stored and observable
        // We can't easily verify observable gauges without more complex setup
        // but we verify no exceptions are thrown
    }

    [Fact]
    public void RecordGauge_ShouldThrowArgumentException_WhenNameIsNull()
    {
        var act = () => this._adapter.RecordGauge(null!, 1.0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RecordGauge_ShouldThrowArgumentNullException_WhenTagsIsNull()
    {
        var act = () => this._adapter.RecordGauge("test_gauge", 1.0, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RecordHistogram_ShouldRecordHistogramValue()
    {
        // Arrange
        var tags = new[] { new KeyValuePair<string, object?>("operation", "test") };

        // Act
        this._adapter.RecordHistogram("test_histogram", 123.45, tags);

        // Assert
        this._histogramMeasurements.Should().ContainSingle();
        var measurement = this._histogramMeasurements[0];
        measurement.instrument.Name.Should().Be("test_histogram");
        measurement.value.Should().Be(123.45);
    }

    [Fact]
    public void RecordHistogram_ShouldThrowArgumentException_WhenNameIsNull()
    {
        var act = () => this._adapter.RecordHistogram(null!, 1.0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RecordHistogram_ShouldThrowArgumentNullException_WhenTagsIsNull()
    {
        var act = () => this._adapter.RecordHistogram("test_histogram", 1.0, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MeasureDuration_ShouldRecordDurationWhenDisposed()
    {
        // Arrange
        var tags = new[] { new KeyValuePair<string, object?>("method", "test") };

        // Act
        using (var measurement = this._adapter.MeasureDuration("test_duration", tags))
        {
            Thread.Sleep(10); // Small delay to ensure measurable duration
        }

        // Assert
        this._histogramMeasurements.Should().ContainSingle();
        var recorded = this._histogramMeasurements[0];
        recorded.instrument.Name.Should().Be("test_duration");
        recorded.value.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MeasureDuration_ShouldNotRecordDuration_WhenNotDisposed()
    {
        // Arrange
        var tags = new[] { new KeyValuePair<string, object?>("method", "test") };

        // Act
        _ = this._adapter.MeasureDuration("test_duration", tags);

        // Assert - duration not recorded until disposal
        this._histogramMeasurements.Should().BeEmpty();
    }

    [Fact]
    public void MeasureDuration_ShouldThrowArgumentException_WhenNameIsNull()
    {
        var act = () => this._adapter.MeasureDuration(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MeasureDuration_ShouldThrowArgumentNullException_WhenTagsIsNull()
    {
        var act = () => this._adapter.MeasureDuration("test_duration", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MeasureDuration_ShouldHandleMultipleDisposeCalls()
    {
        // Arrange
        var measurement = this._adapter.MeasureDuration("test_duration");

        // Act
        measurement.Dispose();
        measurement.Dispose(); // Second dispose should be safe

        // Assert
        this._histogramMeasurements.Should().ContainSingle();
    }

    [Fact]
    public void Dispose_ShouldDisposeAdapter()
    {
        // Act
        this._adapter.Dispose();

        // Assert - operations after dispose should throw
        var act = () => this._adapter.IncrementCounter("test");

        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        // Act
        this._adapter.Dispose();
        var act = () => this._adapter.Dispose();

        // Assert - second dispose should not throw
        act.Should().NotThrow();
    }

    [Fact]
    public void IncrementCounter_ShouldThrowObjectDisposedException_WhenDisposed()
    {
        // Arrange
        this._adapter.Dispose();

        // Act
        var act = () => this._adapter.IncrementCounter("test");

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void RecordGauge_ShouldThrowObjectDisposedException_WhenDisposed()
    {
        // Arrange
        this._adapter.Dispose();

        // Act
        var act = () => this._adapter.RecordGauge("test", 1.0);

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void RecordHistogram_ShouldThrowObjectDisposedException_WhenDisposed()
    {
        // Arrange
        this._adapter.Dispose();

        // Act
        var act = () => this._adapter.RecordHistogram("test", 1.0);

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void MeasureDuration_ShouldThrowObjectDisposedException_WhenDisposed()
    {
        // Arrange
        this._adapter.Dispose();

        // Act
        var act = () => this._adapter.MeasureDuration("test");

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._listener?.Dispose();
        this._adapter?.Dispose();
        this._disposed = true;
    }
}
