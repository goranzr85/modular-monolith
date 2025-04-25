using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modular.Customers.Models;

namespace Modular.Customers.UseCases.Change;

internal sealed record ChangeCustomerCommand(Guid CustomerId, string FirstName, string? MiddleName, string LastName,
AddressDto Address, AddressDto? ShippingAddress, string? Email, string? Phone) : IRequest<ErrorOr<Unit>>
{
}

internal sealed class ChangeCustomerCommandHandler : IRequestHandler<ChangeCustomerCommand, ErrorOr<Unit>>
{
    private readonly CustomerDbContext _customerDbContext;
    private readonly ContactFactory _contactFactory;

    public ChangeCustomerCommandHandler(CustomerDbContext customerDbContext, ContactFactory contactFactory)
    {
        _customerDbContext = customerDbContext;
        _contactFactory = contactFactory;
    }

    public async Task<ErrorOr<Unit>> Handle(ChangeCustomerCommand request, CancellationToken cancellationToken)
    {
        Customer? customer = await _customerDbContext.Customers
             .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (customer is null)
        {
            return Error.NotFound("Customers.NotFound", "Customer does not exist.");
        }

        if (customer.Contact.Email != request.Email || customer.Contact.Phone != request.Phone)
        {
            ErrorOr<Contact> newContactResponse = await _contactFactory.CreateAsync(request.CustomerId, request.Email, request.Phone);

            if (newContactResponse.IsError)
            {
                return newContactResponse.FirstError;
            }

            customer.ChangeContact(newContactResponse.Value);
        }

        Address newAddress = Address.Create(request.Address.Street, request.Address.City, request.Address.State, request.Address.Zip);
        customer.ChangeAddress(newAddress);

        Address newShippingAddress = request.ShippingAddress is not null ?
                Address.Create(request.ShippingAddress.Street, request.ShippingAddress.City, request.ShippingAddress.State, request.ShippingAddress.Zip)
                : newAddress;
        customer.ChangeShippingAddress(newShippingAddress);

        ErrorOr<FullName> fullNameResponse = FullName.Create(request.FirstName, request.MiddleName, request.LastName);

        if (fullNameResponse.IsError)
        {
            return fullNameResponse.FirstError;
        }

        customer.ChangeFullName(fullNameResponse.Value);

        _customerDbContext.Customers.Update(customer);
        await _customerDbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}