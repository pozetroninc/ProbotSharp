// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Abstractions;

/// <summary>
/// Base class for aggregate root entities that can raise domain events.
/// </summary>
/// <typeparam name="TId">The type of the entity ID.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<object> _domainEvents = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot{TId}"/> class.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    /// <summary>
    /// Gets the collection of domain events raised by this aggregate.
    /// </summary>
    public IReadOnlyCollection<object> DomainEvents => this._domainEvents.AsReadOnly();

    /// <summary>
    /// Raises a domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to raise.</param>
    protected void RaiseDomainEvent(object domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        this._domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events raised by this aggregate.
    /// </summary>
    public void ClearDomainEvents() => this._domainEvents.Clear();
}
