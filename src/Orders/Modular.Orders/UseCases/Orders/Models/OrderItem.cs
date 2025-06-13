using Modular.Common;
using Modular.Orders.UseCases.Common;

namespace Modular.Orders.UseCases.Orders.Models;

public sealed class OrderItem
{
    public Guid OrderId { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public uint Quantity { get; set; }
    public Price Price { get; set; }
    public ProductShippedStatus ShippedStatus { get; private set; } = ProductShippedStatus.NotShipped;

    internal void IncreaseQuantity(uint quantity)
    {
        Quantity += quantity;
    }

    internal void DecreaseQuantity(uint quantity)
    {
        Quantity -= quantity;
    }

    internal void MarkAsShipped()
    {
        ShippedStatus = ProductShippedStatus.Shipped;
    }
}
