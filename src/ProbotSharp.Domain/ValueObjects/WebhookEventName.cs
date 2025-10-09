// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.ValueObjects;

public sealed record class WebhookEventName
{
    private WebhookEventName(string value)
    {
        this.Value = value;
    }

    public string Value { get; }

    public static WebhookEventName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Webhook event name cannot be null or whitespace.", nameof(value));
        }

        return new WebhookEventName(value.Trim());
    }

    public override string ToString() => this.Value;
}

