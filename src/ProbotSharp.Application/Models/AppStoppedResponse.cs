// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

public sealed record class AppStoppedResponse(
    DateTimeOffset StoppedAt,
    bool WasGraceful,
    string? Reason = null);
