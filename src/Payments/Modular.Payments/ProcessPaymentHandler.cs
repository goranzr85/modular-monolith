using Microsoft.Extensions.Logging;
using Modular.Common.Events;
using Modular.Orders.Integrations;
using Modular.Payments.IntegrationEvents;

namespace Modular.Payments;

public class ProcessPaymentHandler : IIntegrationEventConsumer<ProcessPayment>
{
    private readonly IIntegrationEventPublisher _publisher;
    private readonly IPaymentGatewayClient _paymentGateway;
    private readonly ILogger<ProcessPaymentHandler> _logger;

    public ProcessPaymentHandler(IIntegrationEventPublisher publisher, IPaymentGatewayClient paymentGateway, ILogger<ProcessPaymentHandler> logger)
    {
        _publisher = publisher;
        _paymentGateway = paymentGateway;
        _logger = logger;
    }

    public async Task ConsumeAsync(ProcessPayment message, CancellationToken cancellationToken)
    {
        PaymentChargeResult result = await _paymentGateway.ChargeAsync(message.OrderId, message.TotalAmount, cancellationToken);

        if (result.Succeeded)
        {
            _logger.LogInformation("Payment succeeded for order {OrderId} (transaction {TransactionId}).",
                message.OrderId, result.TransactionId);

            await _publisher.PublishAsync(new PaymentProcessedIntegrationEvent(message.OrderId), cancellationToken);
        }
        else
        {
            _logger.LogWarning("Payment failed for order {OrderId}: {ErrorCode} ({DeclineCode}).",
                message.OrderId, result.ErrorCode, result.DeclineCode);

            await _publisher.PublishAsync(new PaymentFailedIntegrationEvent(message.OrderId, result.ErrorCode!, result.DeclineCode), cancellationToken);
        }
    }
}
