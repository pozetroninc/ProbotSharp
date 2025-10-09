// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Context;

/// <summary>
/// Represents repository information extracted from a webhook payload.
/// </summary>
public sealed class RepositoryInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryInfo"/> class.
    /// </summary>
    /// <param name="id">The repository ID.</param>
    /// <param name="name">The repository name.</param>
    /// <param name="owner">The repository owner.</param>
    /// <param name="fullName">The full name (owner/repo).</param>
    public RepositoryInfo(long id, string name, string owner, string fullName)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Repository ID must be positive");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        this.Id = id;
        this.Name = name;
        this.Owner = owner;
        this.FullName = fullName;
    }

    /// <summary>
    /// Gets the repository ID.
    /// </summary>
    public long Id { get; }

    /// <summary>
    /// Gets the repository name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the repository owner.
    /// </summary>
    public string Owner { get; }

    /// <summary>
    /// Gets the full repository name (owner/repo).
    /// </summary>
    public string FullName { get; }
}
