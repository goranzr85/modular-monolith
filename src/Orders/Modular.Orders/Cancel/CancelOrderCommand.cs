//using MediatR;

//namespace Modular.Orders.Cancel;
//internal sealed record CancelOrderCommand(int OrderId);


//internal sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand>
//{
//    //private readonly IOrderRepository _orderRepository;
//    //public CancelOrderCommandHandler(IOrderRepository orderRepository)
//    //{
//    //    _orderRepository = orderRepository;
//    //}
//    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
//    {
//        //var order = await _orderRepository.GetByIdAsync(request.OrderId);
//        //if (order == null)
//        //{
//        //    throw new NotFoundException(nameof(Order), request.OrderId);
//        //}
//        //order.Cancel();
//        //await _orderRepository.UpdateAsync(order);
//        //return Unit.Value;
//    }

//}