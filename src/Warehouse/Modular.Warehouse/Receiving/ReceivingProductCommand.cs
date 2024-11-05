using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Warehouse.Errors;

namespace Modular.Warehouse.Receiving;

internal sealed record ReceivingProductCommand(Guid ProductId, uint Quantity) : IRequest<ErrorOr<Unit>>
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
                   .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            return ProductErrors.ProductNotFound(request.ProductId);
        }

        _logger.LogDebug("Increased quantity of product {ProductId} for {Quantity} pieces.", request.ProductId, request.Quantity);

        product.IncreaseQuantity(request.Quantity);

        _warehouseDbContext.Update(product);
        await _warehouseDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Increased quantity of product {ProductId} for {Quantity} pieces succeeded.", request.ProductId, request.Quantity);

        return Unit.Value;
    }
}