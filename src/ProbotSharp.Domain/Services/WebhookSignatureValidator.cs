// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Services;

/// <summary>
/// Provides webhook signature validation using HMAC-SHA256.
/// </summary>
public sealed class WebhookSignatureValidator
{
    /// <summary>
    /// Validates that a webhook payload matches the provided signature.
    /// </summary>
    /// <param name="payload">The raw webhook payload string.</param>
    /// <param name="secret">The webhook secret used for signing.</param>
    /// <param name="signature">The signature from the X-Hub-Signature-256 header.</param>
    /// <returns>True if the signature is valid; otherwise, false.</returns>
    public bool IsSignatureValid(string payload, string secret, string signature)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        ArgumentException.ThrowIfNullOrWhiteSpace(signature);

        var webhookSignature = WebhookSignature.Create(signature);
        return WebhookSignature.TryValidatePayload(payload, secret, webhookSignature);
    }
}
