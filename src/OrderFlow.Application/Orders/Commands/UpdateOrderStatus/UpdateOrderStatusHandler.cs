using MediatR;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Extensions;
using OrderFlow.Application.Orders.DTOs;
using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Application.Orders.Commands.UpdateOrderStatus;

public record UpdateOrderStatusCommand(Guid OrderId, OrderStatus NewStatus) : IRequest<OrderDto>;

public sealed class UpdateOrderStatusHandler(IOrderRepository repository)
    : IRequestHandler<UpdateOrderStatusCommand, OrderDto>
{
    public async Task<OrderDto> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException(request.OrderId);

        switch (request.NewStatus)
        {
            case OrderStatus.Confirmed: order.Confirm(); break;
            case OrderStatus.Processing: order.Process(); break;
            case OrderStatus.Shipped: order.Ship(); break;
            case OrderStatus.Delivered: order.Deliver(); break;
            case OrderStatus.Cancelled: order.Cancel(); break;
            default:
                throw new DomainException($"Transition to '{request.NewStatus}' is not supported.");
        }

        await repository.UpdateAsync(order, cancellationToken);

        return order.ToDto();
    }
}
