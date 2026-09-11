using ErrorOr;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Modular.Common;
using Modular.Common.User;
using Modular.Customers.Errors;
using Modular.Customers.Models;

namespace Modular.Customers.UseCases.Change;

internal sealed record ChangeCustomerCommand(Guid CustomerId, string FirstName, string? MiddleName, string LastName,
AddressDto Address, AddressDto? ShippingAddress, string? Email, string? Phone, PrimaryContactType PrimaryContactType)
{
}

internal sealed class ChangeCustomerCommandHandler
{
    private readonly CustomerDbContext _customerDbContext;
    private readonly ContactFactory _contactFactory;
    private readonly IValidator<ChangeCustomerCommand> _validator;

    public ChangeCustomerCommandHandler(CustomerDbContext customerDbContext, ContactFactory contactFactory, IValidator<ChangeCustomerCommand> validator)
    {
        _customerDbContext = customerDbContext;
        _contactFactory = contactFactory;
        _validator = validator;
    }

    public async Task<ErrorOr<Unit>> Handle(ChangeCustomerCommand request, CancellationToken cancellationToken)
    {
        List<Error> validationErrors = await _validator.GetValidationErrorsAsync(request, cancellationToken);

        if (validationErrors.Count > 0)
        {
            return validationErrors;
        }

        Customer? customer = await _customerDbContext.Customers
             .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (customer is null)
        {
            return CustomerErrors.CustomerNotFound(request.CustomerId);
        }

        if (IsContactChanged(request, customer))
        {
            ErrorOr<Contact> newContactResponse = await _contactFactory.CreateAsync(request.CustomerId, request.Email, request.Phone, request.PrimaryContactType);

            if (newContactResponse.IsError)
            {
                return newContactResponse.FirstError;
            }

            customer.ChangeContact(newContactResponse.Value);
        }

        ErrorOr<(Address Address, Address ShippingAddress)> addressResult = AddressFactory.CreateWithFallback(request.Address, request.ShippingAddress);

        if (addressResult.IsError)
        {
            return addressResult.FirstError;
        }

        (Address newAddress, Address newShippingAddress) = addressResult.Value;
        customer.ChangeAddress(newAddress);
        customer.ChangeShippingAddress(newShippingAddress);

        ErrorOr<FullName> fullNameResponse = FullName.Create(request.FirstName, request.MiddleName, request.LastName);

        if (fullNameResponse.IsError)
        {
            return fullNameResponse.FirstError;
        }

        ErrorOr<Unit> changeFullNameResult = customer.ChangeFullName(fullNameResponse.Value);

        if (changeFullNameResult.IsError)
        {
            return changeFullNameResult.FirstError;
        }

        _customerDbContext.Customers.Update(customer);
        await _customerDbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private static bool IsContactChanged(ChangeCustomerCommand request, Customer customer)
    {
        return customer.Contact.Email != request.Email || customer.Contact.Phone != request.Phone || customer.Contact.PrimaryContactType != request.PrimaryContactType;
    }
}