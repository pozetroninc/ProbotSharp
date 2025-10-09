// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ProbotSharp.Infrastructure.Adapters.Persistence.Models;

namespace ProbotSharp.Infrastructure.Adapters.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the GitHub App manifest entity.
/// </summary>
internal sealed class GitHubAppManifestConfiguration : IEntityTypeConfiguration<GitHubAppManifestEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GitHubAppManifestEntity> builder)
    {
        builder.ToTable("github_app_manifests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.ManifestJson)
            .HasColumnName("manifest_json")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
    }
}
