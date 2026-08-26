using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Common;
using Modular.Common.Events;
using Modular.Common.Messaging;
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
        using Activity? activity = RabbitMqTelemetry.StartBatchActivity("Customers outbox.process");

        var outboxMessages = await _customerDbContext.OutboxMessages
             .Where(m => m.ProcessedOnUtc == null)
             .Take(20)
             .ToListAsync();

        activity.SetBatchSize(outboxMessages.Count);

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
                await _publisher.PublishAsync(integrationEvent, outboxMessage.TraceParent, ct);
            });

            outboxMessage.ProcessedOnUtc = DateTime.UtcNow;
        }

        await _customerDbContext.SaveChangesAsync();

        _logger.LogBatchProcessed(outboxMessages.Count, "outbox");
    }
}
