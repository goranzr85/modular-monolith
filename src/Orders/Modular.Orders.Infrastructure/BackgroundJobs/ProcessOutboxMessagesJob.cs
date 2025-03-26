using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Common;
using Modular.Orders;
using Newtonsoft.Json;
using Quartz;

namespace Modular.Orders.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class ProcessOutboxMessagesJob : IJob
{
    // TODO: add polly to try again 
    // TODO: use masstransit and rabbitmq for publishing events

    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<ProcessOutboxMessagesJob> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public ProcessOutboxMessagesJob(OrderDbContext orderDbContext, ILogger<ProcessOutboxMessagesJob> logger,
        IPublishEndpoint publishEndpoint)
    {
        _orderDbContext = orderDbContext;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }


    public async Task Execute(IJobExecutionContext context)
    {
        List<OutboxMessage> outboxMessages = await _orderDbContext.OutboxMessages
             .Where(m => m.ProcessedOnUtc == null)
             .Take(20)
             .ToListAsync();

        foreach (OutboxMessage? outboxMessage in outboxMessages)
        {
            var domainEvent = JsonConvert.DeserializeObject(outboxMessage.Content, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });

            if (domainEvent is null)
            {
                _logger.LogError("Failed to deserialize domain event: {@OutboxMessage}", outboxMessage);
                continue;
            }

            await _publishEndpoint.Publish(domainEvent);

            outboxMessage!.ProcessedOnUtc = DateTime.UtcNow;
        }

        await _orderDbContext.SaveChangesAsync();
    }
}
