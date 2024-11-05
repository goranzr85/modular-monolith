using static Modular.Orders.Order;

namespace Modular.Orders;

public class Order
{
    public Guid OrderId { get; private set; }  // Unique identifier for the order
    public DateTime OrderDate { get; private set; }  // Date when the order was placed
    public Guid CustomerId { get; private set; }  // Name of the customer
    public decimal TotalAmount { get; private set; }  // Total amount for the order
    public List<OrderItem> Items { get; private set; }  // List of items in the order
    public OrderStatus Status { get; internal set; }  // Order status (e.g., Pending, Shipped, Delivered)

    private Order()
    {
        Items = new List<OrderItem>();
    }

    public static Order Create(Guid orderId, DateTime orderDate, Guid customerId, decimal totalAmount, List<OrderItem> items,
        OrderStatus status)
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

    public void ChangeStatus(OrderStatus orderStatus)
    {
        if (orderStatus is null)
        {
            throw new ArgumentNullException(nameof(orderStatus), "Order status cannot be null.");
        }

        Status.ChangeStatus(this, orderStatus);
    }

    public void AddItem(OrderItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item), "Item cannot be null.");
        }

        //if(items)

        Items.Add(item);
    }

    public class OrderItem
    {
        public Guid ItemId { get; set; }  // Unique identifier for the item
        public Guid ProductId { get; set; }  // Name of the product
        public uint Quantity { get; set; }  // Quantity of the product ordered
        public decimal Price { get; set; }  // Price per unit of the product
    }

    public class Product
    {
        public Guid ProductId { get; set; }  // Unique identifier for the product
        public string Name { get; set; }  // Name of the product
        public string Description { get; set; }  // Detailed description of the product
        public decimal Price { get; set; }  // Price of the product
        public uint StockQuantity { get; set; }  // Quantity of the product available in stock
        public string SKU { get; set; }  // Stock Keeping Unit identifier

        // Constructor
        public Product()
        {
            // Initialize properties if needed
        }
    }

    public abstract class OrderStatus
    {
        public static OrderStatus Pending => new PendingStatus();
        public static OrderStatus Shipped => new ShippedStatus();
        public static OrderStatus Delivered => new DeliveredStatus();

        public abstract void ChangeStatus(Order order, OrderStatus status);
    }

    internal class PendingStatus : OrderStatus
    {
        public PendingStatus()
        {
        }

        public override void ChangeStatus(Order order, OrderStatus status)
        {
            if (status is PendingStatus)
            {
                return;
            }

            order.Status = status;
        }
    }
}

internal class DeliveredStatus : OrderStatus
{
    public override void ChangeStatus(Order order, OrderStatus status)
    {
        return;
    }
}

internal class ShippedStatus : OrderStatus
{
    public override void ChangeStatus(Order order, OrderStatus status)
    {
        if (status is PendingStatus or ShippedStatus)
        {
            return;
        }

        order.Status = status;
    }
}