// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

/// <summary>
/// Represents a command to process a webhook delivery.
/// </summary>
/// <param name="DeliveryId">The unique delivery ID from GitHub.</param>
/// <param name="EventName">The webhook event name.</param>
/// <param name="Payload">The webhook payload.</param>
/// <param name="InstallationId">The optional installation ID.</param>
/// <param name="Signature">The webhook signature for validation.</param>
/// <param name="RawPayload">The raw payload string for signature validation.</param>
public sealed record class ProcessWebhookCommand(
    DeliveryId DeliveryId,
    WebhookEventName EventName,
    WebhookPayload Payload,
    InstallationId? InstallationId,
    WebhookSignature Signature,
    string RawPayload);
