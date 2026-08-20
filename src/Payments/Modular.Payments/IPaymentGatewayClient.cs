using Modular.Common;

namespace Modular.Payments;

public interface IPaymentGatewayClient
{
    Task<PaymentChargeResult> ChargeAsync(Guid orderId, Price amount, CancellationToken cancellationToken);
}

public sealed record PaymentChargeResult(bool Succeeded, string? TransactionId, string? ErrorCode, string? DeclineCode)
{
    public static PaymentChargeResult Success(string transactionId) => new(true, transactionId, null, null);

    public static PaymentChargeResult Failure(string errorCode, string? declineCode) => new(false, null, errorCode, declineCode);
}
