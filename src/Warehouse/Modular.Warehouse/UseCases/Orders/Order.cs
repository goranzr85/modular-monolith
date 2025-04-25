namespace Modular.Warehouse.UseCases.Orders;
public sealed class Order
{
    internal Guid Id { get; set; }
    internal OrderItem[] Items { get; set; }

}

public sealed class OrderItem
{
    internal string ProductSku { get; set; }
    internal Guid OrderId { get; set; }
    internal uint Quantity { get; set; }
}