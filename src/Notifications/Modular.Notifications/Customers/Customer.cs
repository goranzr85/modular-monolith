using Modular.Common.User;

namespace Modular.Notifications.Customers;
public sealed class Customer
{
    public Guid Id { get; init; }
    public FullName FullName { get; internal set; }
    public Contact Contact { get; internal set; }
}
