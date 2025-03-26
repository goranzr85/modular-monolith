using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modular.Orders.Errors;
using Modular.Orders.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modular.Orders.Change.ChangeProductQuantity;
internal sealed record DecreaseProductQuantityCommand(Guid OrderId, int ProductId, uint Quantity) : IRequest<ErrorOr<Unit>>
{
}

internal sealed class DecreaseProductQuantityCommandHandler : IRequestHandler<DecreaseProductQuantityCommand, ErrorOr<Unit>>
{
    private readonly OrderDbContext _orderDbContext;

    public DecreaseProductQuantityCommandHandler(OrderDbContext orderDbContext)
    {
        _orderDbContext = orderDbContext;
    }

    public async Task<ErrorOr<Unit>> Handle(DecreaseProductQuantityCommand request, CancellationToken cancellationToken)
    {
        Order? order = await _orderDbContext.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            return OrderErrors.OrderNotFound(request.OrderId);
        }

        OrderItem? orderItem = order.Items.FirstOrDefault(x => x.ProductId == request.ProductId);

        if(orderItem is null)
        {
            return OrderErrors.ProductIsNotPlaced(order.Id, request.ProductId);
        }

        if(orderItem.Quantity < request.Quantity)
        {
            return OrderErrors.ProductQuantityIsNotEnough(order.Id, request.ProductId, orderItem.Quantity);
        }


        return Unit.Value;
    }
}