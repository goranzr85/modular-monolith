using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Common;
using Newtonsoft.Json;
using Quartz;
using System.Text.Json;

namespace Modular.Catalog.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class ProcessOutboxMessagesJob : IJob
{
    // TODO: add polly to try again 
    // TODO: use masstransit and rabbitmq for publishing events

    private readonly CatalogDbContext _catalogDbContext;
    private readonly IPublisher _publisher;
    private readonly ILogger<ProcessOutboxMessagesJob> _logger;

    public ProcessOutboxMessagesJob(CatalogDbContext catalogDbContext, IPublisher publisher, ILogger<ProcessOutboxMessagesJob> logger)
    {
        _catalogDbContext = catalogDbContext;
        _publisher = publisher;
        _logger = logger;
    }


    public async Task Execute(IJobExecutionContext context)
    {
        var outboxMessages = await _catalogDbContext.OutboxMessages
             .Where(m => m.ProcessedOnUtc == null)
             .Take(20)
             .ToListAsync();

        foreach (OutboxMessage? outboxMessage in outboxMessages)
        {
            IntegrationEvent? integrationEvent = JsonConvert.DeserializeObject<IntegrationEvent>(outboxMessage.Content,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

            if (integrationEvent is null)
            {
                _logger.LogError("Failed to deserialize integration event: {@OutboxMessage}", outboxMessage);
                continue;
            }

            await _publisher.Publish(integrationEvent);

            outboxMessage!.ProcessedOnUtc = DateTime.UtcNow;
        }

        await _catalogDbContext.SaveChangesAsync();
    }
}
