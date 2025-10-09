// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ProbotSharp.Infrastructure.Adapters.Persistence.Models;

namespace ProbotSharp.Infrastructure.Adapters.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the issue metadata entity.
/// </summary>
internal sealed class IssueMetadataConfiguration : IEntityTypeConfiguration<IssueMetadataEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IssueMetadataEntity> builder)
    {
        builder.ToTable("issue_metadata");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.RepositoryOwner)
            .HasColumnName("repository_owner")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.RepositoryName)
            .HasColumnName("repository_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.IssueNumber)
            .HasColumnName("issue_number")
            .IsRequired();

        builder.Property(x => x.Key)
            .HasColumnName("key")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Value)
            .HasColumnName("value")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Composite unique index on (repository_owner, repository_name, issue_number, key)
        builder.HasIndex(x => new { x.RepositoryOwner, x.RepositoryName, x.IssueNumber, x.Key })
            .IsUnique()
            .HasDatabaseName("ix_issue_metadata_composite_key");
    }
}
