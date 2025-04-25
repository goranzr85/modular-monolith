using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Modular.Orders.Errors;
using Modular.Orders.UseCases.Orders.Models;

namespace Modular.Orders.UseCases.Orders.Change.ChangeProductQuantity.Decrease;

internal sealed record DecreaseProductQuantityCommand(Guid OrderId, int ProductId, uint Quantity) : IRequest<ErrorOr<Unit>>;

internal sealed class DecreaseProductQuantityCommandHandler : IRequestHandler<DecreaseProductQuantityCommand, ErrorOr<Unit>>
{
    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<DecreaseProductQuantityCommandHandler> _logger;

    public DecreaseProductQuantityCommandHandler(OrderDbContext orderDbContext, ILogger<DecreaseProductQuantityCommandHandler> logger)
    {
        _orderDbContext = orderDbContext;
        _logger = logger;
    }

    public async Task<ErrorOr<Unit>> Handle(DecreaseProductQuantityCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Decreasing product quantity for order {OrderId} and product {ProductId} by {Quantity}.",
            request.OrderId, request.ProductId, request.Quantity);

        Order? order = await _orderDbContext.Orders
            .Include(x => x.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            return OrderErrors.OrderNotFound(request.OrderId);
        }

        OrderItem? orderItem = order.Items.FirstOrDefault(x => x.ProductId == request.ProductId);

        if (orderItem is null)
        {
            return OrderErrors.ProductIsNotPlaced(order.Id, request.ProductId);
        }

        if (orderItem.Quantity < request.Quantity)
        {
            return OrderErrors.ProductQuantityIsNotEnoughForDecrease(order.Id, request.ProductId, orderItem.Quantity);
        }

        using var transaction = await _orderDbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            order.DecreaseQuantity(request.ProductId, request.Quantity);
            _orderDbContext.Update(order);
            await _orderDbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Product quantity for order {OrderId} and product {ProductId} decreased by {Quantity}.",
                 request.OrderId, request.ProductId, request.Quantity);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(ex, "An error occurred while decreasing product quantity for order {OrderId} and product {ProductId} by {Quantity}.",
                request.OrderId, request.ProductId, request.Quantity);

            return OrderErrors.DecreaseProductQuantityError(order.Id, request.ProductId, request.Quantity);
        }
    }
}