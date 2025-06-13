using MassTransit;
using Modular.Orders.Integrations;
using Modular.Payments.IntegrationEvents;

namespace Modular.Payments;

public class ProcessPaymentHandler : IConsumer<ProcessPayment>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public ProcessPaymentHandler(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<ProcessPayment> context)
    {
        // process payment logic here

        await _publishEndpoint.Publish(new PaymentProcessedIntegrationEvent(context.Message.OrderId));
    }
}
