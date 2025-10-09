// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

public sealed record class AuthenticateAsAppCommand(
    GitHubAppId AppId,
    PrivateKeyPem PrivateKey);
