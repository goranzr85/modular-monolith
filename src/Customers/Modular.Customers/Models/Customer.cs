using ErrorOr;
using Modular.Common;
using Modular.Common.User;
using Modular.Customers.Errors;
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
        if (Address.Equals(newAddress))
        {
            return;
        }

        Address = newAddress;
    }

    internal void ChangeShippingAddress(Address newShipingAddress)
    {
        if (ShippingAddress is not null && ShippingAddress.Equals(newShipingAddress))
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

    internal ErrorOr<Unit> ChangeFullName(FullName fullName)
    {
        if (fullName is null)
        {
            return CustomerErrors.InvalidFullName();
        }

        RaiseEvent(new CustomerChangedNameEvent(Id,
            new IntegrationEvents.FullName(fullName.FirstName,
            fullName.MiddleName,
            fullName.LastName)));

        FullName = fullName;

        return Unit.Value;
    }

    public static ErrorOr<Customer> Create(FullName fullName, Address address, Address? shippingAddress, Contact contact)
    {
        if (fullName is null)
        {
            return CustomerErrors.InvalidFullName();
        }

        if (address is null)
        {
            return CustomerErrors.InvalidAddress();
        }

        if (contact is null)
        {
            return CustomerErrors.InvalidContact();
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
