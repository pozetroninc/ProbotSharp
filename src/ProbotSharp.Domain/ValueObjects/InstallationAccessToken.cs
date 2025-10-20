// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.ValueObjects;

/// <summary>
/// Represents an installation access token used for authenticating GitHub API requests.
/// </summary>
public sealed record class InstallationAccessToken
{
    private InstallationAccessToken(string value, DateTimeOffset expiresAt)
    {
        this.Value = value;
        this.ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Gets the token value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the expiration time of the token.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; }

    /// <summary>
    /// Determines whether the token is expired at the specified time.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <returns>True if the token is expired; otherwise, false.</returns>
    public bool IsExpired(DateTimeOffset now) => now >= this.ExpiresAt;

    /// <summary>
    /// Creates a new installation access token.
    /// </summary>
    /// <param name="value">The token value.</param>
    /// <param name="expiresAt">The expiration time.</param>
    /// <returns>A new installation access token instance.</returns>
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

    /// <summary>
    /// Returns the token value as a string.
    /// </summary>
    /// <returns>The token value.</returns>
    public override string ToString() => this.Value;
}
