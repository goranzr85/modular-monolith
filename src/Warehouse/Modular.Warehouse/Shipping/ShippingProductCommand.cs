using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Warehouse.Errors;

namespace Modular.Warehouse.Shipping;

internal sealed record ShippingProductCommand(Guid ProductId, uint Quantity) : IRequest<ErrorOr<Unit>>
{
}

internal sealed class ShippingProductCommandHandler : IRequestHandler<ShippingProductCommand, ErrorOr<Unit>>
{
    private readonly WarehouseDbContext _warehouseDbContext;
    private readonly ILogger<ShippingProductCommandHandler> _logger;

    public ShippingProductCommandHandler(WarehouseDbContext warehouseDbContext, ILogger<ShippingProductCommandHandler> logger)
    {
        _warehouseDbContext = warehouseDbContext;
        _logger = logger;
    }

    public async Task<ErrorOr<Unit>> Handle(ShippingProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await _warehouseDbContext.Products
                   .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            return ProductErrors.ProductNotFound(request.ProductId);
        }

        _logger.LogDebug("Shipping {Quantity} pieces of product {ProductId}.", request.Quantity, request.ProductId);

        var result = product.DecreaseQuantity(request.Quantity);

        if (result.IsError)
        {
            return result.FirstError;
        }

        _warehouseDbContext.Update(product);
        await _warehouseDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Shipping {Quantity} pieces of product {ProductId} succeeded.", request.Quantity, request.ProductId);

        return Unit.Value;
    }
}