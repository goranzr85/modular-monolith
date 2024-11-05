using ErrorOr;
using Microsoft.EntityFrameworkCore;

namespace Modular.Customers.Models;

public class ContactFactory
{
    private readonly CustomerDbContext _customerDbContext;

    public ContactFactory(CustomerDbContext customerDbContext)
    {
        _customerDbContext = customerDbContext;
    }

    internal async Task<ErrorOr<Contact>> CreateAsync(string email, string phone) => await CreateAsync(Guid.Empty, email, phone);

    internal async Task<ErrorOr<Contact>> CreateAsync(Guid customerId, string email, string phone)
    {
        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phone))
        {
            return Error.Validation("Customer.Contact.Validation", "At least Email or Phone number must be presented.");
        }

        if (email is not null)
        {
            bool emailAlreadyExists = await _customerDbContext.Customers.AnyAsync(c => c.Contact.Email == email && c.Id != customerId);

            if (emailAlreadyExists)
            {
                return Error.Validation("Customer.Contact.Validation", "Email already exists.");
            }
        }

        if (phone is not null)
        {
            bool phoneAlreadyExists = await _customerDbContext.Customers.AnyAsync(c => c.Contact.Phone == phone && c.Id != customerId);

            if (phoneAlreadyExists)
            {
                return Error.Validation("Customer.Contact.Validation", "Phone number already exists.");
            }
        }

        return new Contact(email, phone);
    }

}
