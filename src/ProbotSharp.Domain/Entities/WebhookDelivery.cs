// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Abstractions;
using ProbotSharp.Domain.Events;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Entities;

public sealed class WebhookDelivery : AggregateRoot<DeliveryId>
{
    private WebhookDelivery(
        DeliveryId id,
        WebhookEventName eventName,
        DateTimeOffset deliveredAt,
        WebhookPayload payload,
        InstallationId? installationId)
        : base(id)
    {
        this.EventName = eventName;
        this.DeliveredAt = deliveredAt;
        this.Payload = payload;
        this.InstallationId = installationId;
    }

    public WebhookEventName EventName { get; }

    public DateTimeOffset DeliveredAt { get; }

    public WebhookPayload Payload { get; }

    public InstallationId? InstallationId { get; }

    public static WebhookDelivery Create(
        DeliveryId id,
        WebhookEventName eventName,
        DateTimeOffset deliveredAt,
        WebhookPayload payload,
        InstallationId? installationId)
    {
        if (deliveredAt == default)
        {
            throw new ArgumentException("DeliveredAt must be set.", nameof(deliveredAt));
        }

        var delivery = new WebhookDelivery(id, eventName, deliveredAt, payload, installationId);
        delivery.RaiseDomainEvent(new WebhookDeliveredDomainEvent(id, eventName, deliveredAt));
        return delivery;
    }
}

