// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Events;

/// <summary>
/// Domain event raised when a webhook has been successfully handled.
/// </summary>
/// <param name="DeliveryId">The unique delivery ID from GitHub.</param>
/// <param name="EventName">The webhook event name.</param>
/// <param name="HandledAt">The timestamp when the webhook was handled.</param>
public sealed record class WebhookHandledDomainEvent(DeliveryId DeliveryId, WebhookEventName EventName, DateTimeOffset HandledAt);
