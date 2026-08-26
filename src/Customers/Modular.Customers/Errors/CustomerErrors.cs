using ErrorOr;

namespace Modular.Customers.Errors;

internal static class CustomerErrors
{
    internal static Error CustomerNotFound(Guid customerId) =>
        Error.NotFound("Customer.NotFound", $"Customer with ID '{customerId}' was not found.");

    internal static Error CustomerNotCreated() =>
        Error.Failure("Customer.Failure", "Creating customer failed.");

    internal static Error InvalidFullName() =>
        Error.Validation("Customer.InvalidFullName", "FullName cannot be null.");

    internal static Error InvalidAddress() =>
        Error.Validation("Customer.InvalidAddress", "Address cannot be null.");

    internal static Error InvalidContact() =>
        Error.Validation("Customer.InvalidContact", "Contact cannot be null.");
}
