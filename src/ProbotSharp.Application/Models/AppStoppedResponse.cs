// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Represents the response when an app stops.
/// </summary>
/// <param name="StoppedAt">The timestamp when the app stopped.</param>
/// <param name="WasGraceful">Indicates whether the shutdown was graceful.</param>
/// <param name="Reason">The reason for stopping.</param>
public sealed record class AppStoppedResponse(
    DateTimeOffset StoppedAt,
    bool WasGraceful,
    string? Reason = null);
