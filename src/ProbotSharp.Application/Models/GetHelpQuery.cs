// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Represents a query to get help information.
/// </summary>
/// <param name="CommandName">The optional specific command to get help for.</param>
public sealed record class GetHelpQuery(
    string? CommandName = null);
