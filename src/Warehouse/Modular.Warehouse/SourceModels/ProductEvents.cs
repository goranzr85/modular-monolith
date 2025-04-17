namespace Modular.Warehouse.SourceModels;

public record class ProductCreated(string Sku, string Name, DateTimeOffset OccuredOnUtc);

public sealed record ProductReceived(string Sku, uint Quantity, DateTimeOffset OccuredOnUtc);

public sealed record ProductShipped(string Sku, uint Quantity, DateTimeOffset OccuredOnUtc);

public sealed record IncreasedProductQuantity(string Sku, uint Quantity, string Reason, DateTimeOffset OccuredOnUtc);

public sealed record DecreasedProductQuantity(string Sku, uint Quantity, string Reason, DateTimeOffset OccuredOnUtc);

public sealed record ProductDelisted(string Sku, DateTimeOffset OccuredOnUtc);
