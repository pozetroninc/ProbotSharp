// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.EntityFrameworkCore;

using ProbotSharp.Infrastructure.Adapters.Persistence.Configurations;
using ProbotSharp.Infrastructure.Adapters.Persistence.Models;

namespace ProbotSharp.Infrastructure.Adapters.Persistence;

/// <summary>
/// Entity Framework database context for ProbotSharp persistence operations.
/// </summary>
public sealed class ProbotSharpDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProbotSharpDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public ProbotSharpDbContext(DbContextOptions<ProbotSharpDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the database set for webhook delivery entities.
    /// </summary>
    public DbSet<WebhookDeliveryEntity> WebhookDeliveries => Set<WebhookDeliveryEntity>();

    /// <summary>
    /// Gets the database set for GitHub App manifest entities.
    /// </summary>
    public DbSet<GitHubAppManifestEntity> GitHubAppManifests => Set<GitHubAppManifestEntity>();

    /// <summary>
    /// Gets the database set for idempotency record entities.
    /// </summary>
    public DbSet<IdempotencyRecordEntity> IdempotencyRecords => Set<IdempotencyRecordEntity>();

    /// <summary>
    /// Gets the database set for issue metadata entities.
    /// </summary>
    public DbSet<IssueMetadataEntity> IssueMetadata => Set<IssueMetadataEntity>();

    /// <summary>
    /// Gets the database set for dead-letter queue item entities.
    /// </summary>
    public DbSet<DeadLetterQueueItemEntity> DeadLetterQueueItems => Set<DeadLetterQueueItemEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("probot");
        modelBuilder.ApplyConfiguration(new WebhookDeliveryConfiguration());
        modelBuilder.ApplyConfiguration(new GitHubAppManifestConfiguration());
        modelBuilder.ApplyConfiguration(new IdempotencyRecordConfiguration());
        modelBuilder.ApplyConfiguration(new IssueMetadataConfiguration());
        modelBuilder.ApplyConfiguration(new DeadLetterQueueItemConfiguration());
    }
}
