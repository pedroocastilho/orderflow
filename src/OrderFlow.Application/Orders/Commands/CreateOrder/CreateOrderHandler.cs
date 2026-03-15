using MediatR;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Extensions;
using OrderFlow.Application.Orders.DTOs;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Events;

namespace OrderFlow.Application.Orders.Commands.CreateOrder;

public record CreateOrderCommand(
    string CustomerId,
    IReadOnlyList<CreateOrderItemDto> Items
) : IRequest<OrderDto>;

public record CreateOrderItemDto(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

public sealed class CreateOrderHandler(
    IOrderRepository repository,
    IMessagePublisher publisher) : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var items = request.Items.Select(i =>
            OrderItem.Create(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice));

        var order = Order.Create(request.CustomerId, items);

        await repository.AddAsync(order, cancellationToken);

        var @event = new OrderCreatedEvent(
            order.Id,
            order.CustomerId,
            order.Total,
            order.Items.Count,
            DateTime.UtcNow);

        await publisher.PublishAsync(@event, "orders.created", cancellationToken);

        return order.ToDto();
    }
}
