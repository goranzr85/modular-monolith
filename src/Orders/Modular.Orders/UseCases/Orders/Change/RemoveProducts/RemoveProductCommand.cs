using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Orders.Errors;
using Modular.Orders.UseCases.Orders.Models;

namespace Modular.Orders.UseCases.Orders.Change.RemoveProducts;
internal sealed record RemoveProductCommand(Guid OrderId, int ProductId) : IRequest<ErrorOr<Unit>>;

internal sealed class RemoveProductCommandHandler : IRequestHandler<RemoveProductCommand, ErrorOr<Unit>>
{
    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<RemoveProductCommandHandler> _logger;

    public RemoveProductCommandHandler(OrderDbContext orderDbContext, ILogger<RemoveProductCommandHandler> logger)
    {
        _orderDbContext = orderDbContext;
        _logger = logger;
    }

    public async Task<ErrorOr<Unit>> Handle(RemoveProductCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Removing product from order {OrderId} and product {ProductId}.",
            request.OrderId, request.ProductId);

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

        _logger.LogInformation("Product {ProductId} removed from order {OrderId}.", request.ProductId, request.OrderId);

        return Unit.Value;
    }
}

