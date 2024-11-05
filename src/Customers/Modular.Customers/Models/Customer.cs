namespace Modular.Customers.Models;

public class Customer
{
    public Guid Id { get; private init; }
    public FullName FullName { get; private set; }
    public Address Address { get; private set; }
    public Address? ShippingAddress { get; private set; }
    public Contact Contact { get; private set; }

    private Customer() { }

    private Customer(Guid id, FullName fullName, Address address, Address? shippingAddress, Contact contact)
    {
        Id = id;
        FullName = fullName;
        Address = address;
        ShippingAddress = shippingAddress;
        Contact = contact;
    }

    public void ChangeAddress(Address newAddress)
    {
        if (newAddress is null)
        {
            throw new ArgumentNullException(nameof(newAddress), "Address cannot be null.");
        }

        Address = newAddress;
    }

    public void ChangeShippingAddress(Address newShipingAddress)
    {
        if (newShipingAddress is null)
        {
            throw new ArgumentNullException(nameof(newShipingAddress), "Shipping address cannot be null.");
        }

        ShippingAddress = newShipingAddress;
    }

    public void ChangeContact(Contact contact)
    {
        if (contact is null)
        {
            throw new ArgumentNullException(nameof(contact), "Contact cannot be null.");
        }

        Contact = contact;
    }

    public void ChangeFullName(FullName fullName)
    {
        if (fullName is null)
        {
            throw new ArgumentNullException(nameof(fullName), "FullName cannot be null.");
        }

        FullName = fullName;
    }

    public static Customer Create(FullName fullName, Address address, Address? shippingAddress, Contact contact)
    {
        if (fullName is null)
        {
            throw new ArgumentNullException(nameof(fullName), "FullName cannot be null.");
        }

        if (address is null)
        {
            throw new ArgumentNullException(nameof(address), "Address cannot be null.");
        }

        if (contact is null)
        {
            throw new ArgumentNullException(nameof(contact), "Contact cannot be null.");
        }

        var id = Ulid.NewUlid().ToGuid();
        return new Customer(id, fullName, address, shippingAddress ?? address, contact);
    }
}


