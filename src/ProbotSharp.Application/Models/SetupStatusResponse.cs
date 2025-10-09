// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

public sealed record class SetupStatusResponse(
    bool IsConfigured,
    bool HasAppId,
    bool HasPrivateKey,
    bool HasWebhookSecret,
    Uri? SetupUrl = null);
