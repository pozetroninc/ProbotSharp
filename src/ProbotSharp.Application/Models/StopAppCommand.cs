// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Represents a command to stop a running ProbotSharp application.
/// </summary>
/// <param name="GracefulShutdown">Whether to perform a graceful shutdown.</param>
/// <param name="TimeoutSeconds">The timeout in seconds for graceful shutdown.</param>
public sealed record class StopAppCommand(
    bool GracefulShutdown = true,
    int TimeoutSeconds = 30);
