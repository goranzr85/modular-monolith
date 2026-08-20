using Modular.Common.Events;

namespace Modular.Payments.IntegrationEvents;

public sealed record PaymentFailedIntegrationEvent(Guid OrderId, string ErrorCode, string? DeclineCode) : IIntegrationEvent;
