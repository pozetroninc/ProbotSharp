// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Globalization;

namespace ProbotSharp.Domain.ValueObjects;

/// <summary>
/// Represents a GitHub App installation ID.
/// </summary>
public sealed record class InstallationId
{
    private InstallationId(long value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the installation ID value.
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Creates a new installation ID.
    /// </summary>
    /// <param name="value">The ID value (must be positive).</param>
    /// <returns>A new installation ID instance.</returns>
    public static InstallationId Create(long value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Installation ID must be positive.");
        }

        return new InstallationId(value);
    }

    /// <summary>
    /// Returns the installation ID as a string.
    /// </summary>
    /// <returns>The ID value as a string.</returns>
    public override string ToString() => this.Value.ToString(CultureInfo.InvariantCulture);
}
