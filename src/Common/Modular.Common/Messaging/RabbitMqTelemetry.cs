using System.Diagnostics;
using System.Diagnostics.Metrics;

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
