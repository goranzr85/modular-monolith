using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modular.Orders.Integrations;

namespace Modular.Notifications.Infrastructure.NotificationSenders;
internal sealed class NotificationSenderFactory : INotificationSender
{
    internal const string Key = nameof(SmsNotificationsSender);

    private readonly INotificationSender _emailNotificationsSender;
    private readonly INotificationSender _smsNotificationsSender;
    private readonly NotificationDbContext _notificationDbContext;

    public NotificationSenderFactory([FromKeyedServices(EmailNotificationsSender.Key)] INotificationSender emailNotificationsSender,
        [FromKeyedServices(SmsNotificationsSender.Key)] INotificationSender smsNotificationsSender,
        NotificationDbContext notificationDbContext)
    {
        _emailNotificationsSender = emailNotificationsSender;
        _smsNotificationsSender = smsNotificationsSender;
        _notificationDbContext = notificationDbContext;
    }

    async Task<ErrorOr<Unit>> INotificationSender.SendAsync<TNotification>(TNotification notification)
    {
        OrderShippedIntegrationEvent? orderShippedEvent = notification as OrderShippedIntegrationEvent;

        if (orderShippedEvent is null)
        {
            return Error.Failure("Invalid notification type");
        }

        Common.User.PrimaryContactType primaryContactType = await _notificationDbContext.Customers
               .Where(c => c.Id == orderShippedEvent!.CustomerId)
               .Select(c => c!.Contact.PrimaryContactType)
               .FirstOrDefaultAsync();

        ErrorOr<Unit> result = primaryContactType switch
        {
            Common.User.PrimaryContactType.Email => await _emailNotificationsSender.SendAsync(notification),
            Common.User.PrimaryContactType.Phone => await _smsNotificationsSender.SendAsync(notification),
            _ => Error.Failure("Unsupported primary contact type")
        };

        return result;
    }
}
