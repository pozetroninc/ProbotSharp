// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Context;

namespace ProbotSharp.Application.Abstractions;

/// <summary>
/// Configures ProbotSharpContext instances during creation.
/// Implementations can attach services to the context metadata.
/// </summary>
public interface IProbotSharpContextConfigurator
{
    /// <summary>
    /// Configures the given ProbotSharpContext instance.
    /// </summary>
    /// <param name="context">The context to configure.</param>
    void Configure(ProbotSharpContext context);
}
