// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Represents a command to stop the webhook server.
/// </summary>
/// <param name="Graceful">Whether to perform a graceful shutdown.</param>
public sealed record class StopServerCommand(
    bool Graceful = true);
