using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Customers.IntegrationEvents;
using CustomerContact = Modular.Common.User.Contact;
using CustomerFullName = Modular.Common.User.FullName;

namespace Modular.Notifications.Customers;
internal sealed class CustomerEventsHandler : IConsumer<CustomerCreatedEvent>,
    IConsumer<CustomerChangedNameEvent>,
    IConsumer<CustomerChangedContactInformationEvent>
{
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<CustomerEventsHandler> _logger;

    public CustomerEventsHandler(NotificationDbContext dbContext, ILogger<CustomerEventsHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CustomerCreatedEvent> context)
    {
        Customer? customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == context.Message.Id);

        if (customer is not null)
        {
            _logger.LogWarning("Customer with ID {CustomerId} already exists in the database.", context.Message.Id);
        }

        FullName fullName = context.Message.FullName;
        ContactInfo contact = context.Message.Contact;

        customer = new Customer
        {
            Id = context.Message.Id,
            FullName = CustomerFullName.Create(fullName.FirstName, fullName.MiddleName, fullName.LastName)!.Value!,
            Contact = new CustomerContact(contact.Email, contact.PhoneNumber, contact.PrimaryContactType),
        };

        await _dbContext.Customers.AddAsync(customer);
        await _dbContext.SaveChangesAsync();
    }

    public async Task Consume(ConsumeContext<CustomerChangedNameEvent> context)
    {
        Customer? customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == context.Message.CustomerId);

        if (customer is null)
        {
            _logger.LogWarning("Customer with ID {CustomerId} does not exist in the database.", context.Message.CustomerId);
        }

        FullName fullName = context.Message.FullName;

        customer!.FullName = CustomerFullName.Create(fullName.FirstName, fullName.MiddleName, fullName.LastName)!.Value!;

        _dbContext.Customers.Update(customer);
        await _dbContext.SaveChangesAsync();
    }

    public async Task Consume(ConsumeContext<CustomerChangedContactInformationEvent> context)
    {
        Customer? customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == context.Message.CustomerId);

        if (customer is null)
        {
            _logger.LogWarning("Customer with ID {CustomerId} does not exist in the database.", context.Message.CustomerId);
        }

        var contact = context.Message.ContactInfo;

        customer!.Contact = new CustomerContact
        (
            contact.Email,
            contact.PhoneNumber,
            contact.PrimaryContactType
        );

        _dbContext.Customers.Update(customer);
        await _dbContext.SaveChangesAsync();
    }
}
