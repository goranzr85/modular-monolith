using Modular.Orders.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modular.Orders.Create;
internal sealed record CreateOrderRequest(Guid CustomerId, List<OrderItem> Items);
