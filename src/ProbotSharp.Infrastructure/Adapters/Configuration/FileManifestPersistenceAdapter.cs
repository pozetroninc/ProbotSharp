// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Globalization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Infrastructure.Adapters.Configuration;

/// <summary>
/// Persists the GitHub App manifest to the local file system for reuse between runs.
/// </summary>
public sealed class FileManifestPersistenceAdapter : IManifestPersistencePort
{
    private readonly string _manifestPath;
    private readonly ILogger<FileManifestPersistenceAdapter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileManifestPersistenceAdapter"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration for determining manifest file path.</param>
    /// <param name="hostEnvironment">The host environment for resolving relative paths.</param>
    /// <param name="logger">The logger instance.</param>
    public FileManifestPersistenceAdapter(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<FileManifestPersistenceAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        ArgumentNullException.ThrowIfNull(logger);

        this._logger = logger;

        var configuredPath = configuration["ProbotSharp:ManifestPath"]
            ?? configuration["Manifest:Path"];

        this._manifestPath = !string.IsNullOrWhiteSpace(configuredPath)
            ? Path.GetFullPath(configuredPath, hostEnvironment.ContentRootPath)
            : Path.Combine(hostEnvironment.ContentRootPath, "storage", "manifest.json");
    }

    /// <inheritdoc />
    public async Task<Result<string?>> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(this._manifestPath))
            {
                return Result<string?>.Success(null);
            }

            var stream = new FileStream(this._manifestPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                return Result<string?>.Success(content);
            }
            finally
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (IOException ex)
        {
            LogMessages.ManifestReadFailed(this._logger, this._manifestPath, ex);
            return Result<string?>.Failure("manifest_read_failed", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogMessages.ManifestReadFailed(this._logger, this._manifestPath, ex);
            return Result<string?>.Failure("manifest_read_failed", ex.Message);
        }
        catch (Exception ex)
        {
            LogMessages.ManifestReadFailed(this._logger, this._manifestPath, ex);
            return Result<string?>.Failure("manifest_read_failed", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> SaveAsync(string manifestJson, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(manifestJson);

        try
        {
            var directory = Path.GetDirectoryName(this._manifestPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(this._manifestPath, manifestJson, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (IOException ex)
        {
            LogMessages.ManifestWriteFailed(this._logger, this._manifestPath, ex);
            return Result.Failure("manifest_save_failed", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogMessages.ManifestWriteFailed(this._logger, this._manifestPath, ex);
            return Result.Failure("manifest_save_failed", ex.Message);
        }
        catch (Exception ex)
        {
            LogMessages.ManifestWriteFailed(this._logger, this._manifestPath, ex);
            return Result.Failure("manifest_save_failed", ex.Message);
        }
    }
}
