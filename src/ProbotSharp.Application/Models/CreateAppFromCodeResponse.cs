// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

public sealed record class CreateAppFromCodeResponse(
    GitHubAppId AppId,
    string ClientId,
    string ClientSecret,
    string WebhookSecret,
    PrivateKeyPem PrivateKey,
    string HtmlUrl);
