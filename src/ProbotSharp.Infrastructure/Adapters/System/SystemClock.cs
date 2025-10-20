// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Ports.Outbound;

namespace ProbotSharp.Infrastructure.Adapters.System;

/// <summary>
/// Provides the system UTC clock implementation for the application layer.
/// </summary>
public sealed class SystemClock : IClockPort
{
    /// <summary>
    /// Gets the current UTC date and time from the system clock.
    /// </summary>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
