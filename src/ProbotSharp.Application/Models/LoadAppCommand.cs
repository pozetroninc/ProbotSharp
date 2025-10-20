// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Represents a command to load a ProbotSharp application.
/// </summary>
/// <param name="AppPath">The path to the app to load.</param>
/// <param name="WorkingDirectory">The optional working directory.</param>
public sealed record class LoadAppCommand(
    string AppPath,
    string? WorkingDirectory = null);
