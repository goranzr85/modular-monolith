namespace Modular.Common;

public abstract class AggregateRoot
{
    private readonly List<IntegrationEvent> _integrationEvents = new();

    protected void RaiseEvent(IntegrationEvent @event)
    {
        _integrationEvents.Add(@event);
    }

    public IReadOnlyCollection<IntegrationEvent> GetEvents() => _integrationEvents.ToList();

    public void ClearEvents()
    {
        _integrationEvents.Clear();
    }

}
