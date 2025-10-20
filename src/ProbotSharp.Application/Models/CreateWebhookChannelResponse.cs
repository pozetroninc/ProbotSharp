// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

#pragma warning disable CA1054 // URI parameters should be strings for JSON serialization compatibility
#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility

/// <summary>
/// Represents the response after creating a webhook channel.
/// </summary>
/// <param name="WebhookProxyUrl">The webhook proxy URL to use for local development.</param>
/// <param name="CreatedAt">The timestamp when the channel was created.</param>
public sealed record class CreateWebhookChannelResponse(
    string WebhookProxyUrl,
    DateTimeOffset CreatedAt);
#pragma warning restore CA1056
#pragma warning restore CA1054
