// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

/// <summary>
/// Represents a command to receive and process a webhook from a file.
/// </summary>
/// <param name="EventName">The webhook event name.</param>
/// <param name="PayloadPath">The path to the payload file.</param>
public sealed record class ReceiveCommand(
    WebhookEventName EventName,
    string PayloadPath)
{
    /// <summary>
    /// Gets the path to the payload file.
    /// </summary>
    public string PayloadPath { get; } = !string.IsNullOrWhiteSpace(PayloadPath)
        ? PayloadPath
        : throw new ArgumentException("Payload path cannot be null or whitespace.", nameof(PayloadPath));
}
