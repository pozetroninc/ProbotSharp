namespace ProbotSharp.Domain.ValueObjects;

/// <summary>
/// Represents a path to a configuration file in a repository.
/// Immutable value object for configuration file references.
/// </summary>
public sealed record RepositoryConfigPath
{
    /// <summary>
    /// Gets the file path (e.g., "config.yml", ".github/mybot.yml").
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the repository owner.
    /// </summary>
    public string Owner { get; }

    /// <summary>
    /// Gets the repository name.
    /// </summary>
    public string Repository { get; }

    /// <summary>
    /// Gets the Git reference (branch, tag, or SHA). Optional.
    /// </summary>
    public string? Ref { get; }

    private RepositoryConfigPath(string path, string owner, string repository, string? @ref)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be empty", nameof(path));
        }

        if (string.IsNullOrWhiteSpace(owner))
        {
            throw new ArgumentException("Owner cannot be empty", nameof(owner));
        }

        if (string.IsNullOrWhiteSpace(repository))
        {
            throw new ArgumentException("Repository cannot be empty", nameof(repository));
        }

        // Normalize path - remove leading slashes
        this.Path = path.TrimStart('/');
        this.Owner = owner;
        this.Repository = repository;
        this.Ref = @ref;
    }

    /// <summary>
    /// Creates a repository configuration path.
    /// </summary>
    public static RepositoryConfigPath Create(string path, string owner, string repository, string? @ref = null)
    {
        return new RepositoryConfigPath(path, owner, repository, @ref);
    }

    /// <summary>
    /// Creates a path for the repository root config file.
    /// </summary>
    public static RepositoryConfigPath ForRoot(string fileName, string owner, string repository, string? @ref = null)
    {
        return new RepositoryConfigPath(fileName, owner, repository, @ref);
    }

    /// <summary>
    /// Creates a path for a .github directory config file.
    /// </summary>
    public static RepositoryConfigPath ForGitHubDirectory(string fileName, string owner, string repository, string? @ref = null)
    {
        return new RepositoryConfigPath($".github/{fileName}", owner, repository, @ref);
    }

    /// <summary>
    /// Creates a path for an organization-level .github repository config.
    /// </summary>
    public static RepositoryConfigPath ForOrganization(string fileName, string owner, string? @ref = null)
    {
        return new RepositoryConfigPath($".github/{fileName}", owner, ".github", @ref);
    }

    /// <summary>
    /// Gets the full path with repository coordinates.
    /// </summary>
    public string GetFullPath() => $"{this.Owner}/{this.Repository}/{this.Path}";

    /// <summary>
    /// Gets the cache key for this configuration path.
    /// </summary>
    public string GetCacheKey(string? sha = null)
    {
        var refPart = this.Ref ?? "default";
        var shaPart = sha ?? "latest";
        return $"config:{this.Owner}:{this.Repository}:{refPart}:{shaPart}:{this.Path}";
    }

    /// <summary>
    /// Returns the full path as a string.
    /// </summary>
    /// <returns>The full repository configuration path.</returns>
    public override string ToString() => this.GetFullPath();
}
