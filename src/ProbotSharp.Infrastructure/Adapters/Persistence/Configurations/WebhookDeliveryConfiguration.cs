// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ProbotSharp.Infrastructure.Adapters.Persistence.Models;

namespace ProbotSharp.Infrastructure.Adapters.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the webhook delivery entity.
/// </summary>
internal sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDeliveryEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<WebhookDeliveryEntity> builder)
    {
        builder.ToTable("webhook_deliveries");
        builder.HasKey(x => x.DeliveryId);

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

        builder.Property(x => x.DeliveredAt)
            .HasColumnName("delivered_at")
            .IsRequired();

        builder.Property(x => x.InstallationId)
            .HasColumnName("installation_id");

        builder.Property(x => x.PayloadHash)
            .HasColumnName("payload_hash")
            .HasMaxLength(64);
    }
}
