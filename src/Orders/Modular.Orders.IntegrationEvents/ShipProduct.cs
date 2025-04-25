using Modular.Common;

namespace Modular.Orders.IntegrationEvents;
public sealed record ShipProduct(Guid OrderId, (string ProductSku, uint Quantity)[] Products) : IIntegrationCommand;
