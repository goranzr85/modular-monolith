using Modular.Common;

namespace Modular.Orders;

internal sealed class OrderItem
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public uint Quantity { get; set; }
    public Price Price { get; set; }
}
