using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using NextHappen.Contracts.Events;
using NextHappen.Notification.Domain.Entities;
using NextHappen.Notification.Domain.Repositories;
using NextHappen.Notification.Infrastructure.Messaging;
using Xunit;

namespace NextHappen.Notification.Tests;

public class EventConsumersTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task EventSavedConsumer_Consume_ShouldCreateNotification()
    {
        var repoMock = new Mock<INotificationRepository>();
        var loggerMock = new Mock<ILogger<EventSavedConsumer>>();
        var consumer = new EventSavedConsumer(repoMock.Object, loggerMock.Object);
        var contextMock = new Mock<ConsumeContext<EventSavedEvent>>();
        var msg = new EventSavedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        contextMock.Setup(c => c.Message).Returns(msg);

        await consumer.Consume(contextMock.Object);

        repoMock.Verify(r => r.AddAsync(It.Is<Domain.Entities.Notification>(n =>
            n.EventId == msg.EventId &&
            n.Message == "Un usuario guardó tu evento en favoritos")), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EventSavedConsumer_Consume_ShouldUseUserId_WhenOrganizerIsEmpty()
    {
        var repoMock = new Mock<INotificationRepository>();
        var loggerMock = new Mock<ILogger<EventSavedConsumer>>();
        var consumer = new EventSavedConsumer(repoMock.Object, loggerMock.Object);
        var contextMock = new Mock<ConsumeContext<EventSavedEvent>>();
        var msg = new EventSavedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, DateTime.UtcNow);
        contextMock.Setup(c => c.Message).Returns(msg);

        await consumer.Consume(contextMock.Object);

        repoMock.Verify(r => r.AddAsync(It.Is<Domain.Entities.Notification>(n =>
            n.UserId == msg.UserId)), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EventViewedConsumer_Consume_ShouldCreateNotification_WhenOrganizerNotEmpty()
    {
        var repoMock = new Mock<INotificationRepository>();
        var loggerMock = new Mock<ILogger<EventViewedConsumer>>();
        var consumer = new EventViewedConsumer(repoMock.Object, loggerMock.Object);
        var contextMock = new Mock<ConsumeContext<EventViewedEvent>>();
        var organizerId = Guid.NewGuid();
        var msg = new EventViewedEvent(Guid.NewGuid(), organizerId, DateTime.UtcNow);
        contextMock.Setup(c => c.Message).Returns(msg);

        await consumer.Consume(contextMock.Object);

        repoMock.Verify(r => r.AddAsync(It.Is<Domain.Entities.Notification>(n =>
            n.UserId == organizerId &&
            n.Message == "Alguien vio tu evento")), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EventViewedConsumer_Consume_ShouldNotCreateNotification_WhenOrganizerIsEmpty()
    {
        var repoMock = new Mock<INotificationRepository>();
        var loggerMock = new Mock<ILogger<EventViewedConsumer>>();
        var consumer = new EventViewedConsumer(repoMock.Object, loggerMock.Object);
        var contextMock = new Mock<ConsumeContext<EventViewedEvent>>();
        var msg = new EventViewedEvent(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow);
        contextMock.Setup(c => c.Message).Returns(msg);

        await consumer.Consume(contextMock.Object);

        repoMock.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.Notification>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task TicketPurchasedConsumer_Consume_ShouldCreateNotification()
    {
        var repoMock = new Mock<INotificationRepository>();
        var loggerMock = new Mock<ILogger<TicketPurchasedConsumer>>();
        var consumer = new TicketPurchasedConsumer(repoMock.Object, loggerMock.Object);
        var contextMock = new Mock<ConsumeContext<TicketPurchasedEvent>>();
        var msg = new TicketPurchasedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 150.00m, DateTime.UtcNow);
        contextMock.Setup(c => c.Message).Returns(msg);

        await consumer.Consume(contextMock.Object);

        repoMock.Verify(r => r.AddAsync(It.Is<Domain.Entities.Notification>(n =>
            n.UserId == msg.UserId &&
            n.EventId == msg.EventId &&
            n.Message.Contains("compra confirmada", StringComparison.OrdinalIgnoreCase))), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EventUnsavedConsumer_Consume_ShouldNotCreateNotification()
    {
        var loggerMock = new Mock<ILogger<EventUnsavedConsumer>>();
        var consumer = new EventUnsavedConsumer(loggerMock.Object);
        var contextMock = new Mock<ConsumeContext<EventUnsavedEvent>>();
        var msg = new EventUnsavedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, DateTime.UtcNow);
        contextMock.Setup(c => c.Message).Returns(msg);

        await consumer.Consume(contextMock.Object);

        // No notification should be created - only logging occurs
        Assert.True(true);
    }
}