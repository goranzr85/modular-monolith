using ErrorOr;
using Modular.Catalog.Errors;
using Modular.Common;
using Modular.ProductIntegrationEvents;

namespace Modular.Catalog;

public sealed class Product : AggregateRoot
{
    public string Sku { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Price Price { get; private set; }

    private Product(string sku, string name, string description, Price price)
    {
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

        var product = new Product(sku, name, description, price);

        product.RaiseEvent(new ProductCreatedIntegrationEvent
        {
            Sku = sku
        });

        return product;
    }

    internal void Change(string sku, string name, string description, Price price)
    {
        Sku = sku;
        Name = name;
        Description = description;
        Price = price;
    }
}
