namespace Modular.Customers.UseCases.Get;

internal sealed record GetCustomerResponse(Guid Id, string FirstName, string? MiddleName, string LastName,
    string Street, string City, string Zip, string State, string? Email, string? Phone)
{
}