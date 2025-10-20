// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Specifications;

/// <summary>
/// Represents the logical NOT (negation) of a specification.
/// </summary>
/// <typeparam name="T">The type of entity to evaluate.</typeparam>
internal sealed class NotSpecification<T> : Specification<T>
{
    private readonly Specification<T> _spec;

    public NotSpecification(Specification<T> spec)
    {
        this._spec = spec ?? throw new ArgumentNullException(nameof(spec));
    }

    public override bool IsSatisfiedBy(T candidate)
    {
        return !this._spec.IsSatisfiedBy(candidate);
    }
}
