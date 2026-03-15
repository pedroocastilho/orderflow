using MediatR;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Extensions;
using OrderFlow.Application.Orders.DTOs;

namespace OrderFlow.Application.Orders.Queries.GetOrders;

public record GetOrdersQuery(int Page = 1, int PageSize = 20) : IRequest<IReadOnlyList<OrderDto>>;

public sealed class GetOrdersHandler(IOrderRepository repository)
    : IRequestHandler<GetOrdersQuery, IReadOnlyList<OrderDto>>
{
    public async Task<IReadOnlyList<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await repository.GetAllAsync(request.Page, request.PageSize, cancellationToken);
        return orders.Select(o => o.ToDto()).ToList();
    }
}
