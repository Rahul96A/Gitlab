using System.ComponentModel.DataAnnotations.Schema;

namespace GitLabClone.Domain.Common;

/// <summary>
/// Base class for all domain entities. Provides identity and domain event support.
/// Uses Guid as the default key type — avoids sequential-id enumeration attacks
/// and simplifies distributed ID generation (no DB round-trip needed).
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    private readonly List<DomainEvent> _domainEvents = [];

    [NotMapped]
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void RemoveDomainEvent(DomainEvent domainEvent) => _domainEvents.Remove(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>
/// Marker base for domain events dispatched through MediatR.
/// Uses MediatR.Contracts (a zero-dependency interface package)
/// so the Domain layer stays free of the full MediatR runtime.
/// </summary>
public abstract record DomainEvent : MediatR.INotification;
