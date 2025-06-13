using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Modular.Common;

namespace Modular.Customers.Models;

public class ContactFactory
{
    private readonly CustomerDbContext _customerDbContext;
    const string CustomerContactValidationCode = "Customer.Contact.Validation";

    public ContactFactory(CustomerDbContext customerDbContext)
    {
        _customerDbContext = customerDbContext;
    }

    internal async Task<ErrorOr<Contact>> CreateAsync(string? email, string? phone, PrimaryContactType primaryContactType) =>
        await CreateAsync(Guid.Empty, email, phone, primaryContactType);

    internal async Task<ErrorOr<Contact>> CreateAsync(Guid customerId, string? email, string? phone, PrimaryContactType primaryContactType)
    {
        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phone))
        {
            return Error.Validation(CustomerContactValidationCode, "At least Email or Phone number must be presented.");
        }

        if (primaryContactType == PrimaryContactType.Email && string.IsNullOrEmpty(email))
        {
            return Error.Validation(CustomerContactValidationCode, "Email is required when it is the primary contact method.");
        }

        if (primaryContactType == PrimaryContactType.Phone && string.IsNullOrEmpty(phone))
        {
            return Error.Validation(CustomerContactValidationCode, "Phone is required when it is the primary contact method.");
        }

        if (email is not null)
        {
            bool emailAlreadyExists = await _customerDbContext.Customers.AnyAsync(c => c.Contact.Email == email && c.Id != customerId);

            if (emailAlreadyExists)
            {
                return Error.Validation(CustomerContactValidationCode, "Email already exists.");
            }
        }

        if (phone is not null)
        {
            bool phoneAlreadyExists = await _customerDbContext.Customers.AnyAsync(c => c.Contact.Phone == phone && c.Id != customerId);

            if (phoneAlreadyExists)
            {
                return Error.Validation(CustomerContactValidationCode, "Phone number already exists.");
            }
        }

        return new Contact(email, phone, primaryContactType);
    }

}
