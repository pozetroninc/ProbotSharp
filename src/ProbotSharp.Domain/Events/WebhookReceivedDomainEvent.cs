// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Events;

/// <summary>
/// Domain event raised when a webhook is received by the application.
/// </summary>
/// <param name="DeliveryId">The unique delivery ID from GitHub.</param>
/// <param name="EventName">The webhook event name.</param>
/// <param name="ReceivedAt">The timestamp when the webhook was received.</param>
/// <param name="InstallationId">The optional installation ID associated with the webhook.</param>
public sealed record class WebhookReceivedDomainEvent(DeliveryId DeliveryId, WebhookEventName EventName, DateTimeOffset ReceivedAt, InstallationId? InstallationId);
