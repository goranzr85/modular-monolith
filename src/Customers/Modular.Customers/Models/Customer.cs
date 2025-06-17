using Modular.Common;
using Modular.Common.User;
using Modular.Customers.IntegrationEvents;
using FullName = Modular.Common.User.FullName;
using Address = Modular.Common.User.Address;

namespace Modular.Customers.Models;

public sealed class Customer : AggregateRoot
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

    internal void ChangeAddress(Address newAddress)
    {
        if (!Address.Equals(newAddress))
        {
            return;
        }

        Address = newAddress;
    }

    internal void ChangeShippingAddress(Address newShipingAddress)
    {
        if (!(bool)ShippingAddress?.Equals(newShipingAddress))
        {
            return;
        }

        RaiseEvent(new CustomerChangedShippingAddressEvent(Id,
            new IntegrationEvents.Address(newShipingAddress.Street,
            newShipingAddress.City,
            newShipingAddress.State,
            newShipingAddress.Zip)));

        ShippingAddress = newShipingAddress;
    }

    internal void ChangeContact(Contact contact)
    {
        if (Contact.Equals(contact))
        {
            return;
        }

        RaiseEvent(new CustomerChangedContactInformationEvent(Id,
            new ContactInfo(contact.Email,
                contact.Phone,
                contact.PrimaryContactType)));

        Contact = contact;
    }

    internal void ChangeFullName(FullName fullName)
    {
        if (fullName is null)
        {
            throw new ArgumentNullException(nameof(fullName), "FullName cannot be null.");
        }

        RaiseEvent(new CustomerChangedNameEvent(Id,
            new IntegrationEvents.FullName(fullName.FirstName,
            fullName.MiddleName,
            fullName.LastName)));

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
        Customer customer = new(id, fullName, address, shippingAddress ?? address, contact);

        customer.RaiseEvent(new CustomerCreatedEvent(id,
            new IntegrationEvents.FullName(fullName.FirstName, fullName.MiddleName, fullName.LastName),
            new IntegrationEvents.Address(address.Street, address.City, address.State, address.Zip),
            new ContactInfo(contact.Email, contact.Phone, contact.PrimaryContactType)));

        return customer;
    }
}


