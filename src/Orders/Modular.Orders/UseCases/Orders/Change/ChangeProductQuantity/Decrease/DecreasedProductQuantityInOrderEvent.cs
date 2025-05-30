﻿using Modular.Common;

namespace Modular.Orders.UseCases.Orders.Change.ChangeProductQuantity.Decrease;
internal sealed record DecreasedProductQuantityInOrderEvent(Guid OrderId, int ProductId, uint Quantity) : IDomainEvent;
