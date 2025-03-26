using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modular.Orders.Errors;
using Modular.Orders.Models;

namespace Modular.Orders.Change.RemoveProducts;
internal sealed record RemoveProductCommand(Guid OrderId, int ProductId) : IRequest<ErrorOr<Unit>>;

internal sealed class RemoveProductCommandHandler : IRequestHandler<RemoveProductCommand, ErrorOr<Unit>>
{
    private readonly OrderDbContext _orderDbContext;

    public RemoveProductCommandHandler(OrderDbContext orderDbContext)
    {
        _orderDbContext = orderDbContext;
    }

    public async Task<ErrorOr<Unit>> Handle(RemoveProductCommand request, CancellationToken cancellationToken)
    {
        Order? order = await _orderDbContext.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            return OrderErrors.OrderNotFound(request.OrderId);
        }

        order.RemoveItem(request.ProductId);

        _orderDbContext.Update(order);
        await _orderDbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

