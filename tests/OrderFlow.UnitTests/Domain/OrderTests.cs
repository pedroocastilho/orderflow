using FluentAssertions;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Exceptions;
using Xunit;

namespace OrderFlow.UnitTests.Domain;

public class OrderTests
{
    private static OrderItem ValidItem() =>
        OrderItem.Create("prod-1", "Widget Pro", 2, 49.99m);

    [Fact]
    public void Create_WithValidData_ShouldInitializePending()
    {
        var order = Order.Create("customer-42", [ValidItem()]);

        order.Id.Should().NotBeEmpty();
        order.CustomerId.Should().Be("customer-42");
        order.Status.Should().Be(OrderStatus.Pending);
        order.Items.Should().HaveCount(1);
        order.Total.Should().Be(99.98m);
        order.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithNoItems_ShouldThrowDomainException()
    {
        var act = () => Order.Create("customer-42", []);

        act.Should().Throw<DomainException>()
            .WithMessage("*at least one item*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_WithInvalidCustomerId_ShouldThrow(string customerId)
    {
        var act = () => Order.Create(customerId, [ValidItem()]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Confirm_FromPending_ShouldTransitionToConfirmed()
    {
        var order = Order.Create("customer-42", [ValidItem()]);

        order.Confirm();

        order.Status.Should().Be(OrderStatus.Confirmed);
        order.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Confirm_FromNonPending_ShouldThrowDomainException()
    {
        var order = Order.Create("customer-42", [ValidItem()]);
        order.Confirm();

        var act = () => order.Confirm();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void FullLifecycle_PendingToDelivered_ShouldSucceed()
    {
        var order = Order.Create("customer-42", [ValidItem()]);

        order.Confirm();
        order.Process();
        order.Ship();
        order.Deliver();

        order.Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public void Cancel_FromShipped_ShouldThrowDomainException()
    {
        var order = Order.Create("customer-42", [ValidItem()]);
        order.Confirm();
        order.Process();
        order.Ship();

        var act = () => order.Cancel();

        act.Should().Throw<DomainException>()
            .WithMessage("*shipped or delivered*");
    }

    [Fact]
    public void Cancel_FromPending_ShouldSucceed()
    {
        var order = Order.Create("customer-42", [ValidItem()]);

        order.Cancel();

        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Total_ShouldSumAllItemSubtotals()
    {
        var items = new[]
        {
            OrderItem.Create("p1", "Item A", 3, 10.00m),
            OrderItem.Create("p2", "Item B", 1, 25.50m)
        };

        var order = Order.Create("customer-42", items);

        order.Total.Should().Be(55.50m);
    }
}
