using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Modular.Common.Messaging;

// Central ActivitySource/Meter for the hand-rolled RabbitMQ messaging pipeline (publisher + consumer host +
// outbox/inbox jobs). Registered by name in modular-monolith.ServiceDefaults so every service that references
// this project gets these traces/metrics exported without a project reference back to ServiceDefaults.
public static class RabbitMqTelemetry
{
    public const string Name = "Modular.Common.Messaging";

    public static readonly ActivitySource ActivitySource = new(Name);

    private static readonly Meter Meter = new(Name);

    public static readonly Counter<long> MessagesPublished =
        Meter.CreateCounter<long>("messaging.rabbitmq.published", description: "Integration events published to RabbitMQ.");

    public static readonly Counter<long> MessagesConsumed =
        Meter.CreateCounter<long>("messaging.rabbitmq.consumed", description: "Integration events successfully handled by a consumer.");

    public static readonly Counter<long> MessagesDeadLettered =
        Meter.CreateCounter<long>("messaging.rabbitmq.dead_lettered", description: "Messages nacked to a dead-letter queue.");

    public static readonly Histogram<double> ConsumeDuration =
        Meter.CreateHistogram<double>("messaging.rabbitmq.consume.duration", unit: "ms", description: "Time to handle a delivered message, including retries.");

    // Shared by every module's outbox/inbox Quartz job: starts the batch-processing span (covering the
    // DB query for pending messages, before the batch size is known) and reports the resulting size and
    // outcome consistently, so the convention only needs to change in one place.
    public static Activity? StartBatchActivity(string name) =>
        ActivitySource.StartActivity(name, ActivityKind.Internal);

    public static void SetBatchSize(this Activity? activity, int messageCount) =>
        activity?.SetTag("messaging.batch.message_count", messageCount);

    public static void LogBatchProcessed(this ILogger logger, int messageCount, string messageKind)
    {
        if (messageCount > 0)
        {
            logger.LogInformation("Processed {Count} {MessageKind} messages.", messageCount, messageKind);
        }
    }

    public static void RecordException(this Activity? activity, Exception exception)
    {
        if (activity is null)
        {
            return;
        }

        var tags = new ActivityTagsCollection
        {
            ["exception.type"] = exception.GetType().FullName,
            ["exception.message"] = exception.Message,
            ["exception.stacktrace"] = exception.ToString()
        };

        activity.AddEvent(new ActivityEvent("exception", tags: tags));
        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
    }
}
