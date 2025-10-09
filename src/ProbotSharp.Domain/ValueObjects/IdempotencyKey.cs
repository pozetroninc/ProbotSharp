// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.ValueObjects;

/// <summary>
/// Value object representing an idempotency key used to prevent duplicate webhook processing.
/// Typically derived from GitHub webhook delivery IDs.
/// </summary>
public sealed record class IdempotencyKey
{
    private IdempotencyKey(string value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the raw idempotency key value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new idempotency key from a string value.
    /// </summary>
    /// <param name="value">The idempotency key value.</param>
    /// <returns>A new IdempotencyKey instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is null or whitespace.</exception>
    public static IdempotencyKey Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Idempotency key cannot be null or whitespace.", nameof(value));
        }

        return new IdempotencyKey(value.Trim());
    }

    /// <summary>
    /// Creates an idempotency key from a DeliveryId.
    /// </summary>
    /// <param name="deliveryId">The delivery ID.</param>
    /// <returns>A new IdempotencyKey instance.</returns>
    public static IdempotencyKey FromDeliveryId(DeliveryId deliveryId)
    {
        ArgumentNullException.ThrowIfNull(deliveryId);
        return new IdempotencyKey(deliveryId.Value);
    }

    /// <summary>
    /// Returns the string representation of the idempotency key.
    /// </summary>
    /// <returns>The idempotency key value.</returns>
    public override string ToString() => this.Value;
}
