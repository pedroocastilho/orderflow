using FluentAssertions;
using Moq;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Orders.Commands.UpdateOrderStatus;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Exceptions;
using Xunit;

namespace OrderFlow.UnitTests.Application;

public class UpdateOrderStatusHandlerTests
{
    private readonly Mock<IOrderRepository> _repositoryMock = new();

    private UpdateOrderStatusHandler CreateSut() =>
        new(_repositoryMock.Object);

    private static Order PendingOrder()
    {
        var item = OrderItem.Create("p1", "Product", 1, 100m);
        return Order.Create("customer-1", [item]);
    }

    [Fact]
    public async Task Handle_ConfirmPendingOrder_ShouldReturnConfirmedStatus()
    {
        var order = PendingOrder();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await CreateSut().Handle(
            new UpdateOrderStatusCommand(order.Id, OrderStatus.Confirmed),
            CancellationToken.None);

        result.Status.Should().Be("Confirmed");

        _repositoryMock.Verify(
            r => r.UpdateAsync(order, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ShouldThrowOrderNotFoundException()
    {
        var unknownId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(unknownId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var act = async () => await CreateSut().Handle(
            new UpdateOrderStatusCommand(unknownId, OrderStatus.Confirmed),
            CancellationToken.None);

        await act.Should().ThrowAsync<OrderNotFoundException>();
    }

    [Fact]
    public async Task Handle_InvalidTransition_ShouldThrowDomainException()
    {
        var order = PendingOrder();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var act = async () => await CreateSut().Handle(
            new UpdateOrderStatusCommand(order.Id, OrderStatus.Shipped),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
