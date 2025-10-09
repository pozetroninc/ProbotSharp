// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

public sealed record class StopAppCommand(
    bool GracefulShutdown = true,
    int TimeoutSeconds = 30);
