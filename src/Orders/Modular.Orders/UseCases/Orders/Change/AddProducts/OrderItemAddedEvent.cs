﻿using Modular.Common.Events;

namespace Modular.Orders.UseCases.Orders.Change.AddProducts;
internal sealed record OrderItemAddedEvent(int ProductId, uint Quantity) : IDomainEvent;