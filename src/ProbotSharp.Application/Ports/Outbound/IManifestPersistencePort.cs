// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Port for persisting GitHub App manifests.
/// </summary>
public interface IManifestPersistencePort
{
    /// <summary>
    /// Saves a GitHub App manifest.
    /// </summary>
    /// <param name="manifestJson">The manifest JSON to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SaveAsync(string manifestJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the stored GitHub App manifest.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The manifest JSON if found; otherwise, null.</returns>
    Task<Result<string?>> GetAsync(CancellationToken cancellationToken = default);
}
