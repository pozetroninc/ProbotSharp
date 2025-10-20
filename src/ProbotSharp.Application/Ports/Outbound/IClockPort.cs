// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Port for accessing system time (for testability).
/// </summary>
public interface IClockPort
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
