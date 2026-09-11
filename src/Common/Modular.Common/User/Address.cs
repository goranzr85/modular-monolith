using ErrorOr;
using Modular.Common.Errors;

namespace Modular.Common.User;

public sealed class Address : IEquatable<Address>
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

    public static ErrorOr<Address> Create(string street, string city, string state, string zip)
    {
        if (string.IsNullOrEmpty(street))
        {
            return AddressErrors.InvalidStreet();
        }

        if (string.IsNullOrEmpty(city))
        {
            return AddressErrors.InvalidCity();
        }

        if (string.IsNullOrEmpty(state))
        {
            return AddressErrors.InvalidState();
        }

        if (string.IsNullOrEmpty(zip))
        {
            return AddressErrors.InvalidZip();
        }

        return new Address(street, city, state, zip);
    }

    public bool Equals(Address? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Street == other.Street && City == other.City && State == other.State && Zip == other.Zip;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Address);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Street, City, State, Zip);
    }

    public static bool operator ==(Address? left, Address? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Address? left, Address? right)
    {
        return !Equals(left, right);
    }

}
