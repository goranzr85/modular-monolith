using Modular.Customers.Models;

namespace Modular.Customers.UseCases.Create;

internal sealed record CreateCustomerRequest(string FirstName, string? MiddleName, string LastName,
    AddressDto Address, AddressDto? ShippingAddress, string? Email, string? Phone)
{
}