using Modular.Common.User;
using Modular.Customers.Models;

namespace Modular.Customers.UseCases.Change;

internal sealed record ChangeCustomerRequest(string FirstName, string? MiddleName, string LastName,
    AddressDto Address, AddressDto? ShippingAddress, string? Email, string? Phone, PrimaryContactType PrimaryContactType)
{
}