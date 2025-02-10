using Modular.Common;

namespace Modular.Orders;

internal sealed class Order
{
    public int OrderId { get; private set; }
    public DateTime OrderDate { get; private set; }
    public Guid CustomerId { get; private set; }
    public Price TotalAmount { get; private set; }
    public List<OrderItem> Items { get; private set; }
    public OrderStatus Status { get; internal set; }

    private Order()
    {
        Items = new List<OrderItem>();
    }

    internal static Order Create(int orderId, DateTime orderDate, Guid customerId, Price totalAmount, List<OrderItem> items,
        OrderStatus status)
    {
        if (orderId == default)
        {
            throw new ArgumentException("OrderId cannot be empty.", nameof(orderId));
        }

        if (orderDate == DateTime.MinValue)
        {
            throw new ArgumentException("OrderDate cannot be empty.", nameof(orderDate));
        }

        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("CustomerId cannot be empty.", nameof(customerId));
        }

        if (totalAmount <= 0)
        {
            throw new ArgumentException("TotalAmount cannot be less than or equal to zero.", nameof(totalAmount));
        }

        if (items is null || items.Count == 0)
        {
            throw new ArgumentException("Items cannot be null or empty.", nameof(items));
        }

        if (status is null)
        {
            throw new ArgumentException("Status cannot be null.", nameof(status));
        }

        return new Order
        {
            OrderId = orderId,
            OrderDate = orderDate,
            CustomerId = customerId,
            TotalAmount = totalAmount,
            Items = items,
            Status = status
        };
    }

    internal void ChangeStatus(OrderStatus orderStatus)
    {
        if (orderStatus is null)
        {
            throw new ArgumentNullException(nameof(orderStatus), "Order status cannot be null.");
        }

        Status.ChangeStatus(this, orderStatus);
    }

    internal void AddItem(OrderItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item), "Item cannot be null.");
        }

        //if(items)

        Items.Add(item);
    }

}