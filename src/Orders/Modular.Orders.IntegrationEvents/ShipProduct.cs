using Modular.Common.Events;

namespace Modular.Orders.Integrations;
public sealed record ShipProduct(Guid OrderId, (string ProductSku, uint Quantity)[] Products) : IIntegrationCommand;
