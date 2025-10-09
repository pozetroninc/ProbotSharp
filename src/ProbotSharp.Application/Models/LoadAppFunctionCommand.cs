// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

public sealed record class LoadAppFunctionCommand(
    string AppPath,
    bool IsDefault = false)
{
    public string AppPath { get; } = !string.IsNullOrWhiteSpace(AppPath)
        ? AppPath
        : throw new ArgumentException("App path cannot be null or whitespace.", nameof(AppPath));
}
