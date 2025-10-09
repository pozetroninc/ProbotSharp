// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Ports.Outbound;

public interface IClockPort
{
    DateTimeOffset UtcNow { get; }
}

