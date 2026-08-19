using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Marten.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modular.Common.Events;
using Modular.Warehouse.IntegrationEvents;
using Modular.Warehouse.SourceModels;

namespace Modular.Warehouse;
internal sealed class IntegrationEventPublisher : SubscriptionBase
{
    private readonly IServiceProvider _serviceProvider;

    public IntegrationEventPublisher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override async Task<IChangeListener> ProcessEventsAsync(EventRange page, ISubscriptionController controller,
        IDocumentOperations operations, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IntegrationEventPublisher>>();

        foreach (var @event in page.Events)
        {
            IIntegrationEvent? message = @event.Data switch
            {
                DecreasedProductQuantity decreasedProductQuantity => new ProductQuantityDecreasedInWarehouseIntegrationEvent
                {
                    Sku = decreasedProductQuantity.Sku,
                    Quantity = decreasedProductQuantity.Quantity,
                    OccuredOnUtc = decreasedProductQuantity.OccuredOnUtc
                },
                IncreasedProductQuantity increasedProductQuantity => new ProductQuantityIncreasedInWarehouseIntegrationEvent
                {
                    Sku = increasedProductQuantity.Sku,
                    Quantity = increasedProductQuantity.Quantity,
                    OccuredOnUtc = increasedProductQuantity.OccuredOnUtc
                },
                ProductReceived productReceived => new ProductQuantityIncreasedInWarehouseIntegrationEvent
                {
                    Sku = productReceived.Sku,
                    Quantity = productReceived.Quantity,
                    OccuredOnUtc = productReceived.OccuredOnUtc
                },
                ProductShipped productShipped => new ProductShippedIntegrationEvent
                {
                    Sku = productShipped.Sku,
                    Quantity = productShipped.Quantity,
                    OccuredOnUtc = productShipped.OccuredOnUtc
                },
                _ => null
            };

            if (message is not null)
            {
                await publisher.PublishAsync(message, cancellationToken);
            }
            else
            {
                logger.LogWarning("No integration event mapping found for domain event {EventType}; nothing was published.", @event.Data.GetType().Name);
            }
        }

        return null!;
    }
}
