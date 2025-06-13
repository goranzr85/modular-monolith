using Modular.Common;

namespace Modular.Payments.IntegrationEvents;

public sealed record PaymentProcessedIntegrationEvent(Guid OrderId): IIntegrationEvent;
