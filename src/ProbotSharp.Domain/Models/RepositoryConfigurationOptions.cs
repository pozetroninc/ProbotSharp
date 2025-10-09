namespace ProbotSharp.Domain.Models;

/// <summary>
/// Options for repository configuration loading and merging.
/// </summary>
public sealed class RepositoryConfigurationOptions
{
    /// <summary>
    /// Default configuration file name (e.g., "config.yml").
    /// </summary>
    public string DefaultFileName { get; init; } = "config.yml";

    /// <summary>
    /// Enable organization-level configuration fallback.
    /// </summary>
    public bool EnableOrganizationConfig { get; init; } = true;

    /// <summary>
    /// Enable cascading from .github directory.
    /// </summary>
    public bool EnableGitHubDirectoryCascade { get; init; } = true;

    /// <summary>
    /// Enable _extends key for configuration inheritance.
    /// </summary>
    public bool EnableExtendsKey { get; init; } = true;

    /// <summary>
    /// Cache TTL for configuration data.
    /// </summary>
    public TimeSpan CacheTtl { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Array merge strategy for configuration merging.
    /// </summary>
    public ArrayMergeStrategy ArrayMergeStrategy { get; init; } = ArrayMergeStrategy.Replace;

    /// <summary>
    /// Maximum depth for _extends chain to prevent circular references.
    /// </summary>
    public int MaxExtendsDepth { get; init; } = 5;

    /// <summary>
    /// Default instance with standard Probot-compatible settings.
    /// </summary>
    public static readonly RepositoryConfigurationOptions Default = new();
}

/// <summary>
/// Strategy for merging arrays during configuration deep merge.
/// </summary>
public enum ArrayMergeStrategy
{
    /// <summary>
    /// Replace entire array with new value (default).
    /// </summary>
    Replace,

    /// <summary>
    /// Concatenate arrays (parent + child).
    /// </summary>
    Concatenate,

    /// <summary>
    /// Deep merge array elements by index.
    /// </summary>
    DeepMergeByIndex
}
