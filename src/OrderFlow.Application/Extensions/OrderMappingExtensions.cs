using OrderFlow.Application.Orders.DTOs;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Extensions;

public static class OrderMappingExtensions
{
    public static OrderDto ToDto(this Order order) =>
        new(
            order.Id,
            order.CustomerId,
            order.Status.ToString(),
            order.Total,
            order.Items.Select(i => i.ToDto()).ToList(),
            order.CreatedAt,
            order.UpdatedAt
        );

    public static OrderItemDto ToDto(this OrderItem item) =>
        new(
            item.Id,
            item.ProductId,
            item.ProductName,
            item.Quantity,
            item.UnitPrice,
            item.Subtotal
        );
}
