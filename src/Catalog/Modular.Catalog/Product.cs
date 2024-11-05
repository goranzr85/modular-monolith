using ErrorOr;
using Modular.Catalog.Errors;

namespace Modular.Catalog;

public sealed class Product
{
    public Guid Id { get; private init; }
    public string Sku { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Price Price { get; private set; }

    private Product(Guid id, string sku, string name, string description, Price price)
    {
        Id = id;
        Sku = sku;
        Name = name;
        Description = description;
        Price = price;
    }

    internal static ErrorOr<Product> Create(string sku, string name, string description, Price price)
    {
        if (string.IsNullOrEmpty(sku))
        {
            return ProductErrors.InvalidSku();
        }

        if (string.IsNullOrEmpty(name))
        {
            return ProductErrors.InvalidName();
        }

        if (string.IsNullOrEmpty(description))
        {
            return ProductErrors.InvalidDescription();
        }

        if (price is null)
        {
            return ProductErrors.InvalidPrice();
        }

        return new Product(Ulid.NewUlid().ToGuid(), sku, name, description, price);
    }

    internal void Change(string sku, string name, string description, Price price)
    {
        Sku = sku;
        Name = name;
        Description = description;
        Price = price;
    }
}
