// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Ports.Outbound;

namespace ProbotSharp.Infrastructure.Adapters.System;

/// <summary>
/// Provides the system UTC clock implementation for the application layer.
/// </summary>
public sealed class SystemClock : IClockPort
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

