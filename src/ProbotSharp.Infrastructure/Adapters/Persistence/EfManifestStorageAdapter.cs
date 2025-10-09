// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Infrastructure.Adapters.Persistence.Models;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Infrastructure.Adapters.Persistence;

/// <summary>
/// Entity Framework-based adapter for persisting GitHub App manifest data to a database.
/// </summary>
public sealed class EfManifestStorageAdapter : IManifestPersistencePort
{
    private readonly ProbotSharpDbContext _dbContext;
    private readonly ILogger<EfManifestStorageAdapter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfManifestStorageAdapter"/> class.
    /// </summary>
    /// <param name="dbContext">The database context for manifest operations.</param>
    /// <param name="logger">The logger instance.</param>
    public EfManifestStorageAdapter(ProbotSharpDbContext dbContext, ILogger<EfManifestStorageAdapter> logger)
    {
        this._dbContext = dbContext;
        this._logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> SaveAsync(string manifestJson, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(manifestJson);

        try
        {
            // Check if a manifest already exists
            var existingManifest = await this._dbContext.GitHubAppManifests
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (existingManifest is not null)
            {
                // Update existing manifest
                existingManifest.ManifestJson = manifestJson;
                existingManifest.CreatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                // Create new manifest
                var entity = new GitHubAppManifestEntity
                {
                    ManifestJson = manifestJson,
                    CreatedAt = DateTimeOffset.UtcNow,
                };

                await this._dbContext.GitHubAppManifests.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            }

            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            // CA1031: Catching general exception is intentional here to convert infrastructure errors to Result type
            this._logger.LogError(ex, "Failed to save GitHub App manifest");
            return Result.Failure("manifest_storage_save_failed", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<string?>> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await this._dbContext.GitHubAppManifests
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return Result<string?>.Success(null);
            }

            return Result<string?>.Success(entity.ManifestJson);
        }
        catch (Exception ex)
        {
            // CA1031: Catching general exception is intentional here to convert infrastructure errors to Result type
            this._logger.LogError(ex, "Failed to retrieve GitHub App manifest");
            return Result<string?>.Failure("manifest_storage_get_failed", ex.Message);
        }
    }
}
