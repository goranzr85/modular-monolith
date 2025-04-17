using ErrorOr;
using Marten.Schema;
using MediatR;
using Modular.Warehouse.Errors;
using Modular.Warehouse.SourceModels;

namespace Modular.Warehouse;

public sealed class Product
{
    [Identity]
    public string Sku { get; set; }
    public string Name { get; set; }
    public uint Quantity { get; set; }
    public bool IsDelisted { get; set; }

    public void Apply(ProductCreated productCreated)
    {
        Sku = productCreated.Sku;
        Name = productCreated.Name;
        Quantity = 0;
    }

    public void Apply(ProductReceived productReceived)
    {
        Quantity += productReceived.Quantity;
    }

    public void Apply(ProductShipped productShipped)
    {
        DecreaseQuantity(productShipped.Quantity);
    }

    public void Apply(ProductDelisted productDelisted)
    {
        IsDelisted = true;
    }

    public ErrorOr<Unit> DecreaseQuantity(uint quantity)
    {
        if (Quantity < quantity)
        {
            return ProductErrors.NotEnoughQuantity();
        }

        Quantity -= quantity;

        return Unit.Value;
    }

}
