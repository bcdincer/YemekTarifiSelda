namespace BackendApi.Domain.Events;

/// <summary>
/// Base class for domain events
/// </summary>
public abstract class DomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}

