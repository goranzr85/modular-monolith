﻿using Modular.Customers.Models;

namespace Modular.Customers.Change;

internal sealed record ChangeCustomerRequest(string FirstName, string? MiddleName, string LastName,
    AddressDto Address, AddressDto? ShippingAddress, string? Email, string? Phone)
{
}