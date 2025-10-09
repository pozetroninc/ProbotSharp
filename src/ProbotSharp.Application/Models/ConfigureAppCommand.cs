// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Command to configure application settings during setup wizard process.
/// This allows updating configuration parameters before finalizing the setup.
/// </summary>
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
