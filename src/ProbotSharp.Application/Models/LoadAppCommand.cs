// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

public sealed record class LoadAppCommand(
    string AppPath,
    string? WorkingDirectory = null);
