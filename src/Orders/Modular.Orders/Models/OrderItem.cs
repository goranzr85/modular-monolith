using Modular.Common;

namespace Modular.Orders.Models;

public sealed class OrderItem
{
    public Guid OrderId { get; set; }
    public int ProductId { get; set; }
    public uint Quantity { get; set; }
    public Price Price { get; set; }

    internal void IncreaseQuantity(uint quantity)
    {
        Quantity += quantity;
    }

    internal void DecreaseQuantity(uint quantity)
    {
        Quantity -= quantity;
    }
}
