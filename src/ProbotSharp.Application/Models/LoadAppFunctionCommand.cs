// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Represents a command to load a ProbotSharp application function.
/// </summary>
/// <param name="AppPath">The path to the app function to load.</param>
/// <param name="IsDefault">Whether this is the default app function.</param>
public sealed record class LoadAppFunctionCommand(
    string AppPath,
    bool IsDefault = false)
{
    /// <summary>
    /// Gets the path to the app function to load.
    /// </summary>
    public string AppPath { get; } = !string.IsNullOrWhiteSpace(AppPath)
        ? AppPath
        : throw new ArgumentException("App path cannot be null or whitespace.", nameof(AppPath));
}
