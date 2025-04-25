using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Orders.Errors;
using Modular.Orders.UseCases.Orders.Models;

namespace Modular.Orders.UseCases.Orders.Cancel;

internal sealed record CancelOrderCommand(Guid OrderId) : IRequest<ErrorOr<Unit>>;

internal sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, ErrorOr<Unit>>
{
    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<CancelOrderCommandHandler> _logger;

    public CancelOrderCommandHandler(OrderDbContext orderDbContext, ILogger<CancelOrderCommandHandler> logger)
    {
        _orderDbContext = orderDbContext;
        _logger = logger;
    }

    public async Task<ErrorOr<Unit>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling order {OrderId}.", request.OrderId);

        Order? order = await _orderDbContext.Orders
            .SingleOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            return OrderErrors.OrderNotFound(request.OrderId);
        }

        ErrorOr<Unit> errorOr = order.Cancel();

        if (errorOr.IsError)
        {
            return errorOr.FirstError;
        }

        _orderDbContext.Update(order);
        await _orderDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} cancelled.", request.OrderId);

        return Unit.Value;
    }

}