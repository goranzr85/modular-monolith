using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modular.Orders.Errors;
using Modular.Orders.Models;

namespace Modular.Orders.Create;

internal sealed record CreateOrderCommand(Guid OrderId, DateTime OrderDate, Guid CustomerId, List<OrderItem> Items) : IRequest<ErrorOr<Unit>>;

internal sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, ErrorOr<Unit>>
{
    private readonly OrderDbContext _orderDbContext;

    public CreateOrderCommandHandler(OrderDbContext orderDbContext)
    {
        _orderDbContext = orderDbContext;
    }

    public async Task<ErrorOr<Unit>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        Order? order = await _orderDbContext.Orders.SingleOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken);

        if (order is not null)
        {
            return OrderErrors.OrderAlreadyCreated(order.Id);
        }

        order = Order.Create(request.OrderId, request.OrderDate, request.CustomerId, request.Items);

        _orderDbContext.Add(order);
        await _orderDbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

}