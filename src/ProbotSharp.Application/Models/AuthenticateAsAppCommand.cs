// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Models;

/// <summary>
/// Command to authenticate as a GitHub App using JWT.
/// </summary>
/// <param name="AppId">The GitHub App ID.</param>
/// <param name="PrivateKey">The private key for signing JWTs.</param>
public sealed record class AuthenticateAsAppCommand(
    GitHubAppId AppId,
    PrivateKeyPem PrivateKey);
