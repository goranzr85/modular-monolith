using Modular.Catalog.Create.DomainEvents;
using Modular.Catalog.IntegrationEvents;
using Modular.Common;

namespace Modular.Catalog.Infrastructure;
internal sealed class DomainToIntegrationEventConverter
{
    internal static IIntegrationEvent Convert(IDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            ProductCreated productCreated => new ProductCreatedIntegrationEvent(productCreated.Sku, productCreated.Name, productCreated.Description, productCreated.Price),
            _ => throw new InvalidOperationException($"Unknown domain event type: {domainEvent.GetType().Name}")
        };
    }

}
