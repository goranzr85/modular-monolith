namespace Modular.Customers.Change;

internal sealed record ChangeCustomerRequest(string FirstName, string MiddleName, string LastName,
    string Street, string City, string Zip, string State, string Email, string Phone)
{
}