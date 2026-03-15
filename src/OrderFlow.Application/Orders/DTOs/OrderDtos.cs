namespace OrderFlow.Application.Orders.DTOs;

public record OrderDto(
    Guid Id,
    string CustomerId,
    string Status,
    decimal Total,
    IReadOnlyList<OrderItemDto> Items,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record OrderItemDto(
    Guid Id,
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal
);
