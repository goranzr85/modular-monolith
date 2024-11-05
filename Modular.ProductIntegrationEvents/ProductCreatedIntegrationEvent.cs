using MediatR;

namespace Modular.ProductIntegrationEvents;

public class ProductCreatedIntegrationEvent : INotification
{
    public Guid Id { get; internal set; }
    public string Sku { get; internal set; }
}
