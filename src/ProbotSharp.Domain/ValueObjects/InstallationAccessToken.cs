// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.ValueObjects;

public sealed record class InstallationAccessToken
{
    private InstallationAccessToken(string value, DateTimeOffset expiresAt)
    {
        this.Value = value;
        this.ExpiresAt = expiresAt;
    }

    public string Value { get; }

    public DateTimeOffset ExpiresAt { get; }

    public bool IsExpired(DateTimeOffset now) => now >= this.ExpiresAt;

    public static InstallationAccessToken Create(string value, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Installation access token cannot be null or whitespace.", nameof(value));
        }

        if (expiresAt <= DateTimeOffset.UtcNow.AddMinutes(-5))
        {
            throw new ArgumentException("Installation access token expiration must be in the future.", nameof(expiresAt));
        }

        return new InstallationAccessToken(value.Trim(), expiresAt);
    }

    public override string ToString() => this.Value;
}

