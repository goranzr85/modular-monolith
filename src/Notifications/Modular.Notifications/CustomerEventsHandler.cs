using MassTransit;
using Modular.Customers.IntegrationEvents;

namespace Modular.Notifications;
internal sealed class CustomerEventsHandler : IConsumer<CustomerCreatedEvent>,
    IConsumer<CustomerChangedNameEvent>,
    IConsumer<CustomerChangedContactInformationEvent>
{
    public Task Consume(ConsumeContext<CustomerCreatedEvent> context)
    {
        throw new NotImplementedException();
    }

    public Task Consume(ConsumeContext<CustomerChangedNameEvent> context)
    {
        throw new NotImplementedException();
    }

    public Task Consume(ConsumeContext<CustomerChangedContactInformationEvent> context)
    {
        throw new NotImplementedException();
    }
}
