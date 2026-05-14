using MassTransit;
using NextHappen.Contracts.Events;
using NextHappen.Notification.Domain.Repositories;

namespace NextHappen.Notification.Infrastructure.Messaging;

/// <summary>
/// Consumes EventSavedEvent from RabbitMQ and creates a notification.
/// </summary>
public class EventSavedConsumer : IConsumer<EventSavedEvent>
{
    private readonly INotificationRepository _repo;
    private readonly ILogger<EventSavedConsumer> _logger;

    public EventSavedConsumer(INotificationRepository repo, ILogger<EventSavedConsumer> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventSavedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("[RabbitMQ] EventSaved: User {UserId} saved event {EventId}", msg.UserId, msg.EventId);

        await _repo.AddAsync(new Domain.Entities.Notification
        {
            UserId = msg.OrganizerId != Guid.Empty ? msg.OrganizerId : msg.UserId,
            EventId = msg.EventId,
            Message = $"Un usuario guardó tu evento en favoritos",
            Timestamp = msg.Timestamp
        });
    }
}

/// <summary>
/// Consumes EventUnsavedEvent from RabbitMQ.
/// </summary>
public class EventUnsavedConsumer : IConsumer<EventUnsavedEvent>
{
    private readonly ILogger<EventUnsavedConsumer> _logger;

    public EventUnsavedConsumer(ILogger<EventUnsavedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<EventUnsavedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("[RabbitMQ] EventUnsaved: User {UserId} removed event {EventId}", msg.UserId, msg.EventId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Consumes EventViewedEvent from RabbitMQ and creates a notification.
/// </summary>
public class EventViewedConsumer : IConsumer<EventViewedEvent>
{
    private readonly INotificationRepository _repo;
    private readonly ILogger<EventViewedConsumer> _logger;

    public EventViewedConsumer(INotificationRepository repo, ILogger<EventViewedConsumer> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventViewedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("[RabbitMQ] EventViewed: Event {EventId} was viewed", msg.EventId);

        if (msg.OrganizerId != Guid.Empty)
        {
            await _repo.AddAsync(new Domain.Entities.Notification
            {
                UserId = msg.OrganizerId,
                EventId = msg.EventId,
                Message = "Alguien vio tu evento",
                Timestamp = msg.Timestamp
            });
        }
    }
}
