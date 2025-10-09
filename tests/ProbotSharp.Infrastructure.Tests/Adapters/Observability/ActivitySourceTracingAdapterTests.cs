// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Diagnostics;

using ProbotSharp.Infrastructure.Adapters.Observability;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Observability;

/// <summary>
/// Tests for <see cref="ActivitySourceTracingAdapter"/> covering distributed tracing operations.
/// </summary>
public sealed class ActivitySourceTracingAdapterTests : IDisposable
{
    private readonly ActivitySourceTracingAdapter _adapter;
    private readonly ActivityListener _listener;
    private readonly List<Activity> _startedActivities = [];
    private bool _disposed;

    public ActivitySourceTracingAdapterTests()
    {
        this._adapter = new ActivitySourceTracingAdapter("TestSource", "1.0.0");
        this._listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "TestSource",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => this._startedActivities.Add(activity),
        };

        ActivitySource.AddActivityListener(this._listener);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenSourceNameIsNull()
    {
        var act = () => new ActivitySourceTracingAdapter(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenSourceNameIsEmpty()
    {
        var act = () => new ActivitySourceTracingAdapter(string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StartActivity_ShouldCreateActivity()
    {
        // Arrange
        var tags = new[] { new KeyValuePair<string, object?>("test", "value") };

        // Act
        using var activity = this._adapter.StartActivity("TestOperation", ActivityKind.Internal, default, tags);

        // Assert
        activity.Should().NotBeNull();
        activity!.DisplayName.Should().Be("TestOperation");
        activity.Kind.Should().Be(ActivityKind.Internal);
        activity.Tags.Should().Contain(t => t.Key == "test" && (string?)t.Value == "value");
    }

    [Fact]
    public void StartActivity_ShouldCreateActivityWithoutTags()
    {
        // Act
        using var activity = this._adapter.StartActivity("TestOperation");

        // Assert
        activity.Should().NotBeNull();
        activity!.DisplayName.Should().Be("TestOperation");
    }

    [Fact]
    public void StartActivity_ShouldThrowArgumentException_WhenNameIsNull()
    {
        var act = () => this._adapter.StartActivity(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StartActivity_ShouldThrowArgumentException_WhenNameIsEmpty()
    {
        var act = () => this._adapter.StartActivity(string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StartActivity_ShouldThrowArgumentNullException_WhenTagsIsNull()
    {
        var act = () => this._adapter.StartActivity("test", ActivityKind.Internal, default, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void StartActivity_ShouldCreateActivityWithParentContext()
    {
        // Arrange
        using var parentActivity = this._adapter.StartActivity("ParentOperation");
        var parentContext = parentActivity!.Context;

        // Act
        using var childActivity = this._adapter.StartActivity("ChildOperation", ActivityKind.Internal, parentContext);

        // Assert
        childActivity.Should().NotBeNull();
        childActivity!.ParentId.Should().Be(parentActivity.Id);
    }

    [Fact]
    public void StartActivity_ShouldCreateActivityWithDifferentKinds()
    {
        // Arrange & Act
        using var clientActivity = this._adapter.StartActivity("ClientOp", ActivityKind.Client);
        using var serverActivity = this._adapter.StartActivity("ServerOp", ActivityKind.Server);
        using var producerActivity = this._adapter.StartActivity("ProducerOp", ActivityKind.Producer);
        using var consumerActivity = this._adapter.StartActivity("ConsumerOp", ActivityKind.Consumer);

        // Assert
        clientActivity!.Kind.Should().Be(ActivityKind.Client);
        serverActivity!.Kind.Should().Be(ActivityKind.Server);
        producerActivity!.Kind.Should().Be(ActivityKind.Producer);
        consumerActivity!.Kind.Should().Be(ActivityKind.Consumer);
    }

    [Fact]
    public void AddEvent_ShouldAddEventToCurrentActivity()
    {
        // Arrange
        using var activity = this._adapter.StartActivity("TestOperation");
        var attributes = new[] { new KeyValuePair<string, object?>("key", "value") };

        // Act
        this._adapter.AddEvent("TestEvent", attributes);

        // Assert
        activity.Should().NotBeNull();
        activity!.Events.Should().ContainSingle();
        var ev = activity.Events.First();
        ev.Name.Should().Be("TestEvent");
        ev.Tags.Should().Contain(t => t.Key == "key" && (string?)t.Value == "value");
    }

    [Fact]
    public void AddEvent_ShouldAddEventWithoutAttributes()
    {
        // Arrange
        using var activity = this._adapter.StartActivity("TestOperation");

        // Act
        this._adapter.AddEvent("TestEvent");

        // Assert
        activity!.Events.Should().ContainSingle();
        activity.Events.First().Name.Should().Be("TestEvent");
    }

    [Fact]
    public void AddEvent_ShouldDoNothing_WhenNoCurrentActivity()
    {
        // Act - should not throw
        var act = () => this._adapter.AddEvent("TestEvent");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddEvent_ShouldThrowArgumentException_WhenNameIsNull()
    {
        var act = () => this._adapter.AddEvent(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddEvent_ShouldThrowArgumentNullException_WhenAttributesIsNull()
    {
        var act = () => this._adapter.AddEvent("test", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddTags_ShouldAddTagsToCurrentActivity()
    {
        // Arrange
        using var activity = this._adapter.StartActivity("TestOperation");
        var tags = new[]
        {
            new KeyValuePair<string, object?>("tag1", "value1"),
            new KeyValuePair<string, object?>("tag2", 42),
        };

        // Act
        this._adapter.AddTags(tags);

        // Assert
        activity.Should().NotBeNull();
        activity!.TagObjects.Should().Contain(t => t.Key == "tag1" && (string?)t.Value == "value1");
        activity.TagObjects.Should().Contain(t => t.Key == "tag2" && (int?)t.Value == 42);
    }

    [Fact]
    public void AddTags_ShouldDoNothing_WhenNoCurrentActivity()
    {
        // Arrange
        var tags = new[] { new KeyValuePair<string, object?>("test", "value") };

        // Act - should not throw
        var act = () => this._adapter.AddTags(tags);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddTags_ShouldThrowArgumentNullException_WhenTagsIsNull()
    {
        var act = () => this._adapter.AddTags(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RecordException_ShouldRecordExceptionAsEvent()
    {
        // Arrange
        using var activity = this._adapter.StartActivity("TestOperation");
        var exception = new InvalidOperationException("Test error");

        // Act
        this._adapter.RecordException(exception);

        // Assert
        activity.Should().NotBeNull();
        activity!.Events.Should().ContainSingle();
        var ev = activity.Events.First();
        ev.Name.Should().Be("exception");
        ev.Tags.Should().Contain(t => t.Key == "exception.type" && ((string?)t.Value)!.Contains("InvalidOperationException"));
        ev.Tags.Should().Contain(t => t.Key == "exception.message" && (string?)t.Value == "Test error");
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("Test error");
    }

    [Fact]
    public void RecordException_ShouldDoNothing_WhenNoCurrentActivity()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");

        // Act - should not throw
        var act = () => this._adapter.RecordException(exception);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordException_ShouldThrowArgumentNullException_WhenExceptionIsNull()
    {
        var act = () => this._adapter.RecordException(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetCurrentContext_ShouldReturnCurrentActivityContext()
    {
        // Arrange
        using var activity = this._adapter.StartActivity("TestOperation");

        // Act
        var context = this._adapter.GetCurrentContext();

        // Assert
        context.Should().NotBe(default(ActivityContext));
        context.TraceId.Should().Be(activity!.TraceId);
        context.SpanId.Should().Be(activity.SpanId);
    }

    [Fact]
    public void GetCurrentContext_ShouldReturnDefault_WhenNoCurrentActivity()
    {
        // Act
        var context = this._adapter.GetCurrentContext();

        // Assert
        context.Should().Be(default(ActivityContext));
    }

    [Fact]
    public void Dispose_ShouldDisposeAdapter()
    {
        // Act
        this._adapter.Dispose();

        // Assert - operations after dispose should throw
        var act = () => this._adapter.StartActivity("test");

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
    public void StartActivity_ShouldThrowObjectDisposedException_WhenDisposed()
    {
        // Arrange
        this._adapter.Dispose();

        // Act
        var act = () => this._adapter.StartActivity("test");

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void AddEvent_ShouldThrowObjectDisposedException_WhenDisposed()
    {
        // Arrange
        this._adapter.Dispose();

        // Act
        var act = () => this._adapter.AddEvent("test");

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void AddTags_ShouldThrowObjectDisposedException_WhenDisposed()
    {
        // Arrange
        this._adapter.Dispose();

        // Act
        var act = () => this._adapter.AddTags(Array.Empty<KeyValuePair<string, object?>>());

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void RecordException_ShouldThrowObjectDisposedException_WhenDisposed()
    {
        // Arrange
        this._adapter.Dispose();

        // Act
        var act = () => this._adapter.RecordException(new Exception());

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void GetCurrentContext_ShouldThrowObjectDisposedException_WhenDisposed()
    {
        // Arrange
        this._adapter.Dispose();

        // Act
        var act = () => this._adapter.GetCurrentContext();

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
