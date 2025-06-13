using Modular.Common;

namespace Modular.Orders.Integrations;
public sealed class OrderShippedIntegrationEvent : IIntegrationEvent
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public DateOnly ShippedDate { get; set; }
    public (string ProductName, uint Quantity, Price Price)[] Products { get; set; }
    public Price TotalAmounts { get; set; }
}