using System.Text.Json;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.Models;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

#pragma warning disable CA1848 // Performance: LoggerMessage delegates - not performance-critical for this codebase

namespace ProbotSharp.Application.Services;

/// <summary>
/// Service for loading and merging repository-backed configuration.
/// Implements Probot's context.config() semantics with cascading and _extends support.
/// </summary>
public sealed class RepositoryConfigurationService
{
    private readonly IRepositoryContentPort _contentPort;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RepositoryConfigurationService> _logger;

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryConfigurationService"/> class.
    /// </summary>
    /// <param name="contentPort">The repository content port.</param>
    /// <param name="cache">The memory cache.</param>
    /// <param name="logger">The logger.</param>
    public RepositoryConfigurationService(
        IRepositoryContentPort contentPort,
        IMemoryCache cache,
        ILogger<RepositoryConfigurationService> logger)
    {
        this._contentPort = contentPort;
        this._cache = cache;
        this._logger = logger;
    }

    /// <summary>
    /// Loads configuration with cascading and extends support.
    /// Resolution order: (1) repo root → (2) repo .github/ → (3) org .github repo → (4) _extends chain.
    /// </summary>
    public async Task<Result<T>> GetConfigAsync<T>(
        string owner,
        string repository,
        long installationId,
        string? fileName = null,
        T? defaultConfig = null,
        RepositoryConfigurationOptions? options = null,
        CancellationToken cancellationToken = default) where T : class
    {
        options ??= RepositoryConfigurationOptions.Default;
        fileName ??= options.DefaultFileName;

        this._logger.LogDebug(
            "Loading configuration {FileName} for {Owner}/{Repository}",
            fileName, owner, repository);

        // Try cascade: root → .github → org
        var mergedConfig = await this.LoadWithCascadeAsync(
            owner, repository, installationId, fileName, options, cancellationToken).ConfigureAwait(false);

        if (mergedConfig == null)
        {
            this._logger.LogDebug(
                "No configuration found for {Owner}/{Repository}/{FileName}, using default",
                owner, repository, fileName);

            return defaultConfig != null
                ? Result<T>.Success(defaultConfig)
                : Result<T>.Failure("NotFound", $"Configuration file {fileName} not found");
        }

        // Handle _extends if enabled
        if (options.EnableExtendsKey && mergedConfig.ContainsKey("_extends"))
        {
            mergedConfig = await this.ResolveExtendsAsync(
                mergedConfig, owner, repository, installationId, options, cancellationToken, depth: 0).ConfigureAwait(false);
        }

        // Merge with defaults if provided
        if (defaultConfig != null)
        {
            var defaultDict = ConvertToDict(defaultConfig);
            mergedConfig = DeepMerge(defaultDict, mergedConfig, options.ArrayMergeStrategy);
        }

        // Deserialize to target type
        try
        {
            var json = JsonSerializer.Serialize(mergedConfig);
            var result = JsonSerializer.Deserialize<T>(json, s_jsonOptions);

            return result != null
                ? Result<T>.Success(result)
                : Result<T>.Failure("Validation", "Failed to deserialize configuration");
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to deserialize configuration to type {Type}", typeof(T).Name);
            return Result<T>.Failure("Validation", $"Configuration deserialization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads configuration as a dictionary (untyped).
    /// </summary>
    public async Task<Result<Dictionary<string, object>>> GetConfigAsync(
        string owner,
        string repository,
        long installationId,
        string? fileName = null,
        Dictionary<string, object>? defaultConfig = null,
        RepositoryConfigurationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= RepositoryConfigurationOptions.Default;
        fileName ??= options.DefaultFileName;

        var mergedConfig = await this.LoadWithCascadeAsync(
            owner, repository, installationId, fileName, options, cancellationToken).ConfigureAwait(false);

        if (mergedConfig == null)
        {
            return defaultConfig != null
                ? Result<Dictionary<string, object>>.Success(defaultConfig)
                : Result<Dictionary<string, object>>.Failure("NotFound", $"Configuration file {fileName} not found");
        }

        if (options.EnableExtendsKey && mergedConfig.ContainsKey("_extends"))
        {
            mergedConfig = await this.ResolveExtendsAsync(
                mergedConfig, owner, repository, installationId, options, cancellationToken, depth: 0).ConfigureAwait(false);
        }

        if (defaultConfig != null)
        {
            mergedConfig = DeepMerge(defaultConfig, mergedConfig, options.ArrayMergeStrategy);
        }

        return Result<Dictionary<string, object>>.Success(mergedConfig);
    }

    private async Task<Dictionary<string, object>?> LoadWithCascadeAsync(
        string owner,
        string repository,
        long installationId,
        string fileName,
        RepositoryConfigurationOptions options,
        CancellationToken cancellationToken)
    {
        Dictionary<string, object>? merged = null;

        // 1. Try repository root
        var rootPath = RepositoryConfigPath.ForRoot(fileName, owner, repository);
        var rootConfig = await this.LoadSingleConfigAsync(rootPath, installationId, cancellationToken).ConfigureAwait(false);
        if (rootConfig != null)
        {
            this._logger.LogDebug("Loaded config from repository root: {Path}", rootPath);
            merged = rootConfig;
        }

        // 2. Try repository .github directory (cascade)
        if (options.EnableGitHubDirectoryCascade)
        {
            var githubPath = RepositoryConfigPath.ForGitHubDirectory(fileName, owner, repository);
            var githubConfig = await this.LoadSingleConfigAsync(githubPath, installationId, cancellationToken).ConfigureAwait(false);
            if (githubConfig != null)
            {
                this._logger.LogDebug("Loaded config from .github directory: {Path}", githubPath);
                merged = merged != null
                    ? DeepMerge(githubConfig, merged, options.ArrayMergeStrategy)
                    : githubConfig;
            }
        }

        // 3. Try organization .github repository (fallback)
        if (options.EnableOrganizationConfig)
        {
            var orgPath = RepositoryConfigPath.ForOrganization(fileName, owner);
            var orgConfig = await this.LoadSingleConfigAsync(orgPath, installationId, cancellationToken).ConfigureAwait(false);
            if (orgConfig != null)
            {
                this._logger.LogDebug("Loaded config from organization .github: {Path}", orgPath);
                merged = merged != null
                    ? DeepMerge(orgConfig, merged, options.ArrayMergeStrategy)
                    : orgConfig;
            }
        }

        return merged;
    }

    private async Task<Dictionary<string, object>?> LoadSingleConfigAsync(
        RepositoryConfigPath path,
        long installationId,
        CancellationToken cancellationToken)
    {
        // Try cache first
        var cacheKey = path.GetCacheKey();
        if (this._cache.TryGetValue<string>(cacheKey, out var cached) && cached != null)
        {
            this._logger.LogTrace("Configuration cache hit: {CacheKey}", cacheKey);
            return ParseYaml(cached);
        }

        // Fetch from GitHub
        var result = await this._contentPort.GetFileContentAsync(path, installationId, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            if (result.Error?.Code == "NotFound")
            {
                this._logger.LogTrace("Configuration file not found: {Path}", path);
            }
            else
            {
                this._logger.LogWarning("Failed to load configuration from {Path}: {Error}", path, result.Error?.Message);
            }
            return null;
        }

        var configData = result.Value!;

        // Cache for future requests
        this._cache.Set(cacheKey, configData.Content, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return ParseYaml(configData.Content);
    }

    private async Task<Dictionary<string, object>> ResolveExtendsAsync(
        Dictionary<string, object> config,
        string owner,
        string repository,
        long installationId,
        RepositoryConfigurationOptions options,
        CancellationToken cancellationToken,
        int depth)
    {
        if (depth >= options.MaxExtendsDepth)
        {
            this._logger.LogWarning("Max _extends depth ({Depth}) reached, stopping resolution", depth);
            return config;
        }

        if (!config.TryGetValue("_extends", out var extendsValue))
        {
            return config;
        }

        var extendsPath = extendsValue?.ToString();
        if (string.IsNullOrWhiteSpace(extendsPath))
        {
            return config;
        }

        this._logger.LogDebug("Resolving _extends: {ExtendsPath} (depth: {Depth})", extendsPath, depth);

        // Parse extends path (can be "owner/repo" or "owner/repo:file.yml")
        var (extOwner, extRepo, extFile) = ParseExtendsPath(extendsPath, owner);

        var extPath = RepositoryConfigPath.ForRoot(extFile, extOwner, extRepo);
        var parentConfig = await this.LoadSingleConfigAsync(extPath, installationId, cancellationToken).ConfigureAwait(false);

        if (parentConfig == null)
        {
            this._logger.LogWarning("Failed to resolve _extends from {ExtendsPath}", extendsPath);
            return config;
        }

        // Recursively resolve parent's _extends
        if (parentConfig.ContainsKey("_extends"))
        {
            parentConfig = await this.ResolveExtendsAsync(
                parentConfig, extOwner, extRepo, installationId, options, cancellationToken, depth + 1).ConfigureAwait(false);
        }

        // Remove _extends key before merging
        config.Remove("_extends");

        // Child config overrides parent config
        return DeepMerge(parentConfig, config, options.ArrayMergeStrategy);
    }

    private static (string owner, string repo, string file) ParseExtendsPath(string extendsPath, string defaultOwner)
    {
        // Format: "owner/repo" or "owner/repo:file.yml" or "repo" or "repo:file.yml"
        var parts = extendsPath.Split(':', 2);
        var repoPath = parts[0];
        var fileName = parts.Length > 1 ? parts[1] : "config.yml";

        if (repoPath.Contains('/'))
        {
            var repoParts = repoPath.Split('/', 2);
            return (repoParts[0], repoParts[1], fileName);
        }

        // No owner specified, use current owner
        return (defaultOwner, repoPath, fileName);
    }

    private static Dictionary<string, object>? ParseYaml(string yaml)
    {
        try
        {
            var obj = YamlDeserializer.Deserialize<object>(yaml);
            if (obj is Dictionary<object, object> dict)
            {
                return dict.ToDictionary(
                    kvp => kvp.Key.ToString() ?? string.Empty,
                    kvp => kvp.Value);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, object> ConvertToDict(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
    }

    private static Dictionary<string, object> DeepMerge(
        Dictionary<string, object> parent,
        Dictionary<string, object> child,
        ArrayMergeStrategy arrayStrategy)
    {
        var result = new Dictionary<string, object>(parent);

        foreach (var kvp in child)
        {
            if (!result.ContainsKey(kvp.Key))
            {
                result[kvp.Key] = kvp.Value;
                continue;
            }

            var parentValue = result[kvp.Key];
            var childValue = kvp.Value;

            // Both are dictionaries - recursively merge
            if (parentValue is Dictionary<string, object> parentDict &&
                childValue is Dictionary<string, object> childDict)
            {
                result[kvp.Key] = DeepMerge(parentDict, childDict, arrayStrategy);
                continue;
            }

            // Both are arrays - apply merge strategy
            if (parentValue is List<object> parentList &&
                childValue is List<object> childList)
            {
                result[kvp.Key] = arrayStrategy switch
                {
                    ArrayMergeStrategy.Concatenate => parentList.Concat(childList).ToList(),
                    ArrayMergeStrategy.DeepMergeByIndex => MergeArraysByIndex(parentList, childList, arrayStrategy),
                    _ => childList // Replace (default)
                };
                continue;
            }

            // Otherwise, child overrides parent
            result[kvp.Key] = childValue;
        }

        return result;
    }

    private static List<object> MergeArraysByIndex(
        List<object> parent,
        List<object> child,
        ArrayMergeStrategy strategy)
    {
        var result = new List<object>();
        var maxLength = Math.Max(parent.Count, child.Count);

        for (int i = 0; i < maxLength; i++)
        {
            if (i >= parent.Count)
            {
                result.Add(child[i]);
            }
            else if (i >= child.Count)
            {
                result.Add(parent[i]);
            }
            else if (parent[i] is Dictionary<string, object> parentDict &&
                     child[i] is Dictionary<string, object> childDict)
            {
                result.Add(DeepMerge(parentDict, childDict, strategy));
            }
            else
            {
                result.Add(child[i]); // Child overrides
            }
        }

        return result;
    }
}

#pragma warning restore CA1848
