// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Globalization;

namespace ProbotSharp.Domain.ValueObjects;

public sealed record class InstallationId
{
    private InstallationId(long value)
    {
        this.Value = value;
    }

    public long Value { get; }

    public static InstallationId Create(long value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Installation ID must be positive.");
        }

        return new InstallationId(value);
    }

    public override string ToString() => this.Value.ToString(CultureInfo.InvariantCulture);
}

