using MediatR;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Extensions;
using OrderFlow.Application.Orders.DTOs;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Application.Orders.Queries.GetOrderById;

public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto>;

public sealed class GetOrderByIdHandler(IOrderRepository repository)
    : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException(request.OrderId);

        return order.ToDto();
    }
}
