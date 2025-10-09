// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Port interface for metadata storage operations.
/// Provides key-value storage scoped to GitHub issues and pull requests.
/// </summary>
public interface IMetadataPort
{
    /// <summary>
    /// Retrieves a metadata value for the specified issue or pull request.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="issueNumber">The issue or pull request number.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The metadata value if found, otherwise null.</returns>
    Task<string?> GetAsync(string owner, string repo, int issueNumber, string key, CancellationToken ct);

    /// <summary>
    /// Sets a metadata value for the specified issue or pull request.
    /// If the metadata already exists, it will be updated; otherwise, a new entry will be created.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="issueNumber">The issue or pull request number.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetAsync(string owner, string repo, int issueNumber, string key, string value, CancellationToken ct);

    /// <summary>
    /// Checks if a metadata entry exists for the specified issue or pull request.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="issueNumber">The issue or pull request number.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the metadata exists, otherwise false.</returns>
    Task<bool> ExistsAsync(string owner, string repo, int issueNumber, string key, CancellationToken ct);

    /// <summary>
    /// Deletes a metadata entry for the specified issue or pull request.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="issueNumber">The issue or pull request number.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(string owner, string repo, int issueNumber, string key, CancellationToken ct);

    /// <summary>
    /// Retrieves all metadata entries for the specified issue or pull request.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="issueNumber">The issue or pull request number.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A dictionary containing all metadata key-value pairs for the issue or pull request.</returns>
    Task<IDictionary<string, string>> GetAllAsync(string owner, string repo, int issueNumber, CancellationToken ct);
}
