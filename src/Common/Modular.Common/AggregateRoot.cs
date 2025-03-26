namespace Modular.Common;

public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected void RaiseEvent(IDomainEvent @event)
    {
        _domainEvents.Add(@event);
    }

    public IReadOnlyCollection<IDomainEvent> GetEvents() => _domainEvents.ToList();

    public void ClearEvents()
    {
        _domainEvents.Clear();
    }

}
