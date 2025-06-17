using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modular.Notifications.Infrastructure.NotificationSenders;
using Modular.Orders.Integrations;
using Newtonsoft.Json;
using Quartz;

namespace Modular.Notifications.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class ProcessInboxMessagesJob : IJob
{
    private readonly NotificationDbContext _notificationDbContext;
    private readonly INotificationSender _notificationSender;
    private readonly ILogger<ProcessInboxMessagesJob> _logger;

    public ProcessInboxMessagesJob(NotificationDbContext notificationDbContext,
        [FromKeyedServices(NotificationSenderFactory.Key)] INotificationSender notificationSender,
        ILogger<ProcessInboxMessagesJob> logger)
    {
        _notificationDbContext = notificationDbContext;
        _notificationSender = notificationSender;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        List<InboxMessage> outboxMessages = await _notificationDbContext.InboxMessages
             .Where(m => m.ProcessedAt == null)
             .Take(20)
             .ToListAsync();

        foreach (InboxMessage? inboxMessage in outboxMessages)
        {
            object? domainEvent = JsonConvert.DeserializeObject(inboxMessage.Payload, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });

            if (domainEvent is null)
            {
                _logger.LogError("Failed to deserialize domain event: {@InboxMessage}", inboxMessage);
                continue;
            }

            if (domainEvent is not OrderShippedIntegrationEvent orderShippedEvent)
            {
                _logger.LogError("Invalid domain event type: {@InboxMessage}", inboxMessage);
                continue;
            }

            ErrorOr.ErrorOr<MediatR.Unit> sendResult = await _notificationSender.SendAsync(orderShippedEvent);

            if (sendResult.IsError)
            {
                _logger.LogError("Failed to send notification for message {@InboxMessage}: {Error}", inboxMessage, sendResult.Errors);
                continue;
            }

            inboxMessage!.ProcessedAt = DateTime.UtcNow;
        }

        await _notificationDbContext.SaveChangesAsync();
    }
}
