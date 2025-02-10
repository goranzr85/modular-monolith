using ErrorOr;
using MediatR;
using Modular.Warehouse.Errors;

namespace Modular.Warehouse;

internal sealed class Product
{
    public string Sku { get; internal init; }
    public uint Quantity { get; private set; }

    internal void IncreaseQuantity(uint quantity)
    {
        Quantity += quantity;
    }

    internal ErrorOr<Unit> DecreaseQuantity(uint quantity)
    {
        if (Quantity < quantity)
        {
            return ProductErrors.NotEnoughQuantity();
        }

        Quantity -= quantity;

        return Unit.Value;
    }

    internal static Product Create(string sku)
    {
        return new Product
        {
            Sku = sku,
            Quantity = 0
        };
    }
}
