using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Common;
using Newtonsoft.Json;
using Polly.Registry;
using Quartz;

namespace Modular.Catalog.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class ProcessOutboxMessagesJob : IJob
{
    private readonly CatalogDbContext _catalogDbContext;
    private readonly ILogger<ProcessOutboxMessagesJob> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;

    public ProcessOutboxMessagesJob(CatalogDbContext catalogDbContext, ILogger<ProcessOutboxMessagesJob> logger,
        IPublishEndpoint publishEndpoint, ResiliencePipelineProvider<string> pipelineProvider)
    {
        _catalogDbContext = catalogDbContext;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
        _pipelineProvider = pipelineProvider;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var outboxMessages = await _catalogDbContext.OutboxMessages
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
                _logger.LogError("Failed to deserialize integration event: {@OutboxMessage}", outboxMessage);
                continue;
            }

            var integrationEvent = DomainToIntegrationEventConverter.Convert((IDomainEvent)domainEvent);

            var pipeline = _pipelineProvider.GetPipeline(Constants.ResiliencePipelineName);

            await pipeline.ExecuteAsync(async ct =>
            {
                await _publishEndpoint.Publish(integrationEvent, CancellationToken.None);
            });

            outboxMessage!.ProcessedOnUtc = DateTime.UtcNow;
        }

        await _catalogDbContext.SaveChangesAsync();
    }
}
