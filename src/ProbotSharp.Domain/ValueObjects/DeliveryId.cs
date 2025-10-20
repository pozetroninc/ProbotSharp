// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.ValueObjects;

/// <summary>
/// Represents a webhook delivery ID from GitHub.
/// </summary>
public sealed record class DeliveryId
{
    private DeliveryId(string value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the delivery ID value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new delivery ID.
    /// </summary>
    /// <param name="value">The delivery ID value.</param>
    /// <returns>A new delivery ID instance.</returns>
    public static DeliveryId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Delivery ID cannot be null or whitespace.", nameof(value));
        }

        return new DeliveryId(value.Trim());
    }

    /// <summary>
    /// Returns the delivery ID as a string.
    /// </summary>
    /// <returns>The delivery ID value.</returns>
    public override string ToString() => this.Value;
}
