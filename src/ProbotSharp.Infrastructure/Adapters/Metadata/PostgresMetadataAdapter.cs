// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.EntityFrameworkCore;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Infrastructure.Adapters.Persistence;
using ProbotSharp.Infrastructure.Adapters.Persistence.Models;

namespace ProbotSharp.Infrastructure.Adapters.Metadata;

/// <summary>
/// PostgreSQL implementation of the metadata port using Entity Framework Core.
/// </summary>
public sealed class PostgresMetadataAdapter : IMetadataPort
{
    private readonly ProbotSharpDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresMetadataAdapter"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public PostgresMetadataAdapter(ProbotSharpDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public async Task<string?> GetAsync(string owner, string repo, int issueNumber, string key, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return await _dbContext.IssueMetadata
            .Where(m => m.RepositoryOwner == owner
                && m.RepositoryName == repo
                && m.IssueNumber == issueNumber
                && m.Key == key)
            .Select(m => m.Value)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task SetAsync(string owner, string repo, int issueNumber, string key, string value, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        var metadata = await _dbContext.IssueMetadata
            .FirstOrDefaultAsync(m => m.RepositoryOwner == owner
                && m.RepositoryName == repo
                && m.IssueNumber == issueNumber
                && m.Key == key, ct);

        if (metadata == null)
        {
            // Create new metadata entry
            metadata = new IssueMetadataEntity
            {
                RepositoryOwner = owner,
                RepositoryName = repo,
                IssueNumber = issueNumber,
                Key = key,
                Value = value,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _dbContext.IssueMetadata.Add(metadata);
        }
        else
        {
            // Update existing metadata entry
            metadata.Value = value;
            metadata.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string owner, string repo, int issueNumber, string key, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return await _dbContext.IssueMetadata
            .AnyAsync(m => m.RepositoryOwner == owner
                && m.RepositoryName == repo
                && m.IssueNumber == issueNumber
                && m.Key == key, ct);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string owner, string repo, int issueNumber, string key, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var metadata = await _dbContext.IssueMetadata
            .FirstOrDefaultAsync(m => m.RepositoryOwner == owner
                && m.RepositoryName == repo
                && m.IssueNumber == issueNumber
                && m.Key == key, ct);

        if (metadata != null)
        {
            _dbContext.IssueMetadata.Remove(metadata);
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, string>> GetAllAsync(string owner, string repo, int issueNumber, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var metadataList = await _dbContext.IssueMetadata
            .Where(m => m.RepositoryOwner == owner
                && m.RepositoryName == repo
                && m.IssueNumber == issueNumber)
            .ToDictionaryAsync(m => m.Key, m => m.Value, ct);

        return metadataList;
    }
}
