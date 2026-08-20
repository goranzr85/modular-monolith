using Microsoft.Extensions.Logging;
using Modular.Common;
using Stripe;

namespace Modular.Payments;

// Demo-only rule: ProcessPayment carries no real card/payment-method (there's no checkout UI collecting
// one), so there's nothing for a real provider to react to. To still exercise both outcomes end to end,
// orders at or above DeclineThreshold are charged with Stripe's documented always-declining test
// PaymentMethod; everything else uses the always-succeeding one. See docs.stripe.com/testing#cards.
internal sealed class StripePaymentGatewayClient : IPaymentGatewayClient
{
    internal const decimal DeclineThreshold = 1000m;
    private const string SucceedingTestPaymentMethod = "pm_card_visa";
    private const string DecliningTestPaymentMethod = "pm_card_visa_chargeDeclined";

    private readonly PaymentIntentService _paymentIntentService;
    private readonly ILogger<StripePaymentGatewayClient> _logger;

    public StripePaymentGatewayClient(PaymentIntentService paymentIntentService, ILogger<StripePaymentGatewayClient> logger)
    {
        _paymentIntentService = paymentIntentService;
        _logger = logger;
    }

    public async Task<PaymentChargeResult> ChargeAsync(Guid orderId, Modular.Common.Price amount, CancellationToken cancellationToken)
    {
        string paymentMethod = amount.Value >= DeclineThreshold ? DecliningTestPaymentMethod : SucceedingTestPaymentMethod;

        PaymentIntentCreateOptions options = new()
        {
            Amount = ToMinorUnits(amount.Value),
            Currency = "usd",
            PaymentMethod = paymentMethod,
            PaymentMethodTypes = ["card"],
            Confirm = true,
            Metadata = new Dictionary<string, string> { ["order_id"] = orderId.ToString() }
        };

        try
        {
            PaymentIntent paymentIntent = await _paymentIntentService.CreateAsync(options, cancellationToken: cancellationToken);
            return PaymentChargeResult.Success(paymentIntent.Id);
        }
        catch (StripeException ex) when (ex.StripeError?.Type == "card_error")
        {
            _logger.LogWarning(ex, "Stripe declined payment for order {OrderId}: {ErrorCode} ({DeclineCode}).",
                orderId, ex.StripeError.Code, ex.StripeError.DeclineCode);
            return PaymentChargeResult.Failure(ex.StripeError.Code, ex.StripeError.DeclineCode);
        }
    }

    private static long ToMinorUnits(decimal amount) => (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
}
