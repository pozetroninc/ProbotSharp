// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Globalization;

using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Domain.ValueObjects;

/// <summary>
/// Represents a GitHub App ID.
/// </summary>
public sealed class GitHubAppId : ValueObject
{
    private GitHubAppId(long value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the GitHub App ID value.
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Creates a new GitHub App ID.
    /// </summary>
    /// <param name="value">The ID value (must be positive).</param>
    /// <returns>A new GitHub App ID instance.</returns>
    public static GitHubAppId Create(long value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "GitHub App ID must be positive.");
        }

        return new GitHubAppId(value);
    }

    /// <summary>
    /// Returns the GitHub App ID as a string.
    /// </summary>
    /// <returns>The ID value as a string.</returns>
    public override string ToString() => this.Value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the components used for equality comparison.
    /// </summary>
    /// <returns>The equality components.</returns>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return this.Value;
    }
}
