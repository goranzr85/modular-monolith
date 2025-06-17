using ErrorOr;
using MediatR;
using Modular.Common.Events;

namespace Modular.Notifications.Infrastructure.NotificationSenders;

internal sealed class SmsNotificationsSender : INotificationSender
{
    internal const string Key = nameof(SmsNotificationsSender);

    public async Task<ErrorOr<Unit>> SendAsync<TNotification>(TNotification notification) where TNotification : IIntegrationEvent
    {
        await Task.Delay(100); // Simulate async operation
        // implement sms sending logic here
        return Unit.Value;
    }
}
