// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Services;

public sealed class WebhookSignatureValidator
{
    public bool IsSignatureValid(string payload, string secret, string signature)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        ArgumentException.ThrowIfNullOrWhiteSpace(signature);

        var webhookSignature = WebhookSignature.Create(signature);
        return WebhookSignature.TryValidatePayload(payload, secret, webhookSignature);
    }
}

