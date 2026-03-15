using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public string CustomerId { get; private set; } = default!;
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public decimal Total => _items.Sum(i => i.Subtotal);

    private Order() { }

    public static Order Create(string customerId, IEnumerable<OrderItem> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);

        var itemList = items.ToList();

        if (itemList.Count == 0)
            throw new DomainException("An order must contain at least one item.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        order._items.AddRange(itemList);
        return order;
    }

    public void Confirm()
    {
        EnsureStatus(OrderStatus.Pending, "Only pending orders can be confirmed.");
        Transition(OrderStatus.Confirmed);
    }

    public void Process()
    {
        EnsureStatus(OrderStatus.Confirmed, "Only confirmed orders can be processed.");
        Transition(OrderStatus.Processing);
    }

    public void Ship()
    {
        EnsureStatus(OrderStatus.Processing, "Only processing orders can be shipped.");
        Transition(OrderStatus.Shipped);
    }

    public void Deliver()
    {
        EnsureStatus(OrderStatus.Shipped, "Only shipped orders can be delivered.");
        Transition(OrderStatus.Delivered);
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered)
            throw new DomainException("Cannot cancel an order that has already been shipped or delivered.");

        Transition(OrderStatus.Cancelled);
    }

    private void Transition(OrderStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureStatus(OrderStatus expected, string message)
    {
        if (Status != expected)
            throw new DomainException(message);
    }
}
