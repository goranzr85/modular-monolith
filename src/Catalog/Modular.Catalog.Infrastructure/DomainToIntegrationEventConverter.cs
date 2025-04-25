using Modular.Catalog.IntegrationEvents;
using Modular.Catalog.UseCases.Create.DomainEvents;
using Modular.Common;

namespace Modular.Catalog.Infrastructure;
internal sealed class DomainToIntegrationEventConverter
{
    internal static IIntegrationEvent Convert(IDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            ProductCreatedEvent productCreated => new ProductCreatedIntegrationEvent(productCreated.Sku, productCreated.Name, productCreated.Description, productCreated.Price),
            _ => throw new InvalidOperationException($"Unknown domain event type: {domainEvent.GetType().Name}")
        };
    }

}
