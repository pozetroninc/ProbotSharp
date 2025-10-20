// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Events;

/// <summary>
/// Domain event raised when a webhook is delivered from GitHub.
/// </summary>
/// <param name="DeliveryId">The unique delivery ID from GitHub.</param>
/// <param name="EventName">The webhook event name.</param>
/// <param name="DeliveredAt">The timestamp when the webhook was delivered.</param>
public sealed record class WebhookDeliveredDomainEvent(DeliveryId DeliveryId, WebhookEventName EventName, DateTimeOffset DeliveredAt);
