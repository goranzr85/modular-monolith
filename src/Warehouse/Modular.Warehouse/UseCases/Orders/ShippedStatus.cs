namespace Modular.Warehouse.UseCases.Orders;
public abstract class ProductShippedStatus
{
    public bool IsShipped { get; init; }
    public DateTimeOffset? Date { get; init; }

    public static ProductShippedStatus Shipped => new ShippedStatus();
    public static ProductShippedStatus NotShipped => new NotShippedStatus();
}

public sealed class ShippedStatus : ProductShippedStatus
{
    public ShippedStatus() : base()
    {
        IsShipped = true;
        Date = DateTimeOffset.UtcNow;
    }
}

public sealed class NotShippedStatus : ProductShippedStatus
{
    public NotShippedStatus() : base()
    {
        IsShipped = false;
        Date = null;
    }
}
