namespace Modular.Customers.Create;

internal sealed record CreateCustomerRequest(string FirstName, string MiddleName, string LastName,
    string Street, string City, string Zip, string State, string Email, string Phone)
{
}