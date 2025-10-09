using Microsoft.Extensions.Logging;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Context;
using ProbotSharp.Domain.Models;

namespace ProbotSharp.Application.Extensions;

/// <summary>
/// Extension methods for repository configuration access on ProbotSharpContext.
/// Provides Probot-compatible context.config() API.
/// </summary>
public static class ProbotSharpContextConfigExtensions
{
    private const string ConfigServiceKey = "__ConfigService";
    private const string ConfigOptionsKey = "__ConfigOptions";

    /// <summary>
    /// Gets strongly-typed configuration from the repository with cascading support.
    /// Equivalent to Probot's context.config(fileName, defaults).
    /// </summary>
    /// <typeparam name="T">Configuration type.</typeparam>
    /// <param name="context">Probot context.</param>
    /// <param name="fileName">Configuration file name (default: "config.yml").</param>
    /// <param name="defaultConfig">Default configuration if file not found.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Configuration object of type T, or default if file not found.
    /// </returns>
    /// <example>
    /// <code>
    /// var settings = await context.GetConfigAsync&lt;MyBotSettings&gt;("mybot.yml", cancellationToken);
    /// </code>
    /// </example>
    public static async Task<T?> GetConfigAsync<T>(
        this ProbotSharpContext context,
        string? fileName = null,
        T? defaultConfig = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var service = GetConfigService(context);
        if (service == null)
        {
            context.Logger.LogWarning(
                "RepositoryConfigurationService not available. Ensure AddRepositoryConfiguration() is called in DI setup.");
            return defaultConfig;
        }

        var options = GetConfigOptions(context);

        var repo = context.Payload["repository"];
        if (repo == null)
        {
            context.Logger.LogWarning("No repository in webhook payload, cannot load configuration");
            return defaultConfig;
        }

        var owner = repo["owner"]?["login"]?.ToString();
        var repoName = repo["name"]?.ToString();

        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repoName))
        {
            context.Logger.LogWarning("Repository owner or name missing from payload");
            return defaultConfig;
        }

        var installation = context.Payload["installation"];
        if (installation?["id"] == null)
        {
            context.Logger.LogWarning("Installation ID missing from payload");
            return defaultConfig;
        }

        long installationId = (long)installation["id"]!;

        var result = await service.GetConfigAsync(
            owner,
            repoName,
            installationId,
            fileName,
            defaultConfig,
            options,
            cancellationToken);

        if (!result.IsSuccess)
        {
            context.Logger.LogDebug(
                "Failed to load configuration {FileName}: {Error}",
                fileName ?? "config.yml",
                (string?)result.Error?.Message);
            return defaultConfig;
        }

        return result.Value;
    }

    /// <summary>
    /// Gets configuration as a dictionary (untyped).
    /// Useful when configuration structure is dynamic.
    /// </summary>
    /// <param name="context">Probot context.</param>
    /// <param name="fileName">Configuration file name (default: "config.yml").</param>
    /// <param name="defaultConfig">Default configuration if file not found.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration dictionary, or default if file not found.</returns>
    public static async Task<Dictionary<string, object>?> GetConfigAsync(
        this ProbotSharpContext context,
        string? fileName = null,
        Dictionary<string, object>? defaultConfig = null,
        CancellationToken cancellationToken = default)
    {
        var service = GetConfigService(context);
        if (service == null)
        {
            context.Logger.LogWarning(
                "RepositoryConfigurationService not available. Ensure AddRepositoryConfiguration() is called in DI setup.");
            return defaultConfig;
        }

        var options = GetConfigOptions(context);

        var repo = context.Payload["repository"];
        if (repo == null)
        {
            return defaultConfig;
        }

        var owner = repo["owner"]?["login"]?.ToString();
        var repoName = repo["name"]?.ToString();

        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repoName))
        {
            return defaultConfig;
        }

        var installation = context.Payload["installation"];
        if (installation?["id"] == null)
        {
            return defaultConfig;
        }

        long installationId = (long)installation["id"]!;

        var result = await service.GetConfigAsync(
            owner,
            repoName,
            installationId,
            fileName,
            defaultConfig,
            options,
            cancellationToken);

        return result.IsSuccess ? result.Value : defaultConfig;
    }

    /// <summary>
    /// Gets configuration with custom merge options.
    /// Allows control over array merging and cascade behavior.
    /// </summary>
    public static async Task<T?> GetConfigAsync<T>(
        this ProbotSharpContext context,
        string? fileName,
        T? defaultConfig,
        RepositoryConfigurationOptions options,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var service = GetConfigService(context);
        if (service == null)
        {
            return defaultConfig;
        }

        var repo = context.Payload["repository"];
        if (repo == null)
        {
            return defaultConfig;
        }

        var owner = repo["owner"]?["login"]?.ToString();
        var repoName = repo["name"]?.ToString();

        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repoName))
        {
            return defaultConfig;
        }

        var installation = context.Payload["installation"];
        if (installation?["id"] == null)
        {
            return defaultConfig;
        }

        long installationId = (long)installation["id"]!;

        var result = await service.GetConfigAsync(
            owner,
            repoName,
            installationId,
            fileName,
            defaultConfig,
            options,
            cancellationToken);

        return result.IsSuccess ? result.Value : defaultConfig;
    }

    /// <summary>
    /// Internal: Gets the configuration service from context metadata.
    /// Service is injected via ProbotSharpContext factory.
    /// </summary>
    private static dynamic? GetConfigService(ProbotSharpContext context)
    {
        return context.Metadata.TryGetValue(ConfigServiceKey, out var service)
            ? service
            : null;
    }

    /// <summary>
    /// Internal: Gets configuration options from context metadata.
    /// </summary>
    private static RepositoryConfigurationOptions? GetConfigOptions(ProbotSharpContext context)
    {
        return context.Metadata.TryGetValue(ConfigOptionsKey, out var options)
            ? options as RepositoryConfigurationOptions
            : null;
    }

    /// <summary>
    /// Internal: Sets the configuration service on the context.
    /// Called by IProbotSharpContextFactory during context creation.
    /// </summary>
    internal static void SetConfigurationService(
        this ProbotSharpContext context,
        object configService,
        RepositoryConfigurationOptions? options = null)
    {
        context.Metadata[ConfigServiceKey] = configService;
        if (options != null)
        {
            context.Metadata[ConfigOptionsKey] = options;
        }
    }
}
