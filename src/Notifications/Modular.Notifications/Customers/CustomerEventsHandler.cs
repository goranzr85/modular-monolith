using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Common.Events;
using Modular.Customers.IntegrationEvents;
using CustomerContact = Modular.Common.User.Contact;
using CustomerFullName = Modular.Common.User.FullName;

namespace Modular.Notifications.Customers;
internal sealed class CustomerEventsHandler : IIntegrationEventConsumer<CustomerCreatedEvent>,
    IIntegrationEventConsumer<CustomerChangedNameEvent>,
    IIntegrationEventConsumer<CustomerChangedContactInformationEvent>
{
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<CustomerEventsHandler> _logger;

    public CustomerEventsHandler(NotificationDbContext dbContext, ILogger<CustomerEventsHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ConsumeAsync(CustomerCreatedEvent message, CancellationToken cancellationToken)
    {
        Customer? customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == message.Id, cancellationToken);

        if (customer is not null)
        {
            _logger.LogWarning("Customer with ID {CustomerId} already exists in the database.", message.Id);
            return;
        }

        FullName fullName = message.FullName;
        ContactInfo contact = message.Contact;

        customer = new Customer
        {
            Id = message.Id,
            FullName = CustomerFullName.Create(fullName.FirstName, fullName.MiddleName, fullName.LastName)!.Value!,
            Contact = new CustomerContact(contact.Email, contact.PhoneNumber, contact.PrimaryContactType),
        };

        await _dbContext.Customers.AddAsync(customer, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ConsumeAsync(CustomerChangedNameEvent message, CancellationToken cancellationToken)
    {
        Customer? customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == message.CustomerId, cancellationToken);

        if (customer is null)
        {
            _logger.LogWarning("Customer with ID {CustomerId} does not exist in the database.", message.CustomerId);
            return;
        }

        FullName fullName = message.FullName;

        customer.FullName = CustomerFullName.Create(fullName.FirstName, fullName.MiddleName, fullName.LastName)!.Value!;

        _dbContext.Customers.Update(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ConsumeAsync(CustomerChangedContactInformationEvent message, CancellationToken cancellationToken)
    {
        Customer? customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == message.CustomerId, cancellationToken);

        if (customer is null)
        {
            _logger.LogWarning("Customer with ID {CustomerId} does not exist in the database.", message.CustomerId);
            return;
        }

        var contact = message.ContactInfo;

        customer.Contact = new CustomerContact
        (
            contact.Email,
            contact.PhoneNumber,
            contact.PrimaryContactType
        );

        _dbContext.Customers.Update(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
