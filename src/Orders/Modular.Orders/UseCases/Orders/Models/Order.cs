using ErrorOr;
using MediatR;
using Modular.Common;
using Modular.Orders.Errors;
using Modular.Orders.UseCases.Orders.Cancel;
using Modular.Orders.UseCases.Orders.Change.AddProducts;
using Modular.Orders.UseCases.Orders.Change.ChangeProductQuantity.Decrease;
using Modular.Orders.UseCases.Orders.Change.ChangeProductQuantity.Increase;
using Modular.Orders.UseCases.Orders.Change.RemoveProducts;
using Modular.Orders.UseCases.Orders.Create;
using Modular.Orders.UseCases.Orders.Submitted;

namespace Modular.Orders.UseCases.Orders.Models;

public sealed class Order : AggregateRoot
{
    public Guid Id { get; private set; }
    public DateTimeOffset OrderDate { get; private set; }
    public Guid CustomerId { get; private set; }
    public Price TotalAmount { get; private set; }
    public List<OrderItem> Items { get; private set; }
    public OrderStatus Status { get; internal set; }

    private Order()
    {
        Items = new List<OrderItem>();
    }

    internal static Order Create(Guid orderId, DateTimeOffset orderDate, Guid customerId, List<OrderItem> items)
    {
        if (orderId == Guid.Empty)
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

        order.RaiseEvent(new OrderCreatedEvent(orderId, items));

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

        existingOrderItem.IncreaseQuantity(quantity - existingOrderItem.Quantity);
        RaiseEvent(new IncreasedProductQuantityInOrderEvent(Id, existingOrderItem.ProductId, existingOrderItem.Quantity));
    }

    internal ErrorOr<Unit> IncreaseQuantity(int productId, uint quantity)
    {
        OrderItem? existingOrderItem = Items.FirstOrDefault(i => i.ProductId == productId);

        if (existingOrderItem is null)
        {
            return OrderErrors.ProductIsNotPlaced(Id, productId);
        }

        existingOrderItem.IncreaseQuantity(quantity + existingOrderItem.Quantity);
        RaiseEvent(new IncreasedProductQuantityInOrderEvent(Id, existingOrderItem.ProductId, existingOrderItem.Quantity));

        return Unit.Value;
    }

    internal ErrorOr<Unit> DecreaseQuantity(int productId, uint quantity)
    {
        OrderItem? existingOrderItem = Items.FirstOrDefault(i => i.ProductId == productId);

        if (existingOrderItem is null)
        {
            return OrderErrors.ProductIsNotPlaced(Id, productId);
        }

        if (existingOrderItem.Quantity < quantity)
        {
            return OrderErrors.ProductQuantityIsNotEnoughForDecrease(Id, productId, quantity);
        }

        existingOrderItem.DecreaseQuantity(existingOrderItem.Quantity - quantity);
        RaiseEvent(new DecreasedProductQuantityInOrderEvent(Id, existingOrderItem.ProductId, existingOrderItem.Quantity));

        return Unit.Value;
    }

    internal void RemoveItem(int productId)
    {
        OrderItem? existingOrderItem = Items.FirstOrDefault(i => i.ProductId == productId);

        if (existingOrderItem is null)
        {
            throw new InvalidOperationException("Item not found.");
        }

        Items.Remove(existingOrderItem);

        RaiseEvent(new OrderItemRemovedEvent(productId, existingOrderItem.Quantity));
    }

    internal ErrorOr<Unit> Cancel()
    {
        ErrorOr<Unit> result = ChangeStatus(OrderStatus.Canceled);

        if (result.IsError)
        {
            return result.FirstError;
        }

        RaiseEvent(new OrderCanceledEvent(Id));

        return Unit.Value;
    }
    
    internal ErrorOr<Unit> Submit()
    {
        ErrorOr<Unit> result = ChangeStatus(OrderStatus.Submitted);

        if (result.IsError)
        {
            return result.FirstError;
        }

        RaiseEvent(new OrderSubmittedEvent(Id, CustomerId, TotalAmount));

        return Unit.Value;
    }

    private ErrorOr<Unit> ChangeStatus(OrderStatus orderStatus)
    {
        if (orderStatus is null)
        {
            throw new ArgumentNullException(nameof(orderStatus), "Order status cannot be null.");
        }

        return Status.ChangeStatus(this, orderStatus);
    }
}