using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Orders.Errors;
using Modular.Orders.UseCases.Orders.Models;

namespace Modular.Orders.UseCases.Orders.Submitted;

internal sealed record OrderSubmitCommand(Guid OrderId) : IRequest<ErrorOr<Unit>>;

internal sealed class OrderSubmitCommandHandler : IRequestHandler<OrderSubmitCommand, ErrorOr<Unit>>
{
    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<OrderSubmitCommandHandler> _logger;

    public OrderSubmitCommandHandler(OrderDbContext orderDbContext, ILogger<OrderSubmitCommandHandler> logger)
    {
        _orderDbContext = orderDbContext;
        _logger = logger;
    }

    public async Task<ErrorOr<Unit>> Handle(OrderSubmitCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Submitting order {OrderId}.", request.OrderId);

        Order? order = await _orderDbContext.Orders
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            return OrderErrors.OrderNotFound(request.OrderId);
        }

        ErrorOr<Unit> result = order.Submit();

        if (result.IsError)
        {
            _logger.LogError("Failed to submit order {OrderId}: {Error}", request.OrderId, result.Errors);
            return result.Errors;
        }

        _orderDbContext.Update(order);
        await _orderDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} submitted.", request.OrderId);

        return Unit.Value;
    }

}