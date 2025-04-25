namespace Modular.Warehouse.UseCases.Products.Shipping;
internal record class ProductShippingRequest(Guid OrderId, uint Quantity);
