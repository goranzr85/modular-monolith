using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Modular.Common;
using Modular.Orders.Errors;
using Modular.Orders.Models;

namespace Modular.Orders.Change.AddProducts;
internal sealed record AddProductCommand(Guid OrderId, int ProductId, uint Quantity, Price Price) : IRequest<ErrorOr<Unit>>;

internal sealed class AddProductCommandHandler : IRequestHandler<AddProductCommand, ErrorOr<Unit>>
{
    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<AddProductCommandHandler> _logger;

    public AddProductCommandHandler(OrderDbContext orderDbContext, ILogger<AddProductCommandHandler> logger)
    {
        _orderDbContext = orderDbContext;
        _logger = logger;
    }

    public async Task<ErrorOr<Unit>> Handle(AddProductCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding product to order {OrderId} and product {ProductId} by {Quantity}.",
            request.OrderId, request.ProductId, request.Quantity);

        Order? order = await _orderDbContext.Orders
            .Include(x => x.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken);
        
        if (order is null)
        {
            return OrderErrors.OrderNotFound(request.OrderId);
        }

        using var transaction = await _orderDbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            var product = await _orderDbContext.Products
              .FromSqlRaw("SELECT StockQuantity FROM Products WHERE Id = {0} FOR UPDATE", request.ProductId)
              .Select(p => new { p.StockQuantity })
              .FirstOrDefaultAsync(cancellationToken);

            if (product is null)
            {
                return OrderErrors.ProductNotFound(request.ProductId);
            }

            if (product.StockQuantity < request.Quantity)
            {
                return OrderErrors.ProductQuantityIsNotEnough(request.ProductId);
            }

            order.AddItem(request.ProductId, request.Quantity, request.Price);

            _orderDbContext.Update(order);
            await _orderDbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Product added to order {OrderId} and product {ProductId} by {Quantity}.",
                request.OrderId, request.ProductId, request.Quantity);
            
            return Unit.Value;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            
            _logger.LogError(ex, "An error occurred while adding product to order {OrderId} and product {ProductId} by {Quantity}.",
                request.OrderId, request.ProductId, request.Quantity);
            
            return OrderErrors.AddItemToOrderError(order.Id, request.ProductId);
        }

    }
}

