using ErrorOr;
using Modular.Common;
using Modular.Orders.Errors;

namespace Modular.Orders.UseCases.Common;

public class Product
{
    public int Id { get; private set; }
    public string SKU { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Price Price { get; private set; }
    public uint StockQuantity { get; private set; }

    private Product()
    {
    }

    internal static ErrorOr<Product> Create(string sku, string name, string description, Price price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return OrderErrors.InvalidProductName();
        }
        if (string.IsNullOrWhiteSpace(description))
        {
            return OrderErrors.InvalidProductDescription();
        }
        if (price <= 0)
        {
            return OrderErrors.InvalidProductPrice();
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            return OrderErrors.InvalidProductSku();
        }

        return new Product
        {
            SKU = sku,
            Name = name,
            Description = description,
            Price = price
        };
    }

    internal void IncreaseStock(uint quantity)
    {
        StockQuantity += quantity;
    }

    internal ErrorOr<Unit> DecreaseStock(uint quantity)
    {
        if (StockQuantity < quantity)
        {
            return OrderErrors.InsufficientStock(Id);
        }

        StockQuantity -= quantity;

        return Unit.Value;
    }

    internal ErrorOr<Unit> ChangePrice(Price price)
    {
        if (price <= 0)
        {
            return OrderErrors.InvalidProductPrice();
        }

        Price = price;

        return Unit.Value;
    }

    internal ErrorOr<Unit> Change(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return OrderErrors.InvalidProductName();
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return OrderErrors.InvalidProductDescription();
        }

        Name = name;
        Description = description;

        return Unit.Value;
    }
}
