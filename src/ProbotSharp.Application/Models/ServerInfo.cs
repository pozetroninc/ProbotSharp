// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

public sealed record class ServerInfo(
    string Host,
    int Port,
    string WebhookPath,
    bool IsRunning,
    string? WebhookProxyUrl);
