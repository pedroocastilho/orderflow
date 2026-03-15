namespace OrderFlow.Domain.Events;

public record OrderCreatedEvent(
    Guid OrderId,
    string CustomerId,
    decimal Total,
    int ItemCount,
    DateTime OccurredAt
);
