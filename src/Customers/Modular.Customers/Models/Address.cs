namespace Modular.Customers.Models;

public sealed class Address
{
    public string Street { get; private init; }
    public string City { get; private init; }
    public string State { get; private init; }
    public string Zip { get; private init; }

    private Address(string street, string city, string state, string zip)
    {
        Street = street;
        City = city;
        State = state;
        Zip = zip;
    }

    public static Address Create(string street, string city, string state, string zip)
    {
        if (string.IsNullOrEmpty(street))
        {
            throw new ArgumentException("Street cannot be null or empty.", nameof(street));
        }

        if (string.IsNullOrEmpty(city))
        {
            throw new ArgumentException("City cannot be null or empty.", nameof(city));
        }

        if (string.IsNullOrEmpty(state))
        {
            throw new ArgumentException("State cannot be null or empty.", nameof(state));
        }

        if (string.IsNullOrEmpty(zip))
        {
            throw new ArgumentException("Zip cannot be null or empty.", nameof(zip));
        }

        return new Address(street, city, state, zip);
    }
}


