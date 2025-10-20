// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

using NSubstitute;

using ProbotSharp.Infrastructure.Adapters.Logging;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Logging;

public sealed class LoggingPortAdapterTests
{
    [Fact]
    public void AllMethods_ShouldForwardToLogger()
    {
        var factory = Substitute.For<ILoggerFactory>();
        var logger = Substitute.For<ILogger>();
        factory.CreateLogger("ProbotSharp.Application").Returns(logger);

        var sut = new LoggingPortAdapter(factory);

        using var scope = sut.BeginScope(new Dictionary<string, object> { ["a"] = 1 });
        sut.LogTrace("trace {value}", 1);
        sut.LogDebug("debug {value}", 2);
        sut.LogInformation("info {value}", 3);
        sut.LogWarning("warn {value}", 4);
        sut.LogError(new InvalidOperationException("e"), "error {value}", 5);
        sut.LogCritical(new InvalidOperationException("c"), "critical {value}", 6);

        factory.Received(1).CreateLogger("ProbotSharp.Application");
        logger.Received().BeginScope(Arg.Any<IDictionary<string, object>>());
        logger.Received().Log(
            LogLevel.Trace,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
