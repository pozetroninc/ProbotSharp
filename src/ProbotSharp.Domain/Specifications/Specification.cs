// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Specifications;

/// <summary>
/// Base class for the Specification pattern following Domain-Driven Design principles.
/// Encapsulates business rules and provides composable query logic.
/// </summary>
/// <typeparam name="T">The type of entity to evaluate</typeparam>
public abstract class Specification<T>
{
    /// <summary>
    /// Operator overload for logical AND.
    /// </summary>
    public static Specification<T> operator &(Specification<T> left, Specification<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.And(right);
    }

    /// <summary>
    /// Operator overload for logical OR.
    /// </summary>
    public static Specification<T> operator |(Specification<T> left, Specification<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.Or(right);
    }

    /// <summary>
    /// Operator overload for logical NOT.
    /// </summary>
    public static Specification<T> operator !(Specification<T> spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return spec.Not();
    }

    /// <summary>
    /// Named method alternative for the bitwise AND operator.
    /// Combines this specification with another using logical AND.
    /// </summary>
    /// <param name="left">The left specification</param>
    /// <param name="right">The right specification</param>
    /// <returns>A new specification representing the logical AND of both specifications</returns>
    public static Specification<T> BitwiseAnd(Specification<T> left, Specification<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.And(right);
    }

    /// <summary>
    /// Named method alternative for the bitwise OR operator.
    /// Combines this specification with another using logical OR.
    /// </summary>
    /// <param name="left">The left specification</param>
    /// <param name="right">The right specification</param>
    /// <returns>A new specification representing the logical OR of both specifications</returns>
    public static Specification<T> BitwiseOr(Specification<T> left, Specification<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.Or(right);
    }

    /// <summary>
    /// Named method alternative for the logical NOT operator.
    /// Negates the given specification.
    /// </summary>
    /// <param name="spec">The specification to negate</param>
    /// <returns>A new specification representing the logical NOT of the specification</returns>
    public static Specification<T> LogicalNot(Specification<T> spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return spec.Not();
    }

    /// <summary>
    /// Determines whether the specified candidate satisfies this specification.
    /// </summary>
    /// <param name="candidate">The entity to evaluate</param>
    /// <returns>True if the candidate satisfies the specification; otherwise, false</returns>
    public abstract bool IsSatisfiedBy(T candidate);

    /// <summary>
    /// Combines this specification with another using logical AND.
    /// </summary>
    /// <param name="other">The specification to combine with</param>
    /// <returns>A new specification representing the logical AND of both specifications</returns>
    public Specification<T> And(Specification<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new AndSpecification<T>(this, other);
    }

    /// <summary>
    /// Combines this specification with another using logical OR.
    /// </summary>
    /// <param name="other">The specification to combine with</param>
    /// <returns>A new specification representing the logical OR of both specifications</returns>
    public Specification<T> Or(Specification<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new OrSpecification<T>(this, other);
    }

    /// <summary>
    /// Negates this specification.
    /// </summary>
    /// <returns>A new specification representing the logical NOT of this specification</returns>
    public Specification<T> Not()
    {
        return new NotSpecification<T>(this);
    }
}
