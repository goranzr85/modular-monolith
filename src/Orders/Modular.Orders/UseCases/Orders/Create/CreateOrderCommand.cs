using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Orders.Errors;
using Modular.Orders.UseCases.Orders.Models;

namespace Modular.Orders.Create;

internal sealed record CreateOrderCommand(Guid OrderId, DateTimeOffset OrderDate, Guid CustomerId, List<OrderItem> Items) : IRequest<ErrorOr<Guid>>;

internal sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, ErrorOr<Guid>>
{
    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(OrderDbContext orderDbContext, ILogger<CreateOrderCommandHandler> logger)
    {
        _orderDbContext = orderDbContext;
        _logger = logger;
    }

    public async Task<ErrorOr<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        Order? order = await _orderDbContext.Orders
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken);

        if (order is not null)
        {
            return OrderErrors.OrderAlreadyCreated(order.Id);
        }

        _logger.LogInformation("Creating order {OrderId}.", request.OrderId);

        order = Order.Create(request.OrderId, request.OrderDate, request.CustomerId, request.Items);

        _orderDbContext.Add(order);
        await _orderDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} created.", request.OrderId);

        return order.Id;
    }

}