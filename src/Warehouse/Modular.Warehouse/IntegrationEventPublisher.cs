using Marten;
using Marten.Events.Daemon;
using Marten.Events.Daemon.Internals;
using Marten.Subscriptions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
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


    //private readonly IPublishEndpoint _publishEndpoint;

    //public IntegrationEventPublisher(IPublishEndpoint publishEndpoint)
    //{
    //    _publishEndpoint = publishEndpoint;
    //}

    public override async Task<IChangeListener> ProcessEventsAsync(EventRange page, ISubscriptionController controller,
        IDocumentOperations operations, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

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
                await publishEndpoint.Publish(message, cancellationToken);
            }
        }

        return null!;
    }
}
