using Modular.Common.User;

namespace Modular.Notifications.Customers;
public sealed class Customer
{
    public Guid Id { get; init; }
    public FullName FullName { get; init; }
    public Contact Contact { get; init; }
}
