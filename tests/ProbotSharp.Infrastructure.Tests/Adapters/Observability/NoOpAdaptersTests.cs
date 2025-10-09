// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Diagnostics;

using ProbotSharp.Infrastructure.Adapters.Observability;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Observability;

/// <summary>
/// Tests for no-op observability adapters: <see cref="NoOpMetricsAdapter"/> and <see cref="NoOpTracingAdapter"/>.
/// These adapters should perform no operations and handle all calls safely.
/// </summary>
public sealed class NoOpAdaptersTests
{
    [Fact]
    public void NoOpMetricsAdapter_IncrementCounter_ShouldNotThrow()
    {
        // Arrange
        var adapter = new NoOpMetricsAdapter();
        var tags = new[] { new KeyValuePair<string, object?>("test", "value") };

        // Act
        var act = () => adapter.IncrementCounter("test_counter", 5, tags);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void NoOpMetricsAdapter_RecordGauge_ShouldNotThrow()
    {
        // Arrange
        var adapter = new NoOpMetricsAdapter();
        var tags = new[] { new KeyValuePair<string, object?>("test", "value") };

        // Act
        var act = () => adapter.RecordGauge("test_gauge", 42.5, tags);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void NoOpMetricsAdapter_RecordHistogram_ShouldNotThrow()
    {
        // Arrange
        var adapter = new NoOpMetricsAdapter();
        var tags = new[] { new KeyValuePair<string, object?>("test", "value") };

        // Act
        var act = () => adapter.RecordHistogram("test_histogram", 123.45, tags);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void NoOpMetricsAdapter_MeasureDuration_ShouldReturnDisposable()
    {
        // Arrange
        var adapter = new NoOpMetricsAdapter();
        var tags = new[] { new KeyValuePair<string, object?>("test", "value") };

        // Act
        using var scope = adapter.MeasureDuration("test_duration", tags);

        // Assert
        scope.Should().NotBeNull();
    }

    [Fact]
    public void NoOpMetricsAdapter_MeasureDuration_DisposeShouldNotThrow()
    {
        // Arrange
        var adapter = new NoOpMetricsAdapter();
        var scope = adapter.MeasureDuration("test_duration");

        // Act
        var act = () => scope.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void NoOpMetricsAdapter_MeasureDuration_MultipleDisposeShouldNotThrow()
    {
        // Arrange
        var adapter = new NoOpMetricsAdapter();
        var scope = adapter.MeasureDuration("test_duration");

        // Act
        scope.Dispose();
        var act = () => scope.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void NoOpTracingAdapter_StartActivity_ShouldReturnNull()
    {
        // Arrange
        var adapter = new NoOpTracingAdapter();
        var tags = new[] { new KeyValuePair<string, object?>("test", "value") };

        // Act
        var activity = adapter.StartActivity("TestOperation", ActivityKind.Internal, default, tags);

        // Assert
        activity.Should().BeNull();
    }

    [Fact]
    public void NoOpTracingAdapter_StartActivity_WithParentContext_ShouldReturnNull()
    {
        // Arrange
        var adapter = new NoOpTracingAdapter();
        var parentContext = new ActivityContext(
            ActivityTraceId.CreateRandom(),
            ActivitySpanId.CreateRandom(),
            ActivityTraceFlags.None);

        // Act
        var activity = adapter.StartActivity("TestOperation", ActivityKind.Internal, parentContext);

        // Assert
        activity.Should().BeNull();
    }

    [Fact]
    public void NoOpTracingAdapter_AddEvent_ShouldNotThrow()
    {
        // Arrange
        var adapter = new NoOpTracingAdapter();
        var attributes = new[] { new KeyValuePair<string, object?>("key", "value") };

        // Act
        var act = () => adapter.AddEvent("TestEvent", attributes);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void NoOpTracingAdapter_AddEvent_WithoutAttributes_ShouldNotThrow()
    {
        // Arrange
        var adapter = new NoOpTracingAdapter();

        // Act
        var act = () => adapter.AddEvent("TestEvent");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void NoOpTracingAdapter_AddTags_ShouldNotThrow()
    {
        // Arrange
        var adapter = new NoOpTracingAdapter();
        var tags = new[]
        {
            new KeyValuePair<string, object?>("tag1", "value1"),
            new KeyValuePair<string, object?>("tag2", 42),
        };

        // Act
        var act = () => adapter.AddTags(tags);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void NoOpTracingAdapter_RecordException_ShouldNotThrow()
    {
        // Arrange
        var adapter = new NoOpTracingAdapter();
        var exception = new InvalidOperationException("Test error");

        // Act
        var act = () => adapter.RecordException(exception);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void NoOpTracingAdapter_GetCurrentContext_ShouldReturnDefault()
    {
        // Arrange
        var adapter = new NoOpTracingAdapter();

        // Act
        var context = adapter.GetCurrentContext();

        // Assert
        context.Should().Be(default(ActivityContext));
    }

    [Fact]
    public void NoOpTracingAdapter_MultipleOperations_ShouldNotThrow()
    {
        // Arrange
        var adapter = new NoOpTracingAdapter();

        // Act - perform multiple operations
        var act = () =>
        {
            adapter.StartActivity("Op1");
            adapter.AddEvent("Event1");
            adapter.AddTags(new[] { new KeyValuePair<string, object?>("tag", "value") });
            adapter.RecordException(new Exception());
            _ = adapter.GetCurrentContext();
        };

        // Assert
        act.Should().NotThrow();
    }
}
