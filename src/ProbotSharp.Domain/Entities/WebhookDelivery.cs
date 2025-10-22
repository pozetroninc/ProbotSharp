// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Abstractions;
using ProbotSharp.Domain.Events;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Entities;

/// <summary>
/// Represents a webhook delivery from GitHub.
/// </summary>
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

    /// <summary>
    /// Gets the webhook event name.
    /// </summary>
    public WebhookEventName EventName { get; }

    /// <summary>
    /// Gets the delivery timestamp.
    /// </summary>
    public DateTimeOffset DeliveredAt { get; }

    /// <summary>
    /// Gets the webhook payload.
    /// </summary>
    public WebhookPayload Payload { get; }

    /// <summary>
    /// Gets the installation ID if applicable.
    /// </summary>
    public InstallationId? InstallationId { get; }

    /// <summary>
    /// Creates a new webhook delivery instance.
    /// </summary>
    /// <param name="id">The delivery ID.</param>
    /// <param name="eventName">The event name.</param>
    /// <param name="deliveredAt">The delivery timestamp.</param>
    /// <param name="payload">The webhook payload.</param>
    /// <param name="installationId">The installation ID if applicable.</param>
    /// <returns>A result containing the new webhook delivery instance or an error.</returns>
    public static Result<WebhookDelivery> Create(
        DeliveryId id,
        WebhookEventName eventName,
        DateTimeOffset deliveredAt,
        WebhookPayload payload,
        InstallationId? installationId)
    {
        if (deliveredAt == default)
        {
            return Result<WebhookDelivery>.Failure(
                "webhook_delivery.invalid_delivered_at",
                "DeliveredAt must be set.");
        }

        var delivery = new WebhookDelivery(id, eventName, deliveredAt, payload, installationId);
        delivery.RaiseDomainEvent(new WebhookDeliveredDomainEvent(id, eventName, deliveredAt));
        return Result<WebhookDelivery>.Success(delivery);
    }
}
