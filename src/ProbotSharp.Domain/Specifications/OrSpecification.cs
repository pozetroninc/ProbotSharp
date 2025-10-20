// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Specifications;

/// <summary>
/// Represents the logical OR combination of two specifications.
/// </summary>
/// <typeparam name="T">The type of entity to evaluate.</typeparam>
internal sealed class OrSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public OrSpecification(Specification<T> left, Specification<T> right)
    {
        this._left = left ?? throw new ArgumentNullException(nameof(left));
        this._right = right ?? throw new ArgumentNullException(nameof(right));
    }

    public override bool IsSatisfiedBy(T candidate)
    {
        return this._left.IsSatisfiedBy(candidate) || this._right.IsSatisfiedBy(candidate);
    }
}
