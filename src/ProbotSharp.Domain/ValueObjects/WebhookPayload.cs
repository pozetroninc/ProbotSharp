// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Text.Json;

namespace ProbotSharp.Domain.ValueObjects;

/// <summary>
/// Represents a webhook payload received from GitHub.
/// </summary>
public sealed record class WebhookPayload
{
    private WebhookPayload(string rawBody, JsonElement rootElement)
    {
        this.RawBody = rawBody;
        this.RootElement = rootElement;
    }

    /// <summary>
    /// Gets the raw JSON body of the webhook.
    /// </summary>
    public string RawBody { get; }

    /// <summary>
    /// Gets the root JSON element of the payload.
    /// </summary>
    public JsonElement RootElement { get; }

    /// <summary>
    /// Creates a new webhook payload from a JSON string.
    /// </summary>
    /// <param name="body">The JSON body of the webhook.</param>
    /// <returns>A new webhook payload instance.</returns>
    public static WebhookPayload Create(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Payload body cannot be null or whitespace.", nameof(body));
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement.Clone();
            return new WebhookPayload(body, root);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Payload must be valid JSON.", nameof(body), ex);
        }
    }

    /// <summary>
    /// Gets a property from the webhook payload and deserializes it to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="name">The name of the property.</param>
    /// <returns>The deserialized property value.</returns>
    public T GetProperty<T>(string name)
    {
        if (!this.RootElement.TryGetProperty(name, out var property))
        {
            throw new KeyNotFoundException($"Property '{name}' not found in payload.");
        }

        return property.Deserialize<T>() ?? throw new InvalidOperationException($"Property '{name}' could not be deserialized to {typeof(T).Name}.");
    }

    /// <summary>
    /// Returns the raw JSON body of the webhook.
    /// </summary>
    /// <returns>The raw JSON string.</returns>
    public override string ToString() => this.RawBody;
}
