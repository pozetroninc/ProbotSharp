// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

public sealed record class AuthenticateAsAppResponse(
    string JsonWebToken,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);
