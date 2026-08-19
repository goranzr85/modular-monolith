using ErrorOr;
using Modular.Common;
using Modular.Common.Events;

namespace Modular.Notifications.Infrastructure.NotificationSenders;

public interface INotificationSender
{
    public Task<ErrorOr<Unit>> SendAsync<TNotification>(TNotification notification)
        where TNotification : IIntegrationEvent;
}