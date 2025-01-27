using MediatR;
using Modular.Common;

namespace Modular.ProductIntegrationEvents;

public class ProductCreatedIntegrationEvent : INotification, IntegrationEvent
{
    public string Sku { get; init; }
}
