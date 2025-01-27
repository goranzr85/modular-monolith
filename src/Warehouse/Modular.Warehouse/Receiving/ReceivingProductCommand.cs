using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Warehouse.Errors;

namespace Modular.Warehouse.Receiving;

internal sealed record ReceivingProductCommand(string Sku, uint Quantity) : IRequest<ErrorOr<Unit>>
{
}

internal sealed class ReceivingProductCommandHandler : IRequestHandler<ReceivingProductCommand, ErrorOr<Unit>>
{
    private readonly WarehouseDbContext _warehouseDbContext;
    private readonly ILogger<ReceivingProductCommandHandler> _logger;

    public ReceivingProductCommandHandler(WarehouseDbContext warehouseDbContext, ILogger<ReceivingProductCommandHandler> logger)
    {
        _warehouseDbContext = warehouseDbContext;
        _logger = logger;
    }

    public async Task<ErrorOr<Unit>> Handle(ReceivingProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await _warehouseDbContext.Products
                   .FirstOrDefaultAsync(p => p.Sku == request.Sku, cancellationToken);

        if (product is null)
        {
            return ProductErrors.ProductNotFound(request.Sku);
        }

        _logger.LogDebug("Increased quantity of product {Sku} for {Quantity} pieces.", request.Sku, request.Quantity);

        product.IncreaseQuantity(request.Quantity);

        _warehouseDbContext.Update(product);
        await _warehouseDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Increased quantity of product {Sku} for {Quantity} pieces.", request.Sku, request.Quantity);

        return Unit.Value;
    }
}