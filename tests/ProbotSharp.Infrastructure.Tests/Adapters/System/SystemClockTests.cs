// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Infrastructure.Adapters.System;

namespace ProbotSharp.Infrastructure.Tests.Adapters.System;

public sealed class SystemClockTests
{
    [Fact]
    public void UtcNow_ShouldReturnCurrentUtcTime()
    {
        var sut = new SystemClock();

        var before = DateTimeOffset.UtcNow;
        var value = sut.UtcNow;
        var after = DateTimeOffset.UtcNow;

        value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}
