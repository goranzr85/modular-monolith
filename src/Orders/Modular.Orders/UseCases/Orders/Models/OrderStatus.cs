using ErrorOr;
using MediatR;
using Modular.Common;
using Modular.Orders.Errors;

namespace Modular.Orders.UseCases.Orders.Models;

public abstract class OrderStatus : Enumeration<OrderStatus>
{
    protected OrderStatus(int value, string name)
        : base(value, name)
    {
    }

    public static OrderStatus Pending => new PendingStatus();
    public static OrderStatus Submitted => new SubmittedStatus();
    public static OrderStatus Shipped => new ShippedOrderStatus();
    public static OrderStatus Canceled => new CanceledStatus();

    internal abstract ErrorOr<Unit> ChangeStatus(Order order, OrderStatus status);
}

internal sealed class PendingStatus : OrderStatus
{
    public PendingStatus()
        : base(1, nameof(Pending))
    {
    }

    internal override ErrorOr<Unit> ChangeStatus(Order order, OrderStatus status)
    {
        if (status is not SubmittedStatus or CanceledStatus)
        {
            return OrderErrors.OrderStatusIllegalTransition(order.Id, this, status);
        }

        order.Status = status;

        return Unit.Value;
    }
}

internal sealed class SubmittedStatus : OrderStatus
{
    public SubmittedStatus()
        : base(2, nameof(Submitted))
    {
    }

    internal override ErrorOr<Unit> ChangeStatus(Order order, OrderStatus status)
    {
        if (status is not ShippedOrderStatus or CanceledStatus)
        {
            return OrderErrors.OrderStatusIllegalTransition(order.Id, this, status);
        }

        order.Status = status;

        return Unit.Value;
    }
}

internal sealed class ShippedOrderStatus : OrderStatus
{
    internal ShippedOrderStatus()
        : base(3, nameof(Shipped))
    {
    }

    internal override ErrorOr<Unit> ChangeStatus(Order order, OrderStatus status)
    {
        return OrderErrors.OrderStatusIllegalTransition(order.Id, this, status);
    }
}

internal sealed class CanceledStatus : OrderStatus
{
    internal CanceledStatus()
        : base(4, nameof(Canceled))
    {
    }

    internal override ErrorOr<Unit> ChangeStatus(Order order, OrderStatus status)
    {
        return OrderErrors.OrderStatusIllegalTransition(order.Id, this, status);
    }
}