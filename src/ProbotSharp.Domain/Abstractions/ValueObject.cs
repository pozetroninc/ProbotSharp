// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Abstractions;

/// <summary>
/// Base class for value objects in the domain layer.
/// Value objects are defined by their values rather than their identity.
/// Two value objects with the same values are considered equal.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Determines whether two value objects are equal.
    /// </summary>
    /// <param name="left">The first value object to compare.</param>
    /// <param name="right">The second value object to compare.</param>
    /// <returns>true if the value objects are equal; otherwise, false.</returns>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two value objects are not equal.
    /// </summary>
    /// <param name="left">The first value object to compare.</param>
    /// <param name="right">The second value object to compare.</param>
    /// <returns>true if the value objects are not equal; otherwise, false.</returns>
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current value object.
    /// Equality is based on the equality components, not object identity.
    /// </summary>
    /// <param name="obj">The object to compare with the current value object.</param>
    /// <returns>true if the specified object is equal to the current value object; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        var other = (ValueObject)obj;
        return this.GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Determines whether the specified value object is equal to the current value object.
    /// </summary>
    /// <param name="other">The value object to compare with the current value object.</param>
    /// <returns>true if the specified value object is equal to the current value object; otherwise, false.</returns>
    public bool Equals(ValueObject? other)
    {
        return this.Equals((object?)other);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// The hash code is computed based on the equality components.
    /// </summary>
    /// <returns>A hash code for the current value object.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            return this.GetEqualityComponents()
                .Aggregate(17, (current, obj) =>
                {
                    var hash = obj?.GetHashCode() ?? 0;
                    return current * 31 + hash;
                });
        }
    }

    /// <summary>
    /// Gets the components that define equality for this value object.
    /// Derived classes must implement this to return all properties/fields
    /// that contribute to value equality.
    /// </summary>
    /// <returns>An enumerable of objects that define this value object's equality.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();
}
