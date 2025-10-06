using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Modular.Common.Events;
using Newtonsoft.Json;

namespace Modular.Common;

public sealed class EventsToOutboxMessagesInterceptors : SaveChangesInterceptor
{

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        DbContext? dbContext = eventData.Context;

        if (dbContext is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var outBoxMessages = dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(x => x.Entity)
            .SelectMany(x =>
            {
                IReadOnlyCollection<IDomainEvent> events = x.GetEvents();

                x.ClearEvents();

                return events;
            })
            .Select(@event => new OutboxMessage()
            {
                Id = Guid.NewGuid(),
                OccurredOnUtc = DateTime.UtcNow,
                Type = @event.GetType().Name,
                Content = JsonConvert.SerializeObject(@event,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                })
            })
            .ToList();

        dbContext.Set<OutboxMessage>().AddRange(outBoxMessages);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
