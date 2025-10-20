// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

#pragma warning disable CA1054 // URI parameters should be strings for JSON serialization compatibility
#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility

/// <summary>
/// Represents a command to create a webhook channel for local development.
/// </summary>
/// <param name="SmeeClientUrl">The optional Smee.io client URL for webhook proxying.</param>
public sealed record class CreateWebhookChannelCommand(
    string? SmeeClientUrl = null);
#pragma warning restore CA1056
#pragma warning restore CA1054
