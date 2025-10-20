// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Abstractions;

/// <summary>
/// Base class for domain entities with identity.
/// </summary>
/// <typeparam name="TId">The type of the entity ID.</typeparam>
public abstract class Entity<TId>
    where TId : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Entity{TId}"/> class.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    protected Entity(TId id)
    {
        this.Id = id;
    }

    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public TId Id { get; }

    /// <summary>
    /// Determines whether the specified object is equal to the current entity.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if the objects are equal; otherwise, false.</returns>
    public override bool Equals(object? obj)
        => obj is Entity<TId> other && EqualityComparer<TId>.Default.Equals(this.Id, other.Id);

    /// <summary>
    /// Gets the hash code for the entity.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => this.Id.GetHashCode();
}
