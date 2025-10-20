// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.ValueObjects;

/// <summary>
/// Represents a webhook event name (e.g., 'issues', 'pull_request').
/// </summary>
public sealed record class WebhookEventName
{
    private WebhookEventName(string value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the event name value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new webhook event name.
    /// </summary>
    /// <param name="value">The event name.</param>
    /// <returns>A new webhook event name instance.</returns>
    public static WebhookEventName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Webhook event name cannot be null or whitespace.", nameof(value));
        }

        return new WebhookEventName(value.Trim());
    }

    /// <summary>
    /// Returns the event name as a string.
    /// </summary>
    /// <returns>The event name value.</returns>
    public override string ToString() => this.Value;
}
