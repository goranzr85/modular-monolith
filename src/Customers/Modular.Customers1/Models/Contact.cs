namespace Modular.Customers.Models;

public sealed class Contact
{
    public string? Email { get; private set; }
    public string? Phone { get; private set; }

    internal Contact(string? email, string? phone)
    {
        Email = email;
        Phone = phone;
    }
}



