using Modular.Common;

namespace Modular.Orders.Models;

public abstract class OrderStatus : Enumeration<OrderStatus>
{
    protected OrderStatus(int value, string name)
        : base(value, name)
    {
    }

    public static OrderStatus Pending => new PendingStatus();
    public static OrderStatus Confirmed => new ConfirmedStatus();
    public static OrderStatus Shipped => new ShippedStatus();
    public static OrderStatus Delivered => new DeliveredStatus();
    public static OrderStatus Canceled => new CanceledStatus();

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

internal sealed class ConfirmedStatus : OrderStatus
{
    public ConfirmedStatus()
        : base(2, nameof(Pending))
    {
    }

    internal override void ChangeStatus(Order order, OrderStatus status)
    {
        if (order.Status is not PendingStatus)
        {
            // return Error with message InvalidStatusTransition
            return;
        }

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
        : base(3, nameof(Shipped))
    {
    }

    internal override void ChangeStatus(Order order, OrderStatus status)
    {
        if (order.Status is not ConfirmedStatus)
        {
            return;
        }

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
        : base(4, nameof(Delivered))
    {
    }

    internal override void ChangeStatus(Order order, OrderStatus status)
    {
    }
}

internal sealed class CanceledStatus : OrderStatus
{
    internal CanceledStatus()
        : base(5, nameof(Canceled))
    {
    }

    internal override void ChangeStatus(Order order, OrderStatus status)
    {
        if (order.Status is not PendingStatus)
        {
            return;
        }

        order.Status = status;
    }
}