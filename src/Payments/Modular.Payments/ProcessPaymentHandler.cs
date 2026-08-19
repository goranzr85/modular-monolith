using Modular.Common.Events;
using Modular.Orders.Integrations;
using Modular.Payments.IntegrationEvents;

namespace Modular.Payments;

public class ProcessPaymentHandler : IIntegrationEventConsumer<ProcessPayment>
{
    private readonly IIntegrationEventPublisher _publisher;

    public ProcessPaymentHandler(IIntegrationEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task ConsumeAsync(ProcessPayment message, CancellationToken cancellationToken)
    {
        // process payment logic here

        await _publisher.PublishAsync(new PaymentProcessedIntegrationEvent(message.OrderId), cancellationToken);
    }
}
