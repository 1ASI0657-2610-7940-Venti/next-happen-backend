namespace NextHappen.Contracts.Events;

/// <summary>
/// Published when a user saves an event to their favorites.
/// </summary>
public record EventSavedEvent(
    Guid EventId,
    Guid UserId,
    Guid OrganizerId,
    DateTime Timestamp
);

/// <summary>
/// Published when a user removes an event from their favorites.
/// </summary>
public record EventUnsavedEvent(
    Guid EventId,
    Guid UserId,
    Guid OrganizerId,
    DateTime Timestamp
);

/// <summary>
/// Published when a user views an event detail page.
/// </summary>
public record EventViewedEvent(
    Guid EventId,
    Guid OrganizerId,
    DateTime Timestamp
);

/// <summary>
/// Published when a new event is created.
/// </summary>
public record EventCreatedEvent(
    Guid EventId,
    Guid OrganizerId,
    string Title,
    DateTime Timestamp
);

/// <summary>
/// Published when an event is deleted.
/// </summary>
public record EventDeletedEvent(
    Guid EventId,
    DateTime Timestamp
);

/// <summary>
/// Published when a ticket is purchased.
/// </summary>
public record TicketPurchasedEvent(
    Guid TicketId,
    Guid EventId,
    Guid UserId,
    decimal Price,
    DateTime Timestamp
);
