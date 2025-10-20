// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using FluentAssertions;

using ProbotSharp.Infrastructure.Adapters.Observability;

using Serilog;
using Serilog.Events;
using Serilog.Sinks.TestCorrelator;

using Xunit;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Observability;

public class SerilogLoggingAdapterTests : IDisposable
{
    private readonly Serilog.ILogger _logger;
    private readonly SerilogLoggingAdapter _adapter;
    private readonly IDisposable _testCorrelator;

    public SerilogLoggingAdapterTests()
    {
        _logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Verbose()
            .WriteTo.TestCorrelator()
            .CreateLogger();

        _adapter = new SerilogLoggingAdapter(_logger);
        _testCorrelator = TestCorrelator.CreateContext();
    }

    public void Dispose()
    {
        _testCorrelator?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SerilogLoggingAdapter(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void LogTrace_LogsMessageAtVerboseLevel()
    {
        // Arrange
        const string message = "Test trace message with {Property}";
        const string propertyValue = "value";

        // Act
        _adapter.LogTrace(message, propertyValue);

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        logEvents.Should().HaveCount(1);
        logEvents[0].Level.Should().Be(LogEventLevel.Verbose);
        logEvents[0].MessageTemplate.Text.Should().Be(message);
        logEvents[0].Properties.Should().ContainKey("Property");
        logEvents[0].Properties["Property"].ToString().Should().Contain(propertyValue);
    }

    [Fact]
    public void LogDebug_LogsMessageAtDebugLevel()
    {
        // Arrange
        const string message = "Test debug message with {Count}";
        const int count = 42;

        // Act
        _adapter.LogDebug(message, count);

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        logEvents.Should().HaveCount(1);
        logEvents[0].Level.Should().Be(LogEventLevel.Debug);
        logEvents[0].MessageTemplate.Text.Should().Be(message);
    }

    [Fact]
    public void LogInformation_LogsMessageAtInformationLevel()
    {
        // Arrange
        const string message = "Test information message";

        // Act
        _adapter.LogInformation(message);

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        logEvents.Should().HaveCount(1);
        logEvents[0].Level.Should().Be(LogEventLevel.Information);
        logEvents[0].MessageTemplate.Text.Should().Be(message);
    }

    [Fact]
    public void LogWarning_LogsMessageAtWarningLevel()
    {
        // Arrange
        const string message = "Test warning message with {Item}";
        const string item = "test-item";

        // Act
        _adapter.LogWarning(message, item);

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        logEvents.Should().HaveCount(1);
        logEvents[0].Level.Should().Be(LogEventLevel.Warning);
        logEvents[0].MessageTemplate.Text.Should().Be(message);
    }

    [Fact]
    public void LogError_WithException_LogsMessageAndExceptionAtErrorLevel()
    {
        // Arrange
        const string message = "Test error message";
        var exception = new InvalidOperationException("Test exception");

        // Act
        _adapter.LogError(exception, message);

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        logEvents.Should().HaveCount(1);
        logEvents[0].Level.Should().Be(LogEventLevel.Error);
        logEvents[0].MessageTemplate.Text.Should().Be(message);
        logEvents[0].Exception.Should().Be(exception);
    }

    [Fact]
    public void LogError_WithoutException_LogsMessageAtErrorLevel()
    {
        // Arrange
        const string message = "Test error message without exception";

        // Act
        _adapter.LogError(null, message);

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        logEvents.Should().HaveCount(1);
        logEvents[0].Level.Should().Be(LogEventLevel.Error);
        logEvents[0].MessageTemplate.Text.Should().Be(message);
        logEvents[0].Exception.Should().BeNull();
    }

    [Fact]
    public void LogCritical_WithException_LogsMessageAndExceptionAtFatalLevel()
    {
        // Arrange
        const string message = "Test critical message";
        var exception = new Exception("Critical exception");

        // Act
        _adapter.LogCritical(exception, message);

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        logEvents.Should().HaveCount(1);
        logEvents[0].Level.Should().Be(LogEventLevel.Fatal);
        logEvents[0].MessageTemplate.Text.Should().Be(message);
        logEvents[0].Exception.Should().Be(exception);
    }

    [Fact]
    public void LogCritical_WithoutException_LogsMessageAtFatalLevel()
    {
        // Arrange
        const string message = "Test critical message without exception";

        // Act
        _adapter.LogCritical(null, message);

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        logEvents.Should().HaveCount(1);
        logEvents[0].Level.Should().Be(LogEventLevel.Fatal);
        logEvents[0].MessageTemplate.Text.Should().Be(message);
        logEvents[0].Exception.Should().BeNull();
    }

    [Fact]
    public void BeginScope_WithAnonymousObject_EnrichesLogsWithProperties()
    {
        // Arrange
        var scopeState = new { DeliveryId = "12345", EventName = "push" };

        // Act
        using (_adapter.BeginScope(scopeState))
        {
            _adapter.LogInformation("Message within scope");
        }

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        logEvents.Should().HaveCount(1);
        logEvents[0].Properties.Should().ContainKey("DeliveryId");
        logEvents[0].Properties["DeliveryId"].ToString().Should().Contain("12345");
        logEvents[0].Properties.Should().ContainKey("EventName");
        logEvents[0].Properties["EventName"].ToString().Should().Contain("push");
    }

    [Fact]
    public void BeginScope_WithDictionary_EnrichesLogsWithProperties()
    {
        // Arrange
        var scopeState = new Dictionary<string, object>
        {
            ["RequestId"] = "req-123",
            ["UserId"] = 456
        };

        // Act
        using (_adapter.BeginScope(scopeState))
        {
            _adapter.LogInformation("Message within scope");
        }

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        logEvents.Should().HaveCount(1);
        logEvents[0].Properties.Should().ContainKey("RequestId");
        logEvents[0].Properties["RequestId"].ToString().Should().Contain("req-123");
        logEvents[0].Properties.Should().ContainKey("UserId");
    }

    [Fact]
    public void BeginScope_WhenDisposed_RemovesPropertiesFromContext()
    {
        // Arrange
        var scopeState = new { CorrelationId = "corr-123" };

        // Act
        using (_adapter.BeginScope(scopeState))
        {
            _adapter.LogInformation("Message within scope");
        }

        _adapter.LogInformation("Message after scope");

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        logEvents.Should().HaveCount(2);

        // First message should have the scope property
        logEvents[0].Properties.Should().ContainKey("CorrelationId");

        // Second message should not have the scope property
        logEvents[1].Properties.Should().NotContainKey("CorrelationId");
    }

    [Fact]
    public void BeginScope_WithNestedScopes_AccumulatesProperties()
    {
        // Arrange
        var outerScope = new { OuterId = "outer" };
        var innerScope = new { InnerId = "inner" };

        // Act
        using (_adapter.BeginScope(outerScope))
        {
            using (_adapter.BeginScope(innerScope))
            {
                _adapter.LogInformation("Message in nested scope");
            }
        }

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        logEvents.Should().HaveCount(1);
        logEvents[0].Properties.Should().ContainKey("OuterId");
        logEvents[0].Properties.Should().ContainKey("InnerId");
    }

    [Fact]
    public void BeginScope_WithNullState_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _adapter.BeginScope<object>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("state");
    }

    [Fact]
    public void LogInformation_WithMultipleArguments_FormatsCorrectly()
    {
        // Arrange
        const string message = "Processing {EventType} for {Repository} with {Count} items";
        const string eventType = "push";
        const string repository = "owner/repo";
        const int count = 10;

        // Act
        _adapter.LogInformation(message, eventType, repository, count);

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        logEvents.Should().HaveCount(1);
        logEvents[0].MessageTemplate.Text.Should().Be(message);
        logEvents[0].Properties.Should().ContainKeys("EventType", "Repository", "Count");
    }

    [Fact]
    public void Adapter_HasCorrectSourceContext()
    {
        // Act
        _adapter.LogInformation("Test message");

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        logEvents.Should().HaveCount(1);
        logEvents[0].Properties.Should().ContainKey("SourceContext");
        logEvents[0].Properties["SourceContext"].ToString().Should().Contain("ProbotSharp.Application");
    }
}
