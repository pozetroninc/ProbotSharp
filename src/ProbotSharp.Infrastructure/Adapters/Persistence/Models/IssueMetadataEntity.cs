// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Infrastructure.Adapters.Persistence.Models;

/// <summary>
/// Entity representing metadata associated with a GitHub issue or pull request.
/// </summary>
public sealed class IssueMetadataEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this metadata entry.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the owner of the repository (e.g., "octocat").
    /// </summary>
    public required string RepositoryOwner { get; set; }

    /// <summary>
    /// Gets or sets the name of the repository (e.g., "hello-world").
    /// </summary>
    public required string RepositoryName { get; set; }

    /// <summary>
    /// Gets or sets the issue or pull request number.
    /// </summary>
    public required int IssueNumber { get; set; }

    /// <summary>
    /// Gets or sets the metadata key.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Gets or sets the metadata value.
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this metadata entry was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this metadata entry was last updated.
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; set; }
}
