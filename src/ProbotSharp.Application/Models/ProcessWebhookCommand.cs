// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

public sealed record class ProcessWebhookCommand(
    DeliveryId DeliveryId,
    WebhookEventName EventName,
    WebhookPayload Payload,
    InstallationId? InstallationId,
    WebhookSignature Signature,
    string RawPayload);

