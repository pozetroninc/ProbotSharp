// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ProbotSharp.Domain.ValueObjects;

/// <summary>
/// Represents a webhook signature used for validating GitHub webhook payloads.
/// </summary>
public sealed record class WebhookSignature
{
    private const string Prefix = "sha256=";

    private WebhookSignature(string value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the signature value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new webhook signature from a string value.
    /// </summary>
    /// <param name="value">The signature value (must start with 'sha256=').</param>
    /// <returns>A new webhook signature instance.</returns>
    public static WebhookSignature Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Webhook signature cannot be null or whitespace.", nameof(value));
        }

        if (!value.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Webhook signature must start with 'sha256='.", nameof(value));
        }

        var hash = value[Prefix.Length..];
        if (hash.Length != 64 || !hash.All(IsHexCharacter))
        {
            throw new ArgumentException("Webhook signature must be a 64-character hex string after the prefix.", nameof(value));
        }

        return new WebhookSignature(Prefix + hash.ToLowerInvariant());
    }

    /// <summary>
    /// Validates a webhook payload against a signature using the specified secret.
    /// </summary>
    /// <param name="payload">The webhook payload.</param>
    /// <param name="secret">The webhook secret.</param>
    /// <param name="signature">The signature to validate against.</param>
    /// <returns>True if the signature is valid; otherwise, false.</returns>
    public static bool TryValidatePayload(string payload, string secret, WebhookSignature signature)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(secret);
        ArgumentNullException.ThrowIfNull(signature);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = Prefix + Convert.ToHexString(hash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedSignature),
            Encoding.UTF8.GetBytes(signature.Value));
    }

    /// <summary>
    /// Returns the signature value as a string.
    /// </summary>
    /// <returns>The signature value.</returns>
    public override string ToString() => this.Value;

    private static bool IsHexCharacter(char c)
        => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
}
