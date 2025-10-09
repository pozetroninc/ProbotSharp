namespace ProbotSharp.Domain.ValueObjects;

/// <summary>
/// Represents raw configuration data loaded from a repository.
/// Immutable value object for configuration content.
/// </summary>
public sealed record RepositoryConfigData
{
    /// <summary>
    /// Gets the raw content (YAML, JSON, etc.).
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Gets the SHA of the file when it was loaded.
    /// </summary>
    public string Sha { get; }

    /// <summary>
    /// Gets the source path this content was loaded from.
    /// </summary>
    public RepositoryConfigPath SourcePath { get; }

    /// <summary>
    /// Gets when this config was loaded.
    /// </summary>
    public DateTimeOffset LoadedAt { get; }

    private RepositoryConfigData(
        string content,
        string sha,
        RepositoryConfigPath sourcePath,
        DateTimeOffset loadedAt)
    {
        Content = content;
        Sha = sha;
        SourcePath = sourcePath;
        LoadedAt = loadedAt;
    }

    /// <summary>
    /// Creates configuration data.
    /// </summary>
    public static RepositoryConfigData Create(
        string content,
        string sha,
        RepositoryConfigPath sourcePath)
    {
        return new RepositoryConfigData(
            content,
            sha,
            sourcePath,
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Checks if this config is stale based on TTL.
    /// </summary>
    public bool IsStale(TimeSpan ttl)
    {
        return DateTimeOffset.UtcNow - LoadedAt > ttl;
    }
}
