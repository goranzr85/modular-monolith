using Modular.Common;

namespace Modular.Orders.Change.AddProducts;
internal sealed record AddProductToOrderRequest(int ProductId, uint Quantity, Price Price);
