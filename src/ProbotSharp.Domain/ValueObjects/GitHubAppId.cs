// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Globalization;

using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Domain.ValueObjects;

public sealed class GitHubAppId : ValueObject
{
    private GitHubAppId(long value)
    {
        this.Value = value;
    }

    public long Value { get; }

    public static GitHubAppId Create(long value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "GitHub App ID must be positive.");
        }

        return new GitHubAppId(value);
    }

    public override string ToString() => this.Value.ToString(CultureInfo.InvariantCulture);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return this.Value;
    }
}

