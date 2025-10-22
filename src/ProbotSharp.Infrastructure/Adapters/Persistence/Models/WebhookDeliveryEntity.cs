// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Domain.Abstractions;

using DeliveryIdValue = ProbotSharp.Domain.ValueObjects.DeliveryId;
using EventNameValue = ProbotSharp.Domain.ValueObjects.WebhookEventName;
using InstallationIdValue = ProbotSharp.Domain.ValueObjects.InstallationId;

namespace ProbotSharp.Infrastructure.Adapters.Persistence.Models;

/// <summary>
/// Entity used by the infrastructure layer to persist webhook delivery records.
/// </summary>
public sealed class WebhookDeliveryEntity
{
    /// <summary>Gets or sets the unique delivery identifier.</summary>
    public string DeliveryId { get; set; } = string.Empty;

    /// <summary>Gets or sets the GitHub event name associated with the delivery.</summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the webhook was delivered.</summary>
    public DateTimeOffset DeliveredAt { get; set; }

    /// <summary>Gets or sets the raw payload body.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional installation identifier.</summary>
    public long? InstallationId { get; set; }

    /// <summary>Gets or sets an optional hash of the payload used for deduplication.</summary>
    public string? PayloadHash { get; set; }

    internal static WebhookDeliveryEntity FromDomain(WebhookDelivery delivery)
        => new()
        {
            DeliveryId = delivery.Id.Value,
            EventName = delivery.EventName.Value,
            DeliveredAt = delivery.DeliveredAt,
            Payload = delivery.Payload.RawBody,
            InstallationId = delivery.InstallationId?.Value,
        };

    internal Result<WebhookDelivery> ToDomain()
    {
        var payload = WebhookPayload.Create(this.Payload);
        var deliveryResult = WebhookDelivery.Create(
            DeliveryIdValue.Create(this.DeliveryId),
            EventNameValue.Create(this.EventName),
            this.DeliveredAt,
            payload,
            this.InstallationId.HasValue ? InstallationIdValue.Create(this.InstallationId.Value) : null);

        if (!deliveryResult.IsSuccess)
        {
            return deliveryResult;
        }

        var delivery = deliveryResult.Value!;
        delivery.ClearDomainEvents();
        return Result<WebhookDelivery>.Success(delivery);
    }
}
