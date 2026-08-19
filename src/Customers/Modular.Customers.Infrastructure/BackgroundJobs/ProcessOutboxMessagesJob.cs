using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Common;
using Modular.Common.Events;
using Newtonsoft.Json;
using Polly.Registry;
using Quartz;

namespace Modular.Customers.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class ProcessOutboxMessagesJob : IJob
{
    private readonly CustomerDbContext _customerDbContext;
    private readonly ILogger<ProcessOutboxMessagesJob> _logger;
    private readonly IIntegrationEventPublisher _publisher;
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;

    public ProcessOutboxMessagesJob(CustomerDbContext customerDbContext, ILogger<ProcessOutboxMessagesJob> logger,
        IIntegrationEventPublisher publisher, ResiliencePipelineProvider<string> pipelineProvider)
    {
        _customerDbContext = customerDbContext;
        _logger = logger;
        _publisher = publisher;
        _pipelineProvider = pipelineProvider;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var outboxMessages = await _customerDbContext.OutboxMessages
             .Where(m => m.ProcessedOnUtc == null)
             .Take(20)
             .ToListAsync();

        foreach (OutboxMessage outboxMessage in outboxMessages)
        {
            var deserialized = JsonConvert.DeserializeObject(outboxMessage.Content, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });

            if (deserialized is not IIntegrationEvent integrationEvent)
            {
                _logger.LogError("Failed to deserialize integration event: {@OutboxMessage}", outboxMessage);
                continue;
            }

            var pipeline = _pipelineProvider.GetPipeline(Constants.ResiliencePipelineName);

            await pipeline.ExecuteAsync(async ct =>
            {
                await _publisher.PublishAsync(integrationEvent, ct);
            });

            outboxMessage.ProcessedOnUtc = DateTime.UtcNow;
        }

        await _customerDbContext.SaveChangesAsync();
    }
}
