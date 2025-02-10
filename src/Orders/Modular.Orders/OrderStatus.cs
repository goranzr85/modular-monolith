using Modular.Common;

namespace Modular.Orders;

internal abstract class OrderStatus : Enumeration<OrderStatus>
{
    protected OrderStatus(int value, string name)
        : base(value, name)
    {
    }

    public static OrderStatus Pending => new PendingStatus();
    public static OrderStatus Shipped => new ShippedStatus();
    public static OrderStatus Delivered => new DeliveredStatus();

    internal abstract void ChangeStatus(Order order, OrderStatus status);
}

internal sealed class PendingStatus : OrderStatus
{
    public PendingStatus()
        : base(1, nameof(Pending))
    {
    }

    internal override void ChangeStatus(Order order, OrderStatus status)
    {
        if (status is PendingStatus)
        {
            return;
        }

        order.Status = status;
    }
}

internal sealed class ShippedStatus : OrderStatus
{
    internal ShippedStatus()
        : base(2, nameof(Shipped))
    {
    }

    internal override void ChangeStatus(Order order, OrderStatus status)
    {
        if (status is PendingStatus or ShippedStatus)
        {
            return;
        }

        order.Status = status;
    }
}

internal sealed class DeliveredStatus : OrderStatus
{
    internal DeliveredStatus()
        : base(3, nameof(Delivered))
    {
    }

    internal override void ChangeStatus(Order order, OrderStatus status)
    {
    }
}
