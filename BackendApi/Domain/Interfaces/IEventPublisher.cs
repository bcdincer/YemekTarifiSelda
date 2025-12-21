using BackendApi.Domain.Events;

namespace BackendApi.Domain.Interfaces;

/// <summary>
/// Publishes domain events asynchronously (queue-based)
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T domainEvent) where T : DomainEvent;
}

