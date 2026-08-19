using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Common;
using Modular.Common.Events;
using Modular.Orders;
using Newtonsoft.Json;
using Polly.Registry;
using Quartz;

namespace Modular.Orders.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class ProcessOutboxMessagesJob : IJob
{
    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<ProcessOutboxMessagesJob> _logger;
    private readonly IIntegrationEventPublisher _publisher;
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;

    public ProcessOutboxMessagesJob(OrderDbContext orderDbContext, ILogger<ProcessOutboxMessagesJob> logger,
        IIntegrationEventPublisher publisher, ResiliencePipelineProvider<string> pipelineProvider)
    {
        _orderDbContext = orderDbContext;
        _logger = logger;
        _publisher = publisher;
        _pipelineProvider = pipelineProvider;
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

            var pipeline = _pipelineProvider.GetPipeline(Constants.ResiliencePipelineName);

            await pipeline.ExecuteAsync(async ct =>
            {
                await _publisher.PublishAsync(domainEvent, ct);
            });

            outboxMessage!.ProcessedOnUtc = DateTime.UtcNow;
        }

        await _orderDbContext.SaveChangesAsync();
    }
}
