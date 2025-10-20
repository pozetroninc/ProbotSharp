// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Domain.Entities;

/// <summary>
/// Represents a GitHub repository.
/// </summary>
public sealed class Repository : Entity<long>
{
    private Repository(long id, string name, string fullName)
        : base(id)
    {
        this.Name = name;
        this.FullName = fullName;
    }

    /// <summary>
    /// Gets the repository name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the repository full name (owner/repo).
    /// </summary>
    public string FullName { get; private set; }

    /// <summary>
    /// Creates a new repository instance.
    /// </summary>
    /// <param name="id">The repository ID.</param>
    /// <param name="name">The repository name.</param>
    /// <param name="fullName">The full repository name (owner/repo).</param>
    /// <returns>A new repository instance.</returns>
    public static Repository Create(long id, string name, string fullName)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Repository ID must be positive.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Repository name cannot be null or whitespace.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Repository full name cannot be null or whitespace.", nameof(fullName));
        }

        return new Repository(id, name.Trim(), fullName.Trim());
    }

    /// <summary>
    /// Restores a repository instance from persistence.
    /// </summary>
    /// <param name="id">The repository ID.</param>
    /// <param name="name">The repository name.</param>
    /// <param name="fullName">The full repository name.</param>
    /// <returns>A restored repository instance.</returns>
    internal static Repository Restore(long id, string name, string fullName)
        => new(id, name, fullName);

    /// <summary>
    /// Renames the repository.
    /// </summary>
    /// <param name="name">The new repository name.</param>
    /// <param name="fullName">The new full repository name.</param>
    public void Rename(string name, string fullName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Repository name cannot be null or whitespace.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Repository full name cannot be null or whitespace.", nameof(fullName));
        }

        this.Name = name.Trim();
        this.FullName = fullName.Trim();
    }
}
