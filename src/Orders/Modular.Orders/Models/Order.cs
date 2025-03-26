using Modular.Common;
using Modular.Orders.Cancel;
using Modular.Orders.Change;
using Modular.Orders.Change.AddProducts;
using Modular.Orders.Change.ChangeProductQuantity;
using Modular.Orders.Change.RemoveProducts;
using Modular.Orders.Create;

namespace Modular.Orders.Models;

public sealed class Order : AggregateRoot
{
    public Guid Id { get; private set; }
    public DateTime OrderDate { get; private set; }
    public Guid CustomerId { get; private set; }
    public Price TotalAmount { get; private set; }
    public List<OrderItem> Items { get; private set; }
    public OrderStatus Status { get; internal set; }

    private Order()
    {
        Items = new List<OrderItem>();
    }

    internal static Order Create(Guid orderId, DateTime orderDate, Guid customerId, List<OrderItem> items)
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

        if (items is null || items.Count == 0)
        {
            throw new ArgumentException("Items cannot be null or empty.", nameof(items));
        }

        Order order = new Order
        {
            Id = orderId,
            OrderDate = orderDate,
            CustomerId = customerId,
            TotalAmount = Price.Create(items.Sum(x => x.Price)),
            Items = items,
            Status = OrderStatus.Pending
        };

        order.RaiseEvent(new OrderCreated(orderId, items));

        return order;
    }

    internal void AddItem(int productId, uint quantity, Price price)
    {
        OrderItem? existingOrderItem = Items.FirstOrDefault(i => i.ProductId == productId);

        if (existingOrderItem is null)
        {
            OrderItem item = new OrderItem
            {
                OrderId = Id,
                ProductId = productId,
                Quantity = quantity,
                Price = price
            };

            Items.Add(item);
            RaiseEvent(new OrderItemAddedEvent(item.ProductId, item.Quantity));

            return;
        }

        if (existingOrderItem.Quantity < quantity)
        {
            existingOrderItem.IncreaseQuantity(quantity - existingOrderItem.Quantity);
            RaiseEvent(new IncreasedProductQuantity(Id, existingOrderItem.ProductId, existingOrderItem.Quantity));
        }
        else if (existingOrderItem.Quantity > quantity)
        {
            existingOrderItem.DecreaseQuantity(existingOrderItem.Quantity - quantity);
            RaiseEvent(new DecreasedProductQuantity(Id, existingOrderItem.ProductId, existingOrderItem.Quantity));
        }

    }

    internal void RemoveItem(int productId)
    {
        OrderItem? existingOrderItem = Items.FirstOrDefault(i => i.ProductId == productId);

        if (existingOrderItem is null)
        {
            throw new InvalidOperationException("Item not found.");
        }

        Items.Remove(existingOrderItem);

        RaiseEvent(new OrderItemRemoved(productId, existingOrderItem.Quantity));
    }

    internal void Cancel()
    {
        ChangeStatus(OrderStatus.Canceled);

        RaiseEvent(new OrderCanceledEvent(Id));
    }

    private void ChangeStatus(OrderStatus orderStatus)
    {
        if (orderStatus is null)
        {
            throw new ArgumentNullException(nameof(orderStatus), "Order status cannot be null.");
        }

        Status.ChangeStatus(this, orderStatus);
    }
}