// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

/// <summary>
/// Response containing JWT authentication information for a GitHub App.
/// </summary>
/// <param name="JsonWebToken">The JWT token string.</param>
/// <param name="IssuedAt">The timestamp when the token was issued.</param>
/// <param name="ExpiresAt">The timestamp when the token expires.</param>
public sealed record class AuthenticateAsAppResponse(
    string JsonWebToken,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);
