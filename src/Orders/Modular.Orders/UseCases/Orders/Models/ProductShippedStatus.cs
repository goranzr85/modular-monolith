namespace Modular.Orders.UseCases.Orders.Models;

public sealed class ProductShippedStatus
{
    public bool IsShipped { get; init; }
    public DateTimeOffset? Date { get; init; }

    public static ProductShippedStatus Shipped => new() { IsShipped = true, Date = DateTimeOffset.UtcNow };
    public static ProductShippedStatus NotShipped => new() { IsShipped = false, Date = null };
}
