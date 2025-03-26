using Modular.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modular.Orders.DomainEvents;
public sealed record OrderSubmitted(Guid OrderId, Guid CustomerId, Price TotalAmount) : IDomainEvent;
