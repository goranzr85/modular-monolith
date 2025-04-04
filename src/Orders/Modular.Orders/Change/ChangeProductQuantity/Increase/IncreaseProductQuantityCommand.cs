using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Modular.Orders.Errors;
using Modular.Orders.Models;

namespace Modular.Orders.Change.ChangeProductQuantity.Increase;

internal sealed record IncreaseProductQuantityCommand(Guid OrderId, int ProductId, uint Quantity) : IRequest<ErrorOr<Unit>>;

internal sealed class IncreaseProductQuantityCommandHandler : IRequestHandler<IncreaseProductQuantityCommand, ErrorOr<Unit>>
{
    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<IncreaseProductQuantityCommandHandler> _logger;

    public IncreaseProductQuantityCommandHandler(OrderDbContext orderDbContext, ILogger<IncreaseProductQuantityCommandHandler> logger)
    {
        _orderDbContext = orderDbContext;
        _logger = logger;
    }

    public async Task<ErrorOr<Unit>> Handle(IncreaseProductQuantityCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Increasing product quantity for order {OrderId} and product {ProductId} by {Quantity}.",
            request.OrderId, request.ProductId, request.Quantity);

        Order? order = await _orderDbContext.Orders
            .Include(x => x.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            return OrderErrors.OrderNotFound(request.OrderId);
        }

        OrderItem? orderItem = order.Items
            .FirstOrDefault(x => x.ProductId == request.ProductId);

        if (orderItem is null)
        {
            return OrderErrors.ProductIsNotPlaced(order.Id, request.ProductId);
        }

        using var transaction = await _orderDbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            var product = await _orderDbContext.Products
             .FromSqlRaw("SELECT StockQuantity FROM Products WHERE Id = {0} FOR UPDATE", request.ProductId)
             .Select(p => new { p.StockQuantity })
             .FirstOrDefaultAsync();

            if (product is null)
            {
                return OrderErrors.ProductNotFound(request.ProductId);
            }

            if (product.StockQuantity < request.Quantity)
            {
                return OrderErrors.ProductQuantityIsNotEnough(request.ProductId);
            }

            order.IncreaseQuantity(request.ProductId, request.Quantity);
            _orderDbContext.Update(order);
            await _orderDbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Product quantity for order {OrderId} and product {ProductId} increased by {Quantity}.",
                request.OrderId, request.ProductId, request.Quantity);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(ex, "An error occurred while increasing product quantity for order {OrderId} and product {ProductId} by {Quantity}.",
                request.OrderId, request.ProductId, request.Quantity);

            return OrderErrors.IncreaseProductQuantityError(order.Id, request.ProductId, request.Quantity);
        }
    }
}