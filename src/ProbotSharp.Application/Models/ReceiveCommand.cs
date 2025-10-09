// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

public sealed record class ReceiveCommand(
    WebhookEventName EventName,
    string PayloadPath)
{
    public string PayloadPath { get; } = !string.IsNullOrWhiteSpace(PayloadPath)
        ? PayloadPath
        : throw new ArgumentException("Payload path cannot be null or whitespace.", nameof(PayloadPath));
}
