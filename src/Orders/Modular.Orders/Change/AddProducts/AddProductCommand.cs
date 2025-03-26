using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modular.Common;
using Modular.Orders.Errors;
using Modular.Orders.Models;

namespace Modular.Orders.Change.AddProducts;
internal sealed record AddProductCommand(Guid OrderId, int ProductId, uint Quantity, Price Price) : IRequest<ErrorOr<Unit>>;

internal sealed class AddProductCommandHandler : IRequestHandler<AddProductCommand, ErrorOr<Unit>>
{
    private readonly OrderDbContext _orderDbContext;
    
    public AddProductCommandHandler(OrderDbContext orderDbContext)
    {
        _orderDbContext = orderDbContext;
    }

    public async Task<ErrorOr<Unit>> Handle(AddProductCommand request, CancellationToken cancellationToken)
    {
        Order? order = await _orderDbContext.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken);
        
        if (order is null)
        {
            return OrderErrors.OrderNotFound(request.OrderId);
        }

        Product? product = await _orderDbContext.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            return OrderErrors.ProductNotFound(request.ProductId);
        }

        order.AddItem(request.ProductId, request.Quantity, request.Price);

        _orderDbContext.Update(order);
        await _orderDbContext.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}

