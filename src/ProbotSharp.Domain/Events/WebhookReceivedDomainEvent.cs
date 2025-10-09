// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Events;

public sealed record class WebhookReceivedDomainEvent(DeliveryId DeliveryId, WebhookEventName EventName, DateTimeOffset ReceivedAt, InstallationId? InstallationId);
