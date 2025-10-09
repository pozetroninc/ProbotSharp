// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Text.Json;

namespace ProbotSharp.Domain.ValueObjects;

public sealed record class WebhookPayload
{
    private WebhookPayload(string rawBody, JsonElement rootElement)
    {
        this.RawBody = rawBody;
        this.RootElement = rootElement;
    }

    public string RawBody { get; }

    public JsonElement RootElement { get; }

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

    public T GetProperty<T>(string name)
    {
        if (!this.RootElement.TryGetProperty(name, out var property))
        {
            throw new KeyNotFoundException($"Property '{name}' not found in payload.");
        }

        return property.Deserialize<T>() ?? throw new InvalidOperationException($"Property '{name}' could not be deserialized to {typeof(T).Name}.");
    }

    public override string ToString() => this.RawBody;
}

