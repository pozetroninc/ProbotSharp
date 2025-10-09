// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ProbotSharp.Infrastructure.Adapters.Persistence.Models;

namespace ProbotSharp.Infrastructure.Adapters.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the dead-letter queue item entity.
/// </summary>
internal sealed class DeadLetterQueueItemConfiguration : IEntityTypeConfiguration<DeadLetterQueueItemEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DeadLetterQueueItemEntity> builder)
    {
        builder.ToTable("dead_letter_queue_items");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.DeliveryId)
            .HasColumnName("delivery_id")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.EventName)
            .HasColumnName("event_name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .IsRequired();

        builder.Property(x => x.InstallationId)
            .HasColumnName("installation_id");

        builder.Property(x => x.Signature)
            .HasColumnName("signature")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.RawPayload)
            .HasColumnName("raw_payload")
            .IsRequired();

        builder.Property(x => x.Attempt)
            .HasColumnName("attempt")
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.FailedAt)
            .HasColumnName("failed_at")
            .IsRequired();

        builder.Property(x => x.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(2048);

        // Index for querying by delivery ID
        builder.HasIndex(x => x.DeliveryId)
            .HasDatabaseName("ix_dead_letter_queue_items_delivery_id");

        // Index for querying by failed_at for cleanup operations
        builder.HasIndex(x => x.FailedAt)
            .HasDatabaseName("ix_dead_letter_queue_items_failed_at");
    }
}
