using Modular.Common.Events;

namespace Modular.Payments.IntegrationEvents;

public sealed record PaymentProcessedIntegrationEvent(Guid OrderId): IIntegrationEvent;
