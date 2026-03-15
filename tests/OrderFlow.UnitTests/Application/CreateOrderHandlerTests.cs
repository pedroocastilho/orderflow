using FluentAssertions;
using Moq;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Orders.Commands.CreateOrder;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Events;
using Xunit;

namespace OrderFlow.UnitTests.Application;

public class CreateOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _repositoryMock = new();
    private readonly Mock<IMessagePublisher> _publisherMock = new();

    private CreateOrderHandler CreateSut() =>
        new(_repositoryMock.Object, _publisherMock.Object);

    [Fact]
    public async Task Handle_WithValidCommand_ShouldPersistOrderAndPublishEvent()
    {
        var command = new CreateOrderCommand(
            "customer-99",
            [new("prod-1", "Keyboard", 1, 299.90m)]);

        var sut = CreateSut();
        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.CustomerId.Should().Be("customer-99");
        result.Status.Should().Be("Pending");
        result.Total.Should().Be(299.90m);

        _repositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _publisherMock.Verify(
            p => p.PublishAsync(
                It.IsAny<OrderCreatedEvent>(),
                "orders.created",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleItems_ShouldCalculateTotalCorrectly()
    {
        var command = new CreateOrderCommand(
            "customer-55",
            [
                new("prod-1", "Monitor", 2, 1200.00m),
                new("prod-2", "Cable", 3, 29.90m)
            ]);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.Total.Should().Be(2489.70m);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ShouldNotPublishEvent()
    {
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB unavailable"));

        var command = new CreateOrderCommand(
            "customer-1",
            [new("prod-1", "Headset", 1, 150.00m)]);

        var act = async () => await CreateSut().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        _publisherMock.Verify(
            p => p.PublishAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
