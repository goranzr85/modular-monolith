using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modular.Orders.Errors;
using Modular.Orders.Models;

namespace Modular.Orders.Cancel;

internal sealed record CancelOrderCommand(Guid OrderId) : IRequest<ErrorOr<Unit>>;

internal sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, ErrorOr<Unit>>
{
    private readonly OrderDbContext _orderDbContext;

    public CancelOrderCommandHandler(OrderDbContext orderDbContext)
    {
        _orderDbContext = orderDbContext;
    }

    public async Task<ErrorOr<Unit>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        Order? order = await _orderDbContext.Orders.SingleOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            return OrderErrors.OrderNotFound(request.OrderId);
        }

        order.Cancel();
        
        _orderDbContext.Update(order);
        await _orderDbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

}