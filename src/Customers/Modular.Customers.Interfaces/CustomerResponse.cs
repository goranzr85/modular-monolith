namespace Modular.Customers.Interfaces;

public sealed record CustomerResponse(Guid Id, string FirstName, string? MiddleName, string LastName,
    string Street, string City, string Zip, string State, string? Email, string? Phone)
{
}