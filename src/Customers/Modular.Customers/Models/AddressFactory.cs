using ErrorOr;
using Modular.Common.User;

namespace Modular.Customers.Models;

internal static class AddressFactory
{
    internal static ErrorOr<(Address Address, Address ShippingAddress)> CreateWithFallback(AddressDto address, AddressDto? shippingAddress)
    {
        ErrorOr<Address> addressResult = Address.Create(address.Street, address.City, address.State, address.Zip);

        if (addressResult.IsError)
        {
            return addressResult.FirstError;
        }

        if (shippingAddress is null)
        {
            return (addressResult.Value, addressResult.Value);
        }

        ErrorOr<Address> shippingAddressResult = Address.Create(shippingAddress.Street, shippingAddress.City, shippingAddress.State, shippingAddress.Zip);

        if (shippingAddressResult.IsError)
        {
            return shippingAddressResult.FirstError;
        }

        return (addressResult.Value, shippingAddressResult.Value);
    }
}
