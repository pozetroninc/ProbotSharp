// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ProbotSharp.Domain.ValueObjects;

public sealed record class WebhookSignature
{
    private const string Prefix = "sha256=";

    private WebhookSignature(string value)
    {
        this.Value = value;
    }

    public string Value { get; }

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

    public override string ToString() => this.Value;

    private static bool IsHexCharacter(char c)
        => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
}

