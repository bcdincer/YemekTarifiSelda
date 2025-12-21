using BackendApi.Application.Services;
using BackendApi.Domain.Events;
using BackendApi.Domain.Interfaces;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace BackendApi.Infrastructure.Events;

/// <summary>
/// Event publisher using Hangfire for async processing
/// </summary>
public class HangfireEventPublisher : IEventPublisher
{
    private readonly ILogger<HangfireEventPublisher> _logger;

    public HangfireEventPublisher(ILogger<HangfireEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(T domainEvent) where T : DomainEvent
    {
        _logger.LogInformation("Publishing domain event {EventType} with ID {EventId}", typeof(T).Name, domainEvent.EventId);

        // Enqueue the event handler as a background job
        BackgroundJob.Enqueue<DomainEventHandler<T>>(handler => handler.HandleAsync(domainEvent));

        return Task.CompletedTask;
    }
}

/// <summary>
/// Generic domain event handler for Hangfire
/// </summary>
public class DomainEventHandler<T> where T : DomainEvent
{
    private readonly ILogger<DomainEventHandler<T>> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DomainEventHandler(ILogger<DomainEventHandler<T>> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task HandleAsync(T domainEvent)
    {
        _logger.LogInformation("Handling domain event {EventType} with ID {EventId}", typeof(T).Name, domainEvent.EventId);

        try
        {
            // Handle different event types
            switch (domainEvent)
            {
                case RecipeCreatedEvent recipeCreated:
                    await HandleRecipeCreatedAsync(recipeCreated);
                    break;
                default:
                    _logger.LogWarning("No handler found for event type {EventType}", typeof(T).Name);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling domain event {EventType} with ID {EventId}", typeof(T).Name, domainEvent.EventId);
            throw;
        }
    }

    private async Task HandleRecipeCreatedAsync(RecipeCreatedEvent evt)
    {
        var emailService = _serviceProvider.GetRequiredService<BackendApi.Application.Services.IEmailService>();
        await emailService.SendRecipeCreatedNotificationAsync(evt.RecipeId, evt.RecipeTitle, evt.UserEmail);
    }
}

