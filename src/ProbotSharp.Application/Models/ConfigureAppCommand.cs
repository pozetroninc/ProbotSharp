// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Command to configure application settings during setup wizard process.
/// This allows updating configuration parameters before finalizing the setup.
/// </summary>
#pragma warning disable CA1054 // URI parameters should be strings for JSON serialization compatibility
#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility
public sealed record class ConfigureAppCommand(
    string? Host = null,
    int? Port = null,
    string? WebhookPath = null,
    string? WebhookProxyUrl = null,
    string? BaseUrl = null,
    string? RedisUrl = null,
    string? LogLevel = null,
    string? LogFormat = null,
    Dictionary<string, string>? CustomSettings = null);
#pragma warning restore CA1056
#pragma warning restore CA1054
