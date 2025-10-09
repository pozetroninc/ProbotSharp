// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.ValueObjects;

public sealed record class DeliveryId
{
    private DeliveryId(string value)
    {
        this.Value = value;
    }

    public string Value { get; }

    public static DeliveryId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Delivery ID cannot be null or whitespace.", nameof(value));
        }

        return new DeliveryId(value.Trim());
    }

    public override string ToString() => this.Value;
}

