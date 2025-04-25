using Modular.Common;

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

    internal static Product Create(string sku, string name, string description, Price price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description cannot be empty.", nameof(description));
        }
        if (price <= 0)
        {
            throw new ArgumentException("Price cannot be less than or equal to zero.", nameof(price));
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("SKU cannot be empty.", nameof(sku));
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

    internal void DecreaseStock(uint quantity)
    {
        if (StockQuantity < quantity)
        {
            throw new InvalidOperationException("Insufficient stock quantity.");
        }

        StockQuantity -= quantity;
    }

    internal void ChangePrice(Price price)
    {
        if (price <= 0)
        {
            throw new ArgumentException("Price cannot be less than or equal to zero.", nameof(price));
        }

        Price = price;
    }

    internal void Change(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description cannot be empty.", nameof(description));
        }

        Name = name;
        Description = description;
    }
}
